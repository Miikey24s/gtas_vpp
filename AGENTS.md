# Hướng dẫn làm việc với repository GTAS VPP

Áp dụng cho AI agent và lập trình viên trong toàn bộ repository.

## Mô hình AI-first

- `docs/ai/AI-AGENT-OPERATING-MODEL.md` là bản đồ context, skill, MCP, plan, kiểm thử và handoff.
- Trước task phức tạp, chạy `./scripts/gtas.cmd preflight -Scope <all|frontend|backend|tests|thesis>`.
- Đọc `AGENTS.md` gần file đang sửa nhất; instruction theo thư mục được ưu tiên hơn root.
- Workflow lặp lại nằm trong `.agents/skills/`: UI, database safety và luận văn DOCX.
- Model, reasoning, approval, sandbox, MCP credential và browser profile là cấu hình local/user; không commit vào Git.
- Plan task phức tạp dùng hai tầng: bản một ánh nhìn trước, có link đến execution record chi tiết; task nhỏ chỉ cần bản ngắn.

## Phạm vi chuẩn

- Backend: `src/Backend/`; frontend chính: `src/Frontend/Blazor/`; shared DTO duy nhất: `src/Shared/`.
- `gtas_vpp_fe_react/` chỉ giữ dependency Playwright cho tooling LVTN; React POC nằm ở tag `archive/react-poc-2026-07-27`.
- Khi sửa backend, frontend, tests hoặc LVTN, đọc scoped `AGENTS.md` tương ứng.
- Không sao chép DTO shared vào frontend và không sửa API/database/nghiệp vụ chỉ để khớp Atlas hoặc luận văn.
- Identifier giữ tiếng Anh theo convention. Comment source mới hoặc được chạm trong scope viết tiếng Việt ngắn gọn, chỉ giải thích nghiệp vụ khó đoán.
- Không hoàn tác, ghi đè, stage hoặc commit thay đổi ngoài scope của người dùng.

## Workflow thay đổi

1. Nếu phiên vừa bị stop/resume/compact, phục hồi goal, plan, yêu cầu gần nhất, branch, `git status`, diff, process nền và commit gần nhất; tiếp tục phần dang dở trừ khi owner thay thế mục tiêu.
2. Đọc architecture/plan, code, test và implementation tương tự trước khi sửa.
3. Làm vertical slice nhỏ, giữ repository chạy được và không để placeholder thay chức năng thật.
4. Chạy test hẹp trong vòng lặp; chạy `./scripts/gtas.cmd verify -Scope <scope>` khi change-set hoàn chỉnh.
5. Rà toàn bộ diff, stage đúng file và tạo commit local có scope rõ. Push/PR/merge/deploy chỉ khi user yêu cầu rõ.

Không coi task cũ đã xong chỉ vì turn bị ngắt. Trước khi đổi hướng, báo checkpoint đã kiểm chứng, phần còn lại và next exact action; không làm lại phần đã có evidence hoặc skip scope chưa hoàn tất.

## Frontend authority

- Blazor/Radzen là frontend chính; đọc `src/Frontend/Blazor/AGENTS.md` và living plan trước mọi thay đổi UI.
- `docs/design/atlas/` là design reference read-only; browser Blazor thật là visual authority cuối.
- Giữ một cây global `InteractiveServer`; không thêm `@rendermode` cục bộ nếu chưa có quyết định kiến trúc mới.
- Nếu Radzen MCP hết quota hoặc key lỗi, dừng phần phụ thuộc Radzen và yêu cầu key mới.

## Build và kiểm thử

```powershell
./scripts/gtas.cmd test
./scripts/gtas.cmd verify -Scope all
```

Không coi số lượng test lịch sử là invariant; luôn báo kết quả từ output hiện tại.

## Luận văn

- Nguồn chuẩn duy nhất là `LVTN/NguyenAnNam_DH52201078.docx`; đọc `LVTN/AGENTS.md` và `LVTN/README.md` trước khi làm.
- Chỉ chỉnh review copy bị Git ignore; chỉ thay nguồn chuẩn sau owner approval và render-and-verify.
- Không commit checkpoint/render/contact sheet hoặc xóa Word/tooling/diagram/screenshot chỉ vì chúng không tham gia build ứng dụng.

## Bảo mật và Git

- Không commit `.env`, secret, token, cookie, connection string cá nhân, tài khoản test hoặc browser storage.
- Không commit `bin/`, `obj/`, `TestResults/`, log, cache, downloaded tool, render hoặc audit output tạm.
- Migration destructive, hard delete, production mutation và rewrite history cần quyền rõ, preflight và recovery phù hợp.
- Trước commit, chạy `git diff --check`, xem toàn bộ status/diff và chỉ stage đúng scope.
