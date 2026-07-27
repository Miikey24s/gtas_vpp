# GTAS VPP — AI Agent Operating Model

> Nguồn định hướng cho repository gần như 100% AI-generated. File này mô tả cách cung cấp context và workflow; quy tắc bắt buộc vẫn nằm trong `AGENTS.md` gần phạm vi đang sửa nhất.

## 1. Mục tiêu

- Agent mới hiểu đúng kiến trúc mà không phải đọc toàn repository.
- Một quyết định chỉ có một nguồn thật; adapter của từng nền tảng không sao chép cả bộ luật.
- Task dài có plan, evidence, test và continuation note đủ để đổi agent hoặc mất context.
- Thay đổi UI được phát triển từ design system và route thật, không chuyển screenshot thành code một lần.
- Công cụ ngoài repo chỉ được nối qua MCP khi thực sự cần dữ liệu hoặc hành động live.

## 2. Kiến trúc context

| Lớp | Vị trí | Vai trò |
|---|---|---|
| Luật toàn repo | `AGENTS.md` | Cấu trúc, authority, safety, lệnh chuẩn và definition of done |
| Luật theo phạm vi | `src/Frontend/Blazor/AGENTS.md`, `src/Backend/AGENTS.md`, `tests/AGENTS.md`, `LVTN/AGENTS.md` | Chỉ tải quy tắc cần cho vùng đang làm |
| Workflow lặp lại | `.agents/skills/` | Progressive disclosure: chỉ tải hướng dẫn đầy đủ khi task khớp skill |
| Plan dài hạn | `docs/planning/05-EXECUTION-TEMPLATE.md`, `docs/execution/` | Scope, trạng thái, evidence, blocker và continuation |
| Quyết định UI | `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md` | Design contract, owner feedback, route ledger và retrofit queue |
| Tool selection | `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` | Chọn MCP/browser/QA đúng vai trò |
| Adapter agent | `CLAUDE.md`, `GEMINI.md`, `.github/copilot-instructions.md` | Trỏ về nguồn chuẩn và bổ sung hành vi riêng của nền tảng |
| Goal đang hoạt động | Goal của task/thread | Outcome dài hạn còn dang dở; không thay thế plan hoặc Git evidence |
| Memory | Memory theo user/session | Preference và context đã học; chỉ là gợi ý khi sự thật có thể drift |

Không đưa toàn bộ architecture hoặc business rules vào prompt. Prompt chỉ cần `Goal`, `Context`, `Constraints` và `Done when`; agent tự đọc nguồn chuẩn được trỏ từ `AGENTS.md`.

Thuật ngữ dùng trong tài liệu:

- **Authority:** nguồn quyết định khi hai nguồn mâu thuẫn.
- **Progressive disclosure:** chỉ nạp hướng dẫn chi tiết khi task thực sự cần, giúp context ngắn và ít nhiễu.
- **Vertical slice:** một lát cắt chức năng hoàn chỉnh từ UI/API đến test, có thể kiểm chứng độc lập.
- **Adapter:** file cầu nối ngắn cho từng agent, chủ yếu trỏ về nguồn chung.
- **Gate:** điều kiện kiểm tra bắt buộc trước khi chuyển bước hoặc bàn giao.

## 3. Vòng đời task chuẩn

1. Nếu phiên vừa `startup`, `resume` hoặc compact context, chạy resume preflight: đọc goal, plan chưa xong, yêu cầu gần nhất, branch, status/diff, process nền và commit gần nhất.
2. Phân loại tin nhắn mới là thay thế, bổ sung, hỏi trạng thái hoặc tiếp tục. Không bỏ task cũ nếu owner chưa thay mục tiêu.
   - Nếu một prompt chứa nhiều task, sắp theo dependency, conflict, rủi ro và approval gate. Chỉ thay đổi đáng kể thứ tự sau khi báo owner và được duyệt; nếu thứ tự hiện tại ổn thì tiếp tục ngay.
3. Chạy `./scripts/gtas.cmd preflight -Scope <scope>`.
4. Đọc root và scoped `AGENTS.md`, status/diff, tài liệu authority và implementation tương tự.
5. Với task phức tạp, tạo/cập nhật execution record từ template; task nhỏ dùng plan trong thread.
6. Chọn đúng skill và MCP. Không gọi nhiều MCP cùng chức năng.
7. Triển khai theo vertical slice có thể build/test/browser-verify độc lập.
8. Chạy test hẹp trong vòng lặp; chạy `./scripts/gtas.cmd verify -Scope <scope>` trước commit khi phạm vi đủ hoàn chỉnh.
9. Rà diff, stage đúng file, commit local theo scope. Push/PR/deploy chỉ khi người dùng yêu cầu rõ.
10. Nếu chưa xong, ghi continuation note với branch, HEAD, dirty files, gate gần nhất và next exact action.

### Khôi phục sau gián đoạn

- Task dài dùng persisted goal khi owner yêu cầu; goal chỉ complete khi outcome và verification đều đạt.
- `.codex/hooks.json` gọi hook read-only ở `startup|resume|compact` để hiển thị branch, working-tree state và commit gần nhất. Hook chỉ là lời nhắc khôi phục, không thay thế goal/plan/Git.
- Sau khi hook chạy, agent phải kiểm tra goal/plan thực tế, báo phần đã xong và phần chưa xong rồi tiếp tục đúng next action.
- Nếu task bị ngắt trước khi có edit, ghi rõ “mới audit, chưa sửa”; không suy diễn từ số tool call hoặc plan status.

### Plan hai tầng

Với task phức tạp, plan dùng **progressive disclosure** (chỉ mở chi tiết khi cần):

1. **Bản một ánh nhìn:** vừa một màn hình, có mục tiêu, scope, 3–7 bước chính, gate kiểm tra, rủi ro/blocker và next action. Thuật ngữ khó được chú thích ngắn ngay tại chỗ.
2. **Bản chi tiết:** execution record đầy đủ theo `docs/planning/05-EXECUTION-TEMPLATE.md`.

Bản một ánh nhìn phải link đến đúng mục trong bản chi tiết. Khi tiến độ đổi, cập nhật summary trước; không bắt owner đọc lại toàn bộ record. Task nhỏ hoặc ít rủi ro chỉ dùng bản một ánh nhìn nếu bản chi tiết không tạo thêm giá trị.

### Biên nhận ghi nhận

Sau mỗi lần owner review, sửa cách làm hoặc custom agent, phản hồi kế tiếp phải cho biết thay đổi đã được ghi ở đâu thay vì chỉ nói “đã nhớ”:

| Nhãn | Dùng khi | Hiệu lực |
|---|---|---|
| `THREAD` | Chỉ hiểu trong task hiện tại, chưa ghi file | Mất khi context không còn |
| `GOAL` | Outcome dài hạn đang active | Theo task đến khi complete/clear |
| `MEMORY` | Preference hoặc context cần dùng qua phiên | Cross-session nhưng phải nhường repository evidence |
| `GLOBAL AGENTS` | Cách giao tiếp/làm việc áp dụng mọi repo | Mọi project trên máy/user profile |
| `REPO AUTHORITY` | Architecture, command, product rule hoặc definition of done | Repository và scope tương ứng |
| `SKILL` | Workflow lặp lại có procedure/reference/script | Tự kích hoạt khi task khớp description |
| `SCRIPT/CI/HOOK/AUTOMATION` | Kiểm tra hoặc lifecycle có pass/fail ổn định | Tự động/enforced theo trigger |
| `PLUGIN` | Nhóm capability ổn định cần cài/phân phối | Các surface/provider hỗ trợ plugin |

Biên nhận dùng bốn cột `Nội dung`, `Đã lưu ở`, `Phạm vi/hiệu lực`, `Bằng chứng`. Chỉ ghi `đã lưu` sau khi write/validation thành công; ý tưởng đang chờ quyết định phải ghi `PENDING APPROVAL`.

Nếu agent phản biện một giải pháp của owner, phải nêu evidence, phương án khuyến nghị và tác động, sau đó chờ owner duyệt trước khi triển khai hướng khác. Audit read-only và phần độc lập vẫn có thể tiếp tục.

## 4. Prompt contract cho owner

Prompt ngắn vẫn đủ nếu có bốn phần:

```text
Goal: Kết quả sản phẩm cần đạt.
Context: Route/module/file hoặc ảnh tham chiếu quan trọng.
Constraints: Phạm vi không được chạm, dữ liệu TEST, design/architecture authority.
Done when: Build/test/browser/commit hoặc bằng chứng cần có.
```

Không cần lặp lại build commands, Git safety, UI authority hoặc LVTN rules vì các phần đó đã là durable guidance.

## 5. Skill strategy

- Skill repo đặt tại `.agents/skills/<name>/SKILL.md`.
- Chỉ tạo skill cho workflow lặp lại hoặc kiến thức dễ bị agent hiểu sai.
- `SKILL.md` giữ quy trình ngắn; tài liệu dài tiếp tục nằm trong `docs/` và được skill trỏ tới.
- Không tạo plugin khi workflow mới chỉ dùng trong GTAS VPP. Plugin chỉ phù hợp khi cần phân phối nhiều skill hoặc MCP cho dự án khác.
- Repo skills hiện hành:
  - `.agents/skills/gtas-vpp-ui-system/` cho Blazor/Radzen và browser QA.
  - `.agents/skills/gtas-vpp-db-safety/` cho schema, migration, stored procedure và data repair.
  - `.agents/skills/gtas-vpp-thesis-docx/` cho Word, field/link và render review.

## 6. MCP và cấu hình theo máy

| Nhu cầu | Nguồn ưu tiên |
|---|---|
| .NET/Blazor/Aspire | Microsoft Learn MCP |
| Radzen API/property/event | Radzen MCP, truy vấn theo từng component |
| Browser route thật | Playwright MCP |
| Performance/network sâu | Chrome DevTools MCP |
| Figma design context | Figma MCP |
| GitHub live state | GitHub connector hoặc `gh` theo workflow |

MCP token, API key, account authorization, browser profile và database credential luôn ở local/user scope. Không commit `.mcp.json`, `.claude.json`, `.env`, cookie hoặc storage state.

Không có `.codex/config.toml` trong repository là quyết định có chủ đích: model, reasoning, sandbox và approval là preference theo người/máy; MCP đang có credential local. Repository chỉ theo dõi `.codex/hooks.json` và script hook read-only phục vụ khôi phục task; chỉ thêm project config khi xuất hiện một default ổn định, không nhạy cảm và có thể kiểm chứng trên mọi máy.

Không thêm hook hoặc custom agent chỉ vì Codex hỗ trợ chúng. Hook resume hiện tại được giữ vì read-only, chạy nhanh và giải quyết lỗi lặp lại; hook không phải security boundary. Chỉ thêm hook khác sau khi script deterministic đã ổn định; chỉ thêm custom agent khi có vai trò độc lập, eval chứng minh lợi ích và mỗi agent có worktree riêng.

## 7. Verification ladder

| Mức | Dùng khi | Gate |
|---|---|---|
| Focused | Một file/module | Project build hoặc test trực tiếp |
| Slice | Một vertical slice | Build + unit tests liên quan + integration/browser phù hợp |
| Repository | Trước commit lớn/handoff | `./scripts/gtas.cmd verify -Scope <scope>` |
| UI approved | Sau owner review | Route thật, responsive, accessibility và visual baseline ổn định |

Build/unit pass không chứng minh UI đúng. Screenshot chưa được owner duyệt chỉ là evidence, không phải visual regression baseline.

## 8. Cách duy trì context

- Quyết định dài hạn phải vào source authority trong repo, không chỉ nằm trong chat hoặc memory của một agent.
- Khi cùng lỗi lặp lại hai lần, cập nhật `AGENTS.md`, skill hoặc deterministic script phù hợp.
- Không ghi snapshot test count, port hoặc trạng thái branch tạm thành luật vĩnh viễn.
- Adapter nền tảng phải ngắn; nếu nội dung trùng nguồn chuẩn, thay bằng đường dẫn/import.
- Review định kỳ các instruction và xóa luật đã superseded để tránh agent nhận context mâu thuẫn.
- `scripts/ai/Test-AgentSetup.ps1` là deterministic lint cho instruction graph; `docs/ai/AI-AGENT-EVALS.md` là bộ bài đánh giá hành vi, không thay thế product tests.
