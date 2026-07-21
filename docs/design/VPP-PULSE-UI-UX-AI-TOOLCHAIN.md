# VPP Pulse — UI/UX AI Agent Toolchain

> **Trạng thái:** `ADOPTED — UPDATED 2026-07-21`
>
> **Mục tiêu:** Giúp AI agent hiểu đúng source, dữ liệu, layout, accessibility, performance và data story của GTAS VPP mà không tạo một UI Lab hoặc toolchain trùng lặp.

---

## 1. Nguyên tắc chọn công cụ

1. Tài liệu chính chủ trước, nguồn tổng hợp sau.
2. Browser runtime thật của frontend đang được triển khai là visual authority; Figma là design/research context, không tự thắng code đã được owner duyệt.
3. Mỗi công cụ có một vai trò rõ. Không cài nhiều MCP cùng đọc browser hoặc cùng tra docs nếu không tạo thêm bằng chứng.
4. Công cụ tự động phát hiện regression; quyết định UX và data storytelling vẫn cần người review.
5. Browser test, trace và screenshot chỉ dùng TEST/isolated fixture, không dùng profile cá nhân hoặc production secret.

---

## 2. Bộ MCP được chọn

| Công cụ | Vai trò trong GTAS VPP | Trạng thái | Quy tắc dùng |
|---|---|---|---|
| Microsoft Learn MCP | Tài liệu chính chủ .NET, Blazor, ASP.NET Core, Aspire và Microsoft accessibility | Đã cài global | Tra trước khi quyết định API/kiến trúc Microsoft có thể thay đổi theo phiên bản |
| Sosumi Apple Docs MCP | Apple Developer Documentation, Human Interface Guidelines (HIG) và WWDC ở dạng Markdown cho AI | Đã cài global, kết nối HTTP không cần API key | Nguồn tham khảo trực tiếp cho visual hierarchy, clarity, spacing, feedback và accessibility; đây là dịch vụ mã nguồn mở không chính thức, mọi kết luận quan trọng vẫn đối chiếu URL Apple gốc |
| shadcn MCP | Tìm, đọc và cài component/block từ shadcn-compatible registries | Cài global khi khởi tạo `gtas_vpp_fe_react` | Dùng trước khi thêm shadcn component; component được đưa thành source trong repo để AI đọc/sửa, không xem registry item là code đã được duyệt tự động |
| Radzen Blazor MCP | Component, property, event, DataGrid, Dialog, validation và theme Radzen | Đã cài | Bắt buộc tra trước khi sửa Radzen; hết quota/key thì dừng toàn bộ công việc và chờ key mới |
| Playwright MCP | DOM/ARIA snapshot, thao tác route thật, viewport, screenshot, console và request lỗi | Đã cài | Công cụ browser mặc định cho agent; không dùng `networkidle` làm điều kiện duy nhất với Blazor Server |
| Chrome DevTools MCP | Console/network chuyên sâu, source-mapped error và performance trace | Đã cài global | Chỉ dùng Chrome/Chrome for Testing profile riêng; telemetry và CrUX đã tắt trong config |
| Figma MCP | Flow, variable, component, design context và capture live UI sang Figma khi cần | Đã cài OAuth | Không dùng Figma làm pixel/code authority; Starter/View seat có thể bị giới hạn tool call |
| Context7 MCP | Tài liệu package bên thứ ba hiện hành | Đã cài | Chỉ dùng sau tài liệu chính chủ hoặc khi package không có MCP/docs tốt hơn |
| SQL GTAS MCP | Schema/data evidence phục vụ UI và data story | Đã cài local | Không mutation production; ưu tiên query có scope và dữ liệu TEST |

Config được thêm vào Codex user profile:

```text
microsoft-learn  https://learn.microsoft.com/api/mcp
sosumi          https://sosumi.ai/mcp
shadcn          npx shadcn@latest mcp
chrome-devtools  npx -y chrome-devtools-mcp@latest --no-usage-statistics --no-performance-crux
```

Sau khi cài MCP mới, mở task Codex mới hoặc restart Codex nếu tool chưa xuất hiện trong phiên hiện tại.

Sosumi dùng Streamable HTTP, chỉ đọc tài liệu và không lưu secret trong repository. Apple có MCP chính thức qua Xcode, nhưng cơ chế đó yêu cầu macOS/Xcode nên không dùng được trên máy Windows hiện tại. GTAS VPP dùng Sosumi để truy xuất HIG mới theo yêu cầu; browser Blazor thật và QA của dự án vẫn là implementation authority.

### Reference stack cho quyết định UI

- **Figma Make/Opus:** được tự nghiên cứu và chọn art direction cho prototype; không bị khóa bởi token/style cũ.
- **Owner feedback + GTAS runtime:** nguồn quyết định cuối sau khi prototype đã có để review trực quan.
- **Apple HIG, ChatGPT, Notion, Linear và Figma:** nguồn nghiên cứu tùy chọn, không có nguồn nào là art direction độc quyền hoặc mẫu phải sao chép.
- **Radzen + Microsoft:** nguồn quyết định khả năng triển khai Blazor/Radzen đúng component/framework đang dùng.
- **React/Tailwind/shadcn docs:** nguồn quyết định implementation React; code shadcn được đưa vào repository và phải được review/test như source sở hữu.
- **W3C/Deque:** nguồn quyết định accessibility; không được hy sinh để bắt chước một visual reference.
- **Carbon/Tableau/Microsoft data guidance:** chỉ dùng khi cần chọn table, comparison, KPI hoặc data storytelling; không mang nguyên art direction của sản phẩm khác vào GTAS.

### Figma Make / Opus 4.8 Build

- Dùng `Guidelines.md` làm context định tuyến, `gtas_vpp_fe_react/Guidelines.md` làm invariant tối thiểu và `VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md` làm bối cảnh sản phẩm.
- Khi tài khoản có Import GitHub/Code on Canvas, ưu tiên import `Miikey24s/gtas_vpp`, chọn `Nam`, root `gtas_vpp_fe_react`, tạo branch `figma/*` và dùng `VPP-PULSE-FIGMA-CODEBASE-STARTER-PROMPT.md` để Opus đọc source thật.
- Chỉ khi import không khả dụng mới upload `VPP-PULSE-FIGMA-MAKE-CONTEXT.md` và dùng prompt fallback trong `VPP-PULSE-FIGMA-MAKE-STARTER-PROMPT.md`.
- Với app phức tạp, làm theo phase và screen/workspace thay vì một prompt full end-to-end. Context attachment được giữ xuyên conversation; prompt tiếp theo chỉ giao workspace/state cần mở rộng sau owner review.
- Bắt đầu phiên Make mới khi source/context cũ đã lỗi thời; không tiếp tục từ chat hoặc code snapshot cũ.
- Với imported codebase: xác minh latest `origin/Nam`, tạo `figma/*`, review diff/test và chỉ mở PR sau owner review. Nút GitHub Push của Make file trống vẫn là luồng một chiều sang repository do Make tạo, không thay thế import codebase thật.
- Attachment chỉ gồm file cần cho route hiện tại, nêu rõ file nào là authority hay inspiration; không tải secret, cookie, token, connection string hoặc dữ liệu production.

### Vì sao không cài thêm browser MCP khác

Playwright MCP đã phù hợp cho interaction và accessibility tree; Chrome DevTools MCP bổ sung đúng phần còn thiếu là trace, console và network sâu. BrowserTools, Puppeteer MCP hoặc các MCP screenshot khác hiện chỉ tạo vai trò trùng, tăng context và bề mặt bảo mật.

---

## 3. Quality stack trong repository

### 3.1 Functional và responsive

- Giữ `gtas_vpp_fe.UITests` dùng Microsoft Playwright for .NET và isolated Aspire/LocalDB.
- Test route thật qua DTO/API/database fixture, không tạo UI mock khác contract production.
- Matrix tối thiểu: `390×844`, `768×1024`, `1366×768`, `1920×1080`; VI/EN; Light/Dark; Print khi route có in/xuất evidence.
- Thu console error, page error, failed request và Blazor reconnect state trước khi kết luận pass.

### 3.2 Accessibility

- Tự động: `Deque.AxeCore.Playwright` 4.12.0 chính chủ Deque, chạy WCAG 2 A/AA/2.1/2.2 phù hợp trên trạng thái UI đang hiện.
- Package chỉ nằm trong UI test: package Playwright là MIT, dependency `Deque.AxeCore.Commons` là MPL-2.0; không đưa bundle test vào ứng dụng production.
- Gate route: không có violation `critical` hoặc `serious`; exception phải ghi nguyên nhân và retrofit owner.
- Thủ công: Accessibility Insights for Web FastPass trước khi khóa route/shared primitive; kiểm tra tab order, keyboard trap và focus visible.
- Mỗi dialog, menu, drawer hoặc validation state phải được mở ra rồi scan lại vì axe không kiểm tra phần chưa render/đang ẩn.
- Axe tự động chỉ phát hiện một phần lỗi WCAG; không thay keyboard, zoom, screen reader và review nội dung.

### 3.3 Visual regression

- Khi route còn `OWNER_REVIEW`: chỉ lưu screenshot evidence, không tạo golden baseline.
- Sau khi owner chuyển route thành `APPROVED`: khóa baseline ở browser, OS/font, viewport, locale, theme, animation và deterministic fixture cố định.
- Phương án phù hợp với C# hiện tại: `Verify.Playwright` cho screenshot approval/diff. Chỉ thêm package và baseline khi route đầu tiên được duyệt để tránh snapshot churn.
- Không dùng SaaS Percy, Applitools hoặc Chromatic ở giai đoạn hiện tại: có upload/cost và không phù hợp quyết định không tạo UI Lab. Có thể xem lại khi cần CI đa trình duyệt hoặc teamwork.

### 3.4 Performance và stability

- Chrome DevTools MCP dùng trong vòng lặp phát triển để tìm long task, request chậm, layout shift và asset/font lỗi.
- Lighthouse dùng audit nhanh cho login/public shell; Lighthouse CI chỉ thêm ở W8/CI khi có URL/fixture xác định và budget ổn định.
- Lighthouse CI phù hợp để chặn regression performance, accessibility, best practices và budget asset, nhưng không thay functional test hoặc Blazor circuit smoke test.

### 3.5 ARIA/semantic regression

- Playwright locator ưu tiên role/name thay CSS selector khi kiểm tra hành vi người dùng.
- Với navigation, dialog, form và data story ổn định, bổ sung ARIA snapshot để phát hiện semantic drift dù pixel gần như không đổi.
- CSS selector vẫn dùng cho contract visual nội bộ, nhưng phải có tên semantic hoặc test id ổn định nếu người dùng không thể quan sát class đó.

---

## 4. Data storytelling review contract

Không có MCP nào tự quyết định một dashboard kể chuyện tốt. Trước khi duyệt một visual, agent phải trả lời đủ:

| Câu hỏi | Bằng chứng cần có |
|---|---|
| Người dùng đang ở scope/kỳ nào? | Context chỉ xuất hiện một nguồn rõ ràng |
| Kết luận chính là gì? | Takeaway đọc được trước chart/table |
| Vì sao kết luận đó đúng? | Metric/comparison có đơn vị, thời gian và dữ liệu nguồn |
| Người dùng nên làm gì tiếp? | Một primary action theo permission/state thật |
| Thông tin nào đang lặp? | Không lặp deadline, trạng thái, kỳ hoặc CTA nếu không thêm nghĩa |
| Nếu không có dữ liệu thì sao? | Phân biệt empty dataset, filter miss, unauthorized và error |
| VI/EN có cùng nghĩa không? | Resource, DB display data, export và UI test dùng cùng terminology contract |
| Mobile/print còn giữ câu chuyện không? | Thứ tự takeaway → evidence → action không bị đảo hoặc mất |

Chart chỉ được thêm khi nó trả lời câu hỏi tốt hơn số, bảng hoặc câu takeaway. Mọi chart phải có title, unit, time/filter context, accessible fallback và drill-down/table khi cần.

---

## 5. Workflow cho mỗi route

1. Đọc living plan, route source, DTO/API, permission và data fixture.
2. Tra đúng MCP tài liệu trước khi sửa component/framework.
3. Dùng Playwright/Chrome DevTools để chụp baseline, DOM/ARIA, console/network và performance evidence.
4. Viết route brief theo `task → takeaway → evidence → action → states`.
5. Sửa trực tiếp frontend đang được giao: Blazor dùng vòng lặp `dotnet watch`; React dùng Vite/Aspire và shared OpenAPI contract. Không trộn hai frontend trong cùng visual change-set.
6. Chạy unit/architecture test, Playwright responsive/functional và axe phù hợp với frontend được sửa.
7. Owner review runtime.
8. Chỉ sau `APPROVED` mới tạo visual golden baseline và commit route.

---

## 6. Nguồn chính chủ đã đối chiếu

- [Microsoft Learn MCP Server](https://learn.microsoft.com/en-us/training/support/mcp-get-started)
- [Apple — Giving external agents access to Xcode](https://developer.apple.com/documentation/Xcode/giving-external-agents-access-to-xcode)
- [Sosumi — Apple Docs for LLMs](https://sosumi.ai/)
- [shadcn/ui MCP Server](https://ui.shadcn.com/docs/mcp)
- [Chrome DevTools MCP](https://github.com/ChromeDevTools/chrome-devtools-mcp)
- [Playwright accessibility testing](https://playwright.dev/docs/accessibility-testing)
- [Playwright visual comparisons](https://playwright.dev/docs/test-snapshots)
- [Deque axe-core](https://github.com/dequelabs/axe-core)
- [Deque.AxeCore.Playwright](https://www.nuget.org/packages/Deque.AxeCore.Playwright)
- [Accessibility Insights for Web](https://accessibilityinsights.io/docs/web/overview/)
- [Lighthouse CI](https://github.com/GoogleChrome/lighthouse-ci)
- [Figma MCP Server](https://help.figma.com/hc/en-us/articles/32132100833559-Guide-to-the-Dev-Mode-MCP-Server)
- [Figma Code Connect](https://help.figma.com/hc/en-us/articles/23920389749655-Code-Connect)
- [Figma Make guidelines](https://help.figma.com/hc/en-us/articles/33665861260823-Add-guidelines-to-Figma-Make)
- [Figma Make attachments](https://help.figma.com/hc/en-us/articles/31304529835671-Attach-designs-and-images-to-a-prompt)
- [Figma Make plan-first workflow](https://help.figma.com/hc/en-us/articles/35710574222487-Beyond-the-basics-Using-Figma-Make)
- [Figma Make on local code](https://www.figma.com/blog/figma-make-now-on-your-local-code/)
- [Code on the Figma Canvas](https://www.figma.com/blog/code-on-the-figma-canvas/)
- [Tailwind CSS source detection](https://tailwindcss.com/docs/detecting-classes-in-source-files)
