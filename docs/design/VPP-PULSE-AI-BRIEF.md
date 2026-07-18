# VPP Pulse — AI Product, Architecture & UX Brief

> **Trạng thái:** Design/review only — chưa bật AI release và chưa sửa production code
> **Ngày:** 2026-07-18
> **Figma:** [11 — AI & Intelligence](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE)

## 1. Kết luận nên chọn

GTAS VPP nên dùng **server-side AI gateway + OpenAI Responses API**, với **local deterministic provider cho test/offline**, và giữ một interface `IAiProvider` để có thể thêm local model sau này. Không đưa API key xuống browser, không cho AI truy vấn DB tùy ý và không cho AI thực hiện mutation nghiệp vụ.

Đây là hybrid theo nghĩa vận hành:

| Tầng | Vai trò | Quyết định |
|---|---|---|
| Browser/Blazor | Hiển thị insight, evidence, trạng thái và feedback | Không chứa secret, không gọi provider trực tiếp |
| Backend gateway | Authorization, aggregate DTO, prompt/schema, budget, cache, audit, fallback | Nguồn kiểm soát duy nhất |
| Provider chính | Narrative/anomaly explanation chất lượng cao | Responses API, `store=false` mặc định |
| Provider test/offline | Test ổn định, không tốn phí, chạy khi mất mạng | Deterministic stub; local model chỉ là adapter tùy chọn |

OpenAI khuyến nghị Responses API cho tích hợp mới, Structured Outputs cho JSON theo schema, và function calling chỉ khi ứng dụng cần thực thi tool có kiểm soát. VPP v1 chỉ cần Structured Outputs; chưa cần tool calling.

## 2. Phạm vi AI được phép

### Release feature duy nhất có giá trị cao

1. **Report narrative:** viết tóm tắt tiếng Việt từ KPI/aggregate đã được phân quyền.
2. **Anomaly explanation:** giải thích vì sao một chỉ báo nổi bật, kèm evidence ID và mức tin cậy.
3. **Ask the report (read-only):** hỏi trong một report scope; câu trả lời chỉ lấy từ aggregate DTO và phải trỏ về block dữ liệu nguồn.

AI không được:

- duyệt/từ chối đơn, đóng kỳ, chọn nhà cung cấp cuối cùng hoặc sửa dữ liệu;
- tự gọi SQL, tự mở rộng scope, đọc raw PII hoặc suy ra quyền từ prompt;
- thay thế KPI deterministic; số liệu hiển thị phải lấy từ report service;
- tạo câu trả lời không có evidence hoặc che giấu trạng thái fallback/refusal.

## 3. Ranh giới dữ liệu

Backend tạo một DTO tối thiểu, ví dụ:

```text
ReportInsightInput
  periodCode, scopeCode, generatedAt
  kpis: [{ key, label, value, unit, delta, baseline }]
  anomalies: [{ key, metric, current, baseline, delta, severity, evidenceRefs }]
  allowedEvidenceRefs: [{ id, label, route }]
```

Không gửi username, email, access token, mật khẩu, connection string, raw order note, địa chỉ hay dữ liệu nhân sự nếu không cần để trả lời. `evidenceRefs` là ID/route đã được backend kiểm tra, không phải câu SQL.

Source of truth:

- số lượng, tiền, tỷ lệ, period state: **deterministic report service**;
- câu chữ, nhóm điểm nổi bật, diễn giải khả dĩ: **AI**;
- nếu AI timeout, refusal, invalid schema hoặc provider lỗi: **rule-based fallback**.

## 4. API key, secret và môi trường

- Local: `dotnet user-secrets` hoặc environment variable; không ghi vào `appsettings*.json`, Figma, log hay Git.
- CI/DO: secret store của pipeline/host; staging và production dùng project/key riêng, quota riêng.
- Browser không bao giờ biết key. Endpoint AI phải kiểm tra authenticated user, role và report scope ở server.
- Admin UI chỉ cho xem provider/model/budget/feature flag/audit; **không cho xem hoặc nhập API key**.
- Đổi key phải là thao tác hạ tầng, có rotation/runbook, không phải UI feature.

OpenAI ghi rõ API key phải được bảo vệ bằng environment/secret management và nên tách staging/production project. Dữ liệu API không dùng để train nếu không opt-in; tuy vậy ứng dụng vẫn phải tự quyết retention/audit của mình. Với Responses API, `store=false` là mặc định của VPP để tránh lưu state phía provider khi không cần.

## 5. OpenAI API vs local model

| Phương án | Ưu điểm | Nhược điểm | Quyết định cho VPP |
|---|---|---|---|
| OpenAI API | Chất lượng/latency ổn định, không phải vận hành GPU, phù hợp demo và report narrative | Có phí, phụ thuộc mạng/provider, cần data boundary và quota | **Primary cho Test/Demo/DO khi feature được bật** |
| Local deterministic | Test lặp lại, không phí, không leak dữ liệu, chạy được trong CI | Không phải LLM thật; không đánh giá prompt/quality | **Bắt buộc cho test và fallback** |
| Local gpt-oss/LLM | Kiểm soát dữ liệu, chạy trên hạ tầng mình quản lý, có thể offline | Cần RAM/GPU, vận hành model/runtime, đo quality tiếng Việt, latency và license/hardware | **Chỉ thêm sau benchmark; không dùng mặc định** |
| Browser direct | Dễ demo nhanh | Lộ key, bypass authorization, khó audit/cost control | **Không dùng** |

OpenAI công bố gpt-oss-20b có thể chạy với khoảng 16 GB memory và gpt-oss-120b nhắm tới một GPU 80 GB; các model này không được phục vụ qua OpenAI API. Vì DigitalOcean hiện tại không phải GPU inference host của dự án, local model chỉ nên là provider tùy chọn sau khi benchmark thực tế.

## 6. Contract kỹ thuật đề xuất

```text
IAiProvider.GenerateReportInsightAsync(ReportInsightInput, CancellationToken)
 -> AiInsightResult

AiInsightResult
  summary, highlights[], risks[], recommendations[]
  evidenceRefs[], confidence
  provider, model, promptVersion, generatedAt
  isFallback, refusalCode, usage, latencyMs
```

Responses API dùng Structured Outputs với schema strict. Backend vẫn phải validate:

- tất cả số trong câu trả lời có trong input hoặc evidence;
- evidence ID thuộc allow-list;
- không có action verb mutation;
- độ dài/microcopy hợp UI;
- refusal và schema invalid là trạng thái hữu hạn, không nuốt lỗi.

Cache key nên gồm `scope + period + inputHash + promptVersion + model`; audit lưu actor/scope/provider/model/promptVersion/latency/usage/status/inputHash, không lưu raw secret hoặc raw PII.

## 7. UX/Figma đã thiết kế

File VPP Pulse có các màn hình desktop-first:

| Frame | Nội dung |
|---|---|
| `104:2` AI01 — Report overview / Insight panel | KPI deterministic, takeaway, chart, evidence, AI panel, regenerate |
| `105:2` AI02 — Anomaly review | Severity list, detail, evidence xác định, AI explanation, review action |
| `107:2` AI03 — Ask the report | Scope chips, câu hỏi, câu trả lời, evidence refs, unsupported state |
| `108:2` AI04 — AI governance / controls | Provider/model, flag, allowed scope, budget, retention, kill switch, audit |
| `110:2` AI05 — AI states & safety | Loading, success, stale, fallback, unavailable, rate limit, refusal, unauthorized |

Mỗi màn hình phải giữ nhãn “AI generated” hoặc “rule-based fallback”, timestamp, scope và đường về evidence. AI không được trông như nút quyết định nghiệp vụ.

## 8. Workflow duyệt design → code

Không nên chốt bằng “70% Figma rồi code”. Với màn hình nghiệp vụ/AI, cần khóa trước **100% IA, data contract, role, state, error/fallback và interaction chính**; visual polish có thể ở 80–90%. Sau khi code, vẫn phải chỉnh pixel-level vì Radzen, font rendering, viewport và SignalR/prerender không thể mô phỏng tuyệt đối trong Figma.

Quy trình:

1. Duyệt product scope và data boundary.
2. Duyệt Figma desktop: happy path + loading/empty/error/permission/stale/fallback.
3. Map token/component sang Razor/Radzen, không detach design system.
4. Implement vertical slice với feature flag off.
5. Browser QA ở 1440/1920, sau đó 768/390; cập nhật code để khớp Figma đã duyệt.
6. Chỉ thay đổi Figma khi đó là design decision mới, không hợp thức hóa sai lệch runtime.

## 9. Acceptance trước khi bật AI

- feature flag mặc định off ở Production;
- authorized aggregate DTO và redaction test pass;
- key không xuất hiện trong browser bundle/log/repo;
- schema, numeric invariants, evidence allow-list và refusal tests pass;
- deterministic fallback hoạt động khi timeout/provider lỗi;
- budget/rate-limit/cache/audit có test;
- UI có loading/stale/error/unauthorized và reduced-motion behavior;
- human review và legal/privacy checkpoint được ghi nhận trước production thật.

### Tài liệu chính

- [OpenAI production best practices](https://developers.openai.com/api/docs/guides/production-best-practices)
- [OpenAI your data](https://developers.openai.com/api/docs/guides/your-data)
- [Migrate to Responses](https://developers.openai.com/api/docs/guides/migrate-to-responses)
- [Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs)
- [OpenAI open-weight models (gpt-oss)](https://help.openai.com/en/articles/11870455-openai-open-weight-models-gpt-oss)
