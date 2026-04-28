Bạn là AI Scanner (Gemini) trong hệ thống AI Integration cho dự án GTAS VPP.

# Nhiệm vụ: Review + Final Sign-off cho Phase 8

## Đọc trước
- `.ai_workspace/ai_integration/architecture.md`
- `.ai_workspace/ai_integration/tasks.json`
- Tất cả reports trong `.ai_workspace/ai_integration/reports/`

## Checklist Review

### 1. Architecture Compliance
- [ ] `gtas_vpp_be.AI` KHÔNG reference FE project
- [ ] AI interfaces nằm trong `gtas_vpp_be.Service/AI/`
- [ ] Implementations nằm trong `gtas_vpp_be.AI/`
- [ ] Shared DTOs nằm trong `gtas_vpp_shared/DTOs/AI/`
- [ ] DI lifecycle đúng (IChatClient=Singleton, Orchestrator=Scoped, etc.)

### 2. Code Quality
- [ ] Tất cả async methods có CancellationToken
- [ ] Error handling: không throw cho AI failures
- [ ] Logging: structured format
- [ ] Naming convention đúng theo architecture.md

### 3. Security
- [ ] AIController endpoints có [Authorize]
- [ ] Health check endpoint: [AllowAnonymous] OK
- [ ] Không leak connection string / API keys

### 4. Build & Test
- [ ] `dotnet build gtas_vpp/gtas_vpp.slnx` — 0 errors, 0 warnings (trừ existing)
- [ ] `dotnet test code-be/gtas_vpp_be.Tests/` — all pass
- [ ] Swagger shows /api/ai/* endpoints

### 5. Frontend
- [ ] AIChatBox component scoped CSS (không ảnh hưởng global)
- [ ] FE chỉ reference gtas_vpp_shared (không reference BE)
- [ ] HttpClient dùng JWT auth
- [ ] Responsive: chatbox không che nội dung chính

## Output
Ghi kết quả review vào `.ai_workspace/ai_integration/reports/final_review.md`

Nếu có issue → liệt kê rõ + suggest fix.
Nếu PASS tất cả → cập nhật README.md status.
