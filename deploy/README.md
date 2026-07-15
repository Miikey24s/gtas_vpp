# GTAS VPP Development/Production runbook

Tài liệu này là nguồn hướng dẫn chính cho môi trường production học tập tại
`https://gtas-vpp.annam.id.vn`. Kiến trúc production là một DigitalOcean Droplet
Ubuntu 24.04, Docker Compose, SQL Server 2022 Developer, backend ASP.NET Core,
frontend Blazor Server và Nginx kết thúc TLS.

SQL Server Developer chỉ phù hợp mục đích phát triển, kiểm thử, demo và học tập.
Không dùng cấu hình này cho hoạt động thương mại.

## Ranh giới môi trường

- Development ưu tiên Aspire; phương án full-container dùng `docker-compose.yml`.
- Production chỉ dùng `docker-compose.prod.yml` và image GHCR có tag `sha-<commit>`.
- Khi database đã tồn tại, deploy pin đúng image ID SQL Server đang chạy; nâng cấp
  SQL Server là một maintenance riêng, có backup và diễn tập restore trước.
- Development dùng `TestEnv`; production dùng `LiveEnv`.
- Hai môi trường có SQL Server, Docker volume và tên database nghiệp vụ vật lý riêng:
  Development dùng `GTAS_VPP_TEST`, production dùng `GTAS_VPP_LIVE`. Production
  còn có `GTAS_MENU` chứa tài khoản; hai database production là một cặp logic phải
  được backup cùng một backup-set.
- Không chép database production về máy cá nhân nếu trong đó có dữ liệu thật.

## Baseline Droplet

Droplet cần có Docker Engine + Compose v2, Nginx, Certbot, `curl`, `systemd` và
một user deploy đăng nhập bằng SSH key. User deploy cần chạy được Docker và các
lệnh `sudo install`, `nginx -t`, `systemctl reload/enable` mà pipeline sử dụng.

DigitalOcean Cloud Firewall nên chỉ có inbound:

| Port | Nguồn | Mục đích |
| --- | --- | --- |
| TCP 22 | IP quản trị đã biết | SSH |
| TCP 80 | mọi nơi | chuyển hướng HTTP và ACME |
| TCP 443 | mọi nơi | HTTPS |

Không mở `1433`, `5000` hoặc `8080`. Compose bind ba port nội bộ này vào
`127.0.0.1`; SQL Server chỉ nên truy cập từ máy quản trị qua SSH tunnel khi cần.
SSH nên đặt `PasswordAuthentication no` và `PermitRootLogin no` hoặc
`prohibit-password`, sau khi đã xác minh user deploy/sudo hoạt động.

## GitHub production environment và secrets

Workflow `.github/workflows/deploy.yml` gắn với environment tên `production`.
Cấu hình các secret sau ở GitHub; ưu tiên environment secret thay vì repository
secret khi gói GitHub hiện tại hỗ trợ:

- `SSH_HOST`: hostname hoặc IP Droplet.
- `SSH_USER`: user deploy, không nên là root.
- `SSH_KEY`: private key chỉ dùng cho deploy.
- `SSH_KNOWN_HOSTS`: dòng host key đã xác minh của Droplet. Lấy bằng
  `ssh-keyscan -H <host>`, nhưng phải đối chiếu fingerprint trong DigitalOcean
  console trước khi lưu.
- `ENV_FILE_CONTENT`: nội dung dotenv production.
- `DB_SA_PASSWORD`: password SQL mới, ngẫu nhiên và riêng cho production.
- `JWT_KEY`: signing key ngẫu nhiên tối thiểu 32 byte.
- `PASSWORD_ENCRYPTION_KEY`: key TripleDES tương thích dữ liệu cũ; được lưu riêng
  để có thể luân chuyển khỏi composite secret.

`ENV_FILE_CONTENT` tối thiểu có:

```dotenv
MSSQL_MEMORY_LIMIT_MB=4096
REPORT_INSIGHTS_ENABLED=false
OPENAI_API_KEY=
```

Với database đang tồn tại, không tự tạo lại `PASSWORD_ENCRYPTION_KEY`: key phải
khớp dữ liệu `tblUsers.PasswordChar`. Nếu key từng xuất hiện trong Git công khai,
cần lập kế hoạch reset mật khẩu/migrate sang password hash; đổi key đơn lẻ sẽ làm
người dùng hiện có không đăng nhập được.

Script `deploy/validate-env.sh` chặn secret trống, placeholder, JWT ngắn, password
SQL quá yếu và trường hợp bật AI nhưng thiếu API key. Giá trị secret không được
in vào log.

## Luồng CI/CD

Mỗi push vào `Nam` có thay đổi source backend/frontend hoặc hạ tầng Production, hay lần
chạy `workflow_dispatch`, thực hiện:

1. restore, build Release và chạy backend/frontend tests;
2. build hai image, push tag bất biến `sha-<commit>` lên GHCR;
3. tạo release `/app/gtas-vpp/releases/<commit>` và chuyển `.env` bằng SCP;
4. đăng nhập GHCR bằng Docker config tạm, tự xóa khi phiên SSH kết thúc;
5. kiểm tra/khắc phục mapping SQL public, tự lấy đúng named volume đang mount từ
   container hiện hành và giữ container cũ dưới tên dự phòng cho đến khi migrator
   thành công;
6. nếu secret SQL đổi, backup cặp database bằng credential đang hoạt động,
   `ALTER LOGIN sa`, rồi recreate container với credential mới và nguyên volume;
7. tạo `BACKUP ... WITH COPY_ONLY, CHECKSUM` và `RESTORE VERIFYONLY ... WITH CHECKSUM`
   cho cả `GTAS_VPP_LIVE` lẫn `GTAS_MENU`, rồi mới migration;
8. thay backend, chờ healthy; thay frontend, chờ healthy;
9. validate/reload Nginx, tắt SSH password, chỉ cho root đăng nhập bằng key, xóa các
   rule UFW public cũ của `1433`/`5000`/`8080`, rồi audit host;
10. kết nối SSH lại bằng public-key-only và gọi public `/healthz`;
11. chỉ khi mọi bước pass mới chuyển symlink `/app/gtas-vpp/current`.

Nếu runner/SSH bị ngắt đúng lúc đổi container SQL, lần deploy sau phát hiện
`gtas-vpp-db-previous` và khôi phục container đó trước khi đọc credential, volume hoặc
thực hiện backup mới.

Nếu backend/frontend mới lỗi, script tự quay về cặp image trước. Migration database
không tự rollback vì migration ngược có thể phá dữ liệu; backup `pre-deploy` là
điểm phục hồi có chủ ý.

Credential SQL đã rotate không bao giờ bị đổi ngược về credential cũ khi deploy lỗi.
Error handler hoàn tất theo hướng roll-forward tới `DB_SA_PASSWORD` mong muốn,
recreate container SQL để metadata không giữ credential cũ, rồi recreate cặp image
ứng dụng trước bằng `.env` hiện hành nên chúng cũng dùng credential mới. Container
SQL cũ chỉ bị xóa sau khi kết nối bằng credential mới đã pass. Nếu tự động phục hồi
chưa hoàn tất, giữ nguyên credential mới và điều tra/retry; không đưa credential đã
bị thu hồi trở lại.

## Backup và restore

Timer `gtas-vpp-backup.timer` gọi `backup-db-pair.sh` mỗi đêm lúc 02:15 giờ Việt Nam.
Mỗi backup-set dùng chung label/timestamp, gồm một file `GTAS_VPP_LIVE` và một file
`GTAS_MENU`; từng file đều là `COPY_ONLY`, có `CHECKSUM` và đã qua `RESTORE VERIFYONLY`.
Retention mặc định là 14 ngày trong volume `gtas-vpp_sqlserver-backups`.

```bash
systemctl list-timers gtas-vpp-backup.timer
systemctl status gtas-vpp-backup.service
docker exec gtas-vpp-db ls -lh /var/opt/mssql/backup
```

Volume backup vẫn nằm trên cùng Droplet nên chỉ bảo vệ tốt trước migration hoặc
lỗi thao tác, không bảo vệ khi mất toàn bộ Droplet. Với mức học tập, lớp off-host
hợp lý là DigitalOcean weekly backup hoặc một object-storage bucket riêng. Tính
năng DigitalOcean automated backup có phí và phải được bật riêng; repository này
không tự phát sinh chi phí.

Restore là thao tác phá trạng thái hiện tại. `restore-db.sh` xác minh file được chọn,
dừng backend/frontend để chặn write mới, rồi tạo và xác minh backup-set
`before-restore` cho cả hai database trước mutation. Nếu backup/restore lỗi, cleanup
sẽ đưa database về multi-user khi cần và thử khởi động lại ứng dụng. Chỉ chạy sau
khi chọn đúng file, thông báo downtime, xác minh không có deploy/migrator/backup job
khác đang chạy và nhập confirmation:

```bash
cd /app/gtas-vpp/current
RESTORE_CONFIRM=GTAS_VPP_LIVE \
  bash deploy/restore-db.sh "GTAS_VPP_LIVE_<label>_<UTC-timestamp>.bak"
```

Khi recovery point có thể ảnh hưởng tài khoản hoặc phân quyền, luôn restore cả
cặp database trong một maintenance window thay vì gọi script đơn hai lần:

```bash
cd /app/gtas-vpp/current
RESTORE_PAIR_CONFIRM=GTAS_VPP_LIVE+GTAS_MENU \
  bash deploy/restore-db-pair.sh <label> <UTC-YYYYMMDDTHHMMSSZ>
```

`restore-db-pair.sh` xác minh đúng hai file cùng label/timestamp, dừng writers
một lần, tạo recovery pair mới rồi restore cả `GTAS_VPP_LIVE` và `GTAS_MENU`.
Nếu một restore lỗi, script thử phục hồi cả hai database từ recovery pair; nếu
không thể phục hồi đầy đủ thì giữ ứng dụng dừng để tránh chạy trên dữ liệu lệch.

Script chỉ nhận file có prefix trùng `DB_NAME` và đúng format do `backup-db.sh`
tạo, nhằm chặn việc vô tình restore backup `GTAS_MENU` vào `GTAS_VPP_LIVE` hoặc
ngược lại. Restore thành công chỉ được báo sau khi cả backend và frontend healthy.

Hai file trong backup-set được tạo liên tiếp và không phải snapshot transaction
nguyên tử giữa database. Với phục hồi sự cố có thay đổi tài khoản/quyền, dừng luồng
ghi, chọn đúng hai file cùng label/timestamp và lập thứ tự restore phối hợp; không
ghép file từ hai backup-set khác nhau. Sau restore, kiểm tra container, đăng nhập,
các luồng chính và public health. Nên diễn tập restore định kỳ với một
Droplet/database tạm thay vì đợi đến lúc có sự cố.

## Kiểm tra vận hành

```bash
cd /app/gtas-vpp/current
BE_IMAGE=$(grep '^BE_IMAGE=' deploy-state.env | cut -d= -f2-)
FE_IMAGE=$(grep '^FE_IMAGE=' deploy-state.env | cut -d= -f2-)
export BE_IMAGE FE_IMAGE
docker compose -f docker-compose.prod.yml ps
bash deploy/audit-host.sh
curl -fsS https://gtas-vpp.annam.id.vn/healthz
sudo nginx -t
sudo certbot renew --dry-run
```

Kiểm tra dung lượng Docker/backup hằng tuần. Không chạy `docker compose down -v`,
`docker volume prune` hoặc xóa `gtas-vpp_sqlserver-data` trên production.

## Rollback ứng dụng thủ công

Mỗi release lưu `deploy-state.env` chứa đúng tag image, không chứa secret. Để quay
về một release cũ, vào thư mục release đó, export `BE_IMAGE`/`FE_IMAGE`, chạy
`docker compose up -d --force-recreate backend frontend`, đợi health rồi smoke
test. Chỉ đổi symlink `current` sau khi kiểm tra thành công.

Nếu migration mới không tương thích ngược, dừng ứng dụng và restore backup
`pre-deploy` theo quy trình trên; không cố chạy source cũ trên schema chưa được
xác minh.
