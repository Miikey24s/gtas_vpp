# GTAS VPP Refactor — Handoff Briefing cho AI assistant (GPT-5.5 / Claude / Gemini)

> **Mục đích file này**: 1 file ngắn (~5 phút đọc) để paste vào AI assistant khác, giúp họ nắm context nhanh trước khi code refactor. Để hiểu sâu hơn, đọc `AUDIT_REPORT.md` (1900 dòng, 28 trang).

---

## 1. Hệ thống là gì

- **Tên**: GTAS VPP (Văn phòng phẩm — Office stationery procurement)
- **Mục đích**: Hệ thống đặt VPP hằng tháng cho doanh nghiệp lớn (~1000 user)
- **Tech stack**:
  - Backend: ASP.NET Core 8 + EF Core + SQL Server
  - Frontend: Blazor Server + Radzen + custom CSS tokens (Linear/Vercel-inspired)
  - AI: embedded suggestion handler (Microsoft.Extensions.AI)
  - Deploy: Docker Compose, hiện đang test trên DigitalOcean; production company server (chưa biết exact)
- **Repo**: `Miikey24s/gtas_vpp` (GitHub)
- **Root**: `/opt/gtas_vpp/`

## 2. Project structure

```
/opt/gtas_vpp/
├── gtas_vpp_be/                    # Backend monorepo
│   ├── gtas_vpp_be/                # Web API (Controllers, Program.cs)
│   ├── gtas_vpp_be.Service/        # "Services" (god classes, mix với helpers + SQL infra)
│   ├── gtas_vpp_be.Model/          # EF entities + VPPMigrationDbContext (anti-pattern)
│   ├── gtas_vpp_be.Migrations/     # EF migrations
│   ├── gtas_vpp_be.AI/             # AI handler
│   ├── gtas_vpp_shared/            # DTOs + Constants
│   └── gtas_vpp_be.Tests/          # ~6 unit tests, dùng reflection (fragile)
├── gtas_vpp_fe/                    # Frontend Blazor Server
│   └── gtas_vpp_fe/gtas_vpp_fe/
│       ├── Components/Pages/VPPRequest/   # Wizard + 6 Tab
│       └── wwwroot/css/                   # 17 CSS file (vpp-tokens, kpi, datagrid...)
└── audit-report/
    ├── AUDIT_REPORT.md             # 1900 dòng audit chi tiết
    └── HANDOFF_BRIEFING.md         # FILE NÀY
```

## 3. Business rules đã được USER confirm

- **Deadline mỗi kỳ**: Ngày 5 hằng tháng `>= 00:00:00`. Từ midnight 4→5, kỳ mới bắt đầu.
- **Order type**:
  - Regular order: cho kỳ **hiện tại**, status `Submitted(1)` → user edit/cancel trước deadline.
  - Additional order: cho kỳ **vừa đóng**, status `Pending(6)` → admin Approve/Reject.
- **Status enum**: `Submitted=1, Cancelled=4, Pending=6, Approved=7, Rejected=8`.
- **Login**: Stored procedure `sp_Authen_Login` so sánh `PasswordChar` ciphertext **trực tiếp** (không decrypt). Mã hoá **TripleDES (ECB) + MD5(key) + PKCS7 + Base64**.
- **Key TripleDES test**: `"ttpsolutions"` → `Encrypt("abc*123@") = "wiSEc6nf/dK/Vu0E738j8Q=="` (golden vector phải GIỮ NGUYÊN).
- **Key production**: chưa biết, sẽ set qua env var `PasswordEncryption__Key` khi deploy.
- **DB user shared**: Mốt sẽ nối DB user thật của công ty (`GTAS_MENU.dbo.tblUsers`) → KHÔNG được đổi thuật toán encryption.

## 4. Scale & Timeline

- **Concurrent user target**: ~1000 (doanh nghiệp lớn, optimistic estimate).
- **Timeline luận văn**: 3 tháng kể từ tháng 5/2026 (user đang thực tập).
- **Phase ưu tiên**:
  - Tháng 1: P0 (security) + P1 (period logic) + P3 (performance — promoted Critical do scale)
  - Tháng 2: P2 (Clean Arch) + P5 (UX/UI)
  - Tháng 3: P6 (audit trail) + P7 (test + CI) + viết luận văn
- **CI/CD**: chưa có. Cần setup GitHub Actions.
- **Figma/mockup**: chưa có. Reference design = Linear + Vercel (đã declare trong `vpp-tokens.css`).

## 5. 🔴 9 vấn đề Critical cần fix trước (chi tiết §3.1 AUDIT_REPORT.md)

| Mã | Tóm tắt | File:line |
|---|---|---|
| F-01 | Race condition `CreateOrderAsync` — uniqueness check ngoài transaction → 2 request đồng thời tạo được 2 order regular cùng kỳ | `gtas_vpp_be.Service/Services/VPPRequestService.cs:151-187` |
| F-02 | Period logic lặp 3 chỗ (BE + Tab_Orders + Page_OrderCreate + DTO) → drift | nhiều file |
| F-03 | `GenericRepository` mở transaction nested → throw runtime nếu UoW đã có tx | `GenericRepository.cs:15-126` |
| F-04 | Dual DbContext `VPPContext` + `VPPMigrationDbContext` config trùng → schema drift risk | `Program.cs:55-78` |
| F-05 | Key TripleDES hardcode `"ttpsolutions"` trong source | `PasswordHelpers.cs:62` |
| F-06 | JWT key default trong `docker-compose.yml` → nếu thiếu env, app vẫn chạy với key yếu | `docker-compose.yml:65` |
| F-07 | Dead code BCrypt `HashPassword/VerifyPassword/IsBcryptHash` + comment misleading "fallback to bcrypt" | `PasswordHelpers.cs`, `AuthController.cs:62-65` |
| F-08 | `VPPCode = $"{Y}{M:D2}{userId}{mmss}"` — có thể trùng + leak userId | `VPPRequestService.cs:737-741` |
| F-09 | `Y/M` từ client không validate upper bound — user có thể submit `Y=2099` | `VPPRequestService.cs:159-203` |
| **F-12** | 🔴 **Promoted**: `LibraryController` load 1000 row in-memory rồi filter — OOM với 1000 user | `LibraryController.cs:85-194` |
| **F-13** | 🔴 **Promoted**: `GetDashboardCharts` load all orders rồi GroupBy in-memory | `VPPRequestController.cs:264-318` |

## 6. Constraints quan trọng

1. **KHÔNG đổi thuật toán TripleDES**, KHÔNG đổi format ciphertext. Chỉ move key vào `IConfiguration` với fallback default `"ttpsolutions"`.
2. **Golden vector test bắt buộc**: `Encrypt("abc*123@", true) == "wiSEc6nf/dK/Vu0E738j8Q=="` — gate merge tất cả PR security.
3. **Không drop column** trong migration nào (chỉ add nullable column) → backward-compatible.
4. **Test pass trước khi merge**: ít nhất `dotnet test` xanh.
5. **Không sửa AI module** trừ khi user yêu cầu (audit nhẹ).

## 7. Action plan order (recommended)

```
Week 1-2:  P0 (security + race) — 5 task
Week 3:    P1 (period logic) — 4 task
Week 4-5:  P3 (performance — promoted Critical) — 5 task
Week 6-9:  P2 (Clean Architecture) — 7 task
Week 10:   P4 (FE refactor logic) — 3 task
Week 11-12: P5 (UX/UI redesign) — 12 task
Week 13:   P6 (audit trail) — 3 task
Week 14:   P7 (test + CI) — 6 task
```

Mỗi Phase chi tiết task + acceptance test ở §7 của `AUDIT_REPORT.md`.

## 8. 10 Quick-fix (~5h total — làm trước khi vào Phase chính thức)

Xem §8.1 của AUDIT_REPORT. Highlights:
- QF-02: Xoá dead BCrypt code (15')
- QF-03: Xoá `[StructLayout(LayoutKind.Auto)]` khỏi class (10')
- QF-05: Fix JWT default key fail-fast (5')
- QF-10: Setup golden vector test password (20')

## 9. Cách work với AI assistant này

1. **Trước khi gen code**: paste section liên quan từ `AUDIT_REPORT.md` (vd. F-01, P0.2) → context cụ thể.
2. **Yêu cầu output**: ưu tiên **minimal diff** thay vì rewrite cả file. Mỗi PR 1 finding.
3. **Sau khi gen code**: chạy `dotnet test` + check golden vector → merge.
4. **Tránh**: yêu cầu AI tự đoán file path — đưa absolute path từ section §3 của report.

## 10. Liên hệ / Q&A

Nếu cần clarify business rule, hỏi user trực tiếp về:
- Deadline edge case (đã clarify ở §3 trên)
- Multi-step approval workflow (chưa có, future)
- Quota model (chưa có, future)
- AI module customization

---

**File audit gốc**: `/opt/gtas_vpp/audit-report/AUDIT_REPORT.md` (1900 dòng — paste hết hoặc paste section cụ thể tuỳ context limit).

**Last updated**: May 2026 — sau khi user xác nhận deadline definition, scale 1000 user, timeline 3 tháng luận văn.
