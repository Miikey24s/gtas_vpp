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
- Development dùng `TestEnv`; production dùng `LiveEnv`.
- Hai môi trường có SQL Server và Docker volume vật lý riêng, dù cùng dùng tên
  database `GTAS_VPP_LIVE` để migration không bị lệch.
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

Mỗi push vào `Nam` hoặc lần chạy `workflow_dispatch` thực hiện:

1. restore, build Release và chạy backend/frontend tests;
2. build hai image, push tag bất biến `sha-<commit>` lên GHCR;
3. tạo release `/app/gtas-vpp/releases/<commit>` và chuyển `.env` bằng SCP;
4. đăng nhập GHCR bằng Docker config tạm, tự xóa khi phiên SSH kết thúc;
5. kiểm tra/khắc phục mapping SQL public, giữ volume
   `gtas-vpp_sqlserver-data`;
6. nếu secret SQL đổi, backup bằng credential cũ, `ALTER LOGIN sa`, rồi recreate
   container với credential mới và nguyên volume;
7. tạo `BACKUP ... WITH CHECKSUM`, chạy `RESTORE VERIFYONLY`, rồi mới migration;
8. thay backend, chờ healthy; thay frontend, chờ healthy;
9. validate/reload Nginx, audit host và gọi public `/healthz`;
10. chỉ khi mọi bước pass mới chuyển symlink `/app/gtas-vpp/current`.

Nếu backend/frontend mới lỗi, script tự quay về cặp image trước. Migration database
không tự rollback vì migration ngược có thể phá dữ liệu; backup `pre-deploy` là
điểm phục hồi có chủ ý.

## Backup và restore

Timer `gtas-vpp-backup.timer` tạo backup logic đã kiểm chứng mỗi đêm lúc 02:15
giờ Việt Nam, giữ mặc định 14 ngày trong volume
`gtas-vpp_sqlserver-backups`.

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

Restore là thao tác phá trạng thái hiện tại. Chỉ chạy sau khi chọn đúng file,
xác nhận có backup `before-restore`, thông báo downtime và nhập confirmation:

```bash
cd /app/gtas-vpp/current
RESTORE_CONFIRM=GTAS_VPP_LIVE bash deploy/restore-db.sh <backup-file-name.bak>
```

Sau restore, kiểm tra container, đăng nhập, các luồng chính và public health. Nên
diễn tập restore định kỳ với một Droplet/database tạm thay vì đợi đến lúc có sự cố.

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
