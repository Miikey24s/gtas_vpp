# GTAS VPP — Target Mode Execution Template

> Sao chép template này thành một execution record cho mỗi task.
> Gợi ý path: `docs/execution/<TASK-ID>.md`.
> Không ghi secret, password, token, connection string hoặc dữ liệu cá nhân thật.

## 0. Bản một ánh nhìn

> Giữ phần này vừa một màn hình. Đây là nơi owner xem hằng ngày; checklist/log đầy đủ nằm ở các mục chi tiết được link bên dưới.

| Mục | Tóm tắt dễ hiểu | Chi tiết |
|---|---|---|
| Kết quả cần đạt | <Một câu mô tả trạng thái cuối> | [Objective](#plan-detail-objective) |
| Phạm vi | <Chạm gì / không chạm gì> | [Scope](#plan-detail-scope) |
| Các bước chính | <3–7 bước, kèm trạng thái> | [Implementation](#plan-detail-implementation) |
| Kiểm tra | <Build/test/browser/DB/Word gate quan trọng> | [Verification](#plan-detail-verification) |
| Rủi ro hoặc blocker | <Chỉ ghi vấn đề có thể đổi kết quả> | [Risks](#plan-detail-risks) |
| Bước tiếp theo | <Một hành động cụ thể> | [Continuation](#plan-detail-continuation) |

**Thuật ngữ cần biết:** chỉ giải thích tối đa 1–3 thuật ngữ thực sự khó xuất hiện trong summary. Ví dụ: `gate` = điều kiện phải pass trước khi chuyển bước; `vertical slice` = một lát chức năng hoàn chỉnh có thể chạy và kiểm tra độc lập.

Task nhỏ có thể chỉ dùng mục 0 trong thread. Task phức tạp mới tạo execution record đầy đủ bên dưới.

## 1. Task header

```markdown
# <TASK-ID> — <Tên task>

- Status: NOT_STARTED | BLOCKED_DECISION | BLOCKED_EXTERNAL | IN_PROGRESS | IN_REVIEW | DONE | DEFERRED | DROPPED
- Priority: P0 | P1 | P2 | P3
- A+ cutline class: safety-floor | mandatory-product | mandatory-registration | selected-showcase | deferred
- Path: STANDARD | ALTERNATIVE (ghi rõ waiver/evidence/approver)
- Owner/agent:
- Branch:
- Base commit:
- Started at (Asia/Ho_Chi_Minh):
- Finished at:
- Master-plan version/commit:
- Related decisions/ADRs:
- Dependencies verified:
- User approval required: Yes/No
- User approval evidence/link:
```

<a id="plan-detail-objective"></a>

## 2. Objective và non-goals

```markdown
## Objective

<Một kết quả cụ thể, kiểm chứng được.>

## Why now

<Rủi ro/giá trị/dependency mà task xử lý.>

## Non-goals

- <Thay đổi gần kề nhưng không thuộc task.>
- <Không refactor/upgrade/rename ngoài phạm vi.>
```

## 3. Preflight

````markdown
## Preflight

- [ ] Đã chạy `./scripts/gtas.cmd preflight -Scope <scope>` và lưu summary cần thiết.
- [ ] `./scripts/gtas.cmd agent-check` pass; custom instruction/skill mới có eval phù hợp.
- [ ] Đã đọc AGENTS.md và instruction liên quan.
- [ ] Đã đọc task card, ADR và business examples.
- [ ] `git status --short` đã lưu; file dirty ngoài scope đã xác định owner.
- [ ] Branch/base commit đúng; không stage file ngoài scope.
- [ ] Baseline build/test được xác nhận hoặc failure hiện có được ghi.
- [ ] Nếu DB: backup/sanitized clone/preflight/rollback path đã sẵn sàng.
- [ ] Nếu UI: đã đọc `src/Frontend/Blazor/AGENTS.md`, UI skill và living plan.
- [ ] Nếu Word: original read-only, working/final path đúng, render workflow sẵn sàng.
- [ ] Nếu external system/secret: quyền của người dùng đã được xác nhận.

### Initial git status

```text
<paste sanitized output>
```

### Baseline commands/results

| Command | Result | Evidence/path |
|---|---|---|
| `dotnet build ...` | PASS/FAIL/BLOCKED | |
| backend tests | | |
| frontend tests | | |
| relevant integration/E2E | | |
````

<a id="plan-detail-scope"></a>

## 4. Scope và impact

```markdown
## Expected scope

### Frontend

- Project/module/file dự kiến:
- UI states/routes/permissions bị ảnh hưởng:

### Backend

- Project/module/file dự kiến:
- API/policy/service/background job bị ảnh hưởng:

### Database

- Entity/table/index/SP/migration dự kiến:
- Additive/backfill/cutover/drop stage:

### Business rules

- Rule/state/invariant:
- Boundary/timezone/scope:

### Documentation/thesis

- ADR/diagram/traceability cần cập nhật:
```

<a id="plan-detail-implementation"></a>

## 5. Implementation checklist

```markdown
## Implementation steps

1. [ ] <Bước nhỏ 1>
2. [ ] <Bước nhỏ 2>
3. [ ] <Bước nhỏ 3>

## Files actually changed

| File | Why | User-owned overlap? |
|---|---|---|
| | | No/Handled |

## Deviations from plan

- None, hoặc:
- <Deviation> — lý do — approval/ADR — impact.
```

Nếu phát hiện công việc mới:

- bug nằm trong phạm vi và cần để đạt acceptance: ghi lại, sửa trong task nếu nhỏ/rủi ro cùng loại;
- thay đổi nghiệp vụ/schema/UI lớn khác: tạo proposed task mới, không tự mở rộng;
- decision không có trong source/ADR: chuyển `BLOCKED_DECISION`.

## 6. Database/migration record

Điền `N/A` nếu task không chạm DB.

````markdown
## Database and migration

- Backup ID/path (không chứa secret):
- Backup restore verified: Yes/No/N/A
- Source DB type: disposable/sanitized copy/staging/production
- Preflight duplicate/orphan result:
- Migration name/script/bundle hash:
- Generated SQL reviewed by:
- SSMS validation result:
- Fresh DB apply: PASS/FAIL/N/A
- Existing sanitized DB upgrade: PASS/FAIL/N/A
- Post-migration probes: PASS/FAIL/N/A
- Rollback/restore rehearsal: PASS/FAIL/N/A
- Production applied: No by default / Yes with approval reference

### Migration output summary

```text
<No connection string/secret; only migration/result/error summary>
```

### Data reconciliation

| Check | Before | After | Expected |
|---|---:|---:|---:|
| Row count / duplicate / orphan / total | | | |
````

Không coi `Down()` là rollback đủ cho migration destructive hoặc settlement business facts. Ghi rõ khi rollback thực tế là restore backup hoặc forward correction.

<a id="plan-detail-verification"></a>

## 7. Test record

```markdown
## Tests added/updated

| Test/project | Scenario/invariant | Result |
|---|---|---|
| | | PASS/FAIL |

## Required commands

| Command | Result | Count/time | Evidence |
|---|---|---|---|
| `dotnet build gtas_vpp.slnx -c Release` | | | |
| `dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release` | | | |
| `dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release` | | | |
| SQL/API integration | | | |
| UI E2E | | | |
| vulnerability/secret scan | | | |
| `dotnet format ... --verify-no-changes` or changed-file ratchet | | | |
| `git diff --check` | | | |
| `./scripts/gtas.cmd verify -Scope <scope>` nếu phù hợp phạm vi | | | |

## Failures

- Existing/new:
- Root cause:
- Fixed/deferred/blocker:
- Issue/task ID nếu deferred:
```

Test count phải lấy từ output hiện tại, không copy mốc 147/29 nếu suite đã thay đổi.

## 8. UI verification

Điền `N/A` nếu không tác động UI.

```markdown
## UI verification

| Route/journey | Role | 390×844 | 768×1024 | 1920×1080 |
|---|---|---|---|---|
| | | PASS/FAIL | PASS/FAIL | PASS/FAIL |

- [ ] Không page-level horizontal overflow.
- [ ] Loading/empty/error/success/disabled states đúng.
- [ ] Keyboard-only hoàn thành journey.
- [ ] Focus order/dialog return/Escape/skip link đúng.
- [ ] Axe: 0 critical/serious hoặc exception approved.
- [ ] Contrast/reduced motion/zoom dữ liệu dài kiểm tra.
- [ ] Không console error/unhandled circuit exception.
- [ ] Không failed API/network trên happy path.
- [ ] Unauthorized direct route/action trả đúng 401/403/redirect.
- [ ] VI/EN resource/culture/date/time/VND đúng scope.
- [ ] Screenshot diff đã xem trực quan, không chỉ tool pass.

### Evidence

- Screenshot/contact sheet path (ignored):
- Browser trace/video path (ignored):
- Console/network summary:
- Reviewer note:
```

## 9. Acceptance criteria

```markdown
## Acceptance

| Criterion từ master plan | Result | Evidence |
|---|---|---|
| | PASS/FAIL/BLOCKED | |

### Negative/adversarial checks

- [ ] Direct API bypass.
- [ ] Stale rowversion/token/filter.
- [ ] Concurrent/duplicate request.
- [ ] Missing/invalid/unauthorized data.
- [ ] Retry/offline/restart path.
- [ ] Privacy/log/secret check.

### Definition of done decision

- DONE / IN_REVIEW / BLOCKED — lý do:
```

<a id="plan-detail-risks"></a>

## 10. Risk và rollback

```markdown
## Risks observed

| Risk | Probability/impact | Mitigation | Owner |
|---|---|---|---|
| | | | |

## Rollback/recovery performed or rehearsed

- Application rollback:
- Database restore/forward correction:
- Feature flag/compatibility path:
- Data reconciliation after rollback:
- Result:

## Residual risk

- <Rủi ro còn lại, không che giấu.>
```

## 11. Git/commit record

```markdown
## Git

### Final status/diff review

- [ ] `git diff --check` pass.
- [ ] Đã xem toàn bộ `git status` và diff staged/unstaged.
- [ ] Không stage secret/bin/obj/TestResults/log/cache/render/audit temp.
- [ ] Chỉ đúng scope task được stage.
- [ ] Không xóa Word/tooling/diagram/screenshot instruction ngoài scope.

| Commit | Message | Scope | Build/test evidence |
|---|---|---|---|
| | | | |

- Branch pushed: Yes/No/N/A
- PR: URL/N/A
- Last known good commit:
```

Được tạo commit local khi change-set hoàn chỉnh và `AGENTS.md` cho phép. Push, PR, merge, rewrite history hoặc deploy vẫn cần yêu cầu rõ của user.

## 12. Blocker record

```markdown
## Blockers

| ID | Type | Description | Evidence | Needed from | Since | Next check |
|---|---|---|---|---|---|---|
| BLK-... | DECISION/EXTERNAL/TECHNICAL | | | | | |

## What was exhausted

- <Read-only checks/alternatives already tried.>

## Safe work that can continue

- <Independent task/none.>
```

Không đánh dấu blocked chỉ vì task khó. Chỉ block khi thiếu quyết định/quyền/external state thực sự và không còn tiến độ an toàn trong scope.

<a id="plan-detail-continuation"></a>

## 13. Continuation note sau mất context

Đây là phần bắt buộc mỗi khi kết thúc phiên mà task chưa `DONE`.

```markdown
## Continuation note

- Current task/status:
- Current branch/HEAD:
- Working tree status:
- Files being edited:
- Last completed step:
- Last command and exact summarized result:
- Current database/migration state:
- Current browser/app/process state:
- Decisions assumed/confirmed (ADR links):
- Blocker:
- Next exact action (one command/edit/check):
- Do not redo:
- Warnings about user-owned changes:
```

## 14. Task completion summary

```markdown
## Completion summary

- Outcome first:
- Behavior changed:
- Behavior intentionally unchanged:
- Build/test/migration/UI final result:
- Commit(s):
- Documentation/ADR/diagram updated:
- Follow-up task(s):
- User action required:
- Master plan status updated at:
```

## 15. Checkpoint review template

Dùng sau CP0…CP8, không phải sau mỗi commit.

```markdown
# <CHECKPOINT> Review

- Date:
- Release/branch/commit:
- Tasks expected:
- Tasks DONE:
- Tasks BLOCKED/DEFERRED:
- Decisions still open:
- Build/test/integration/E2E summary:
- Migration/data reconciliation summary:
- UI/accessibility/performance summary:
- Security/privacy/secret summary:
- Thesis/diagram/traceability summary:
- Risks accepted by user:
- Rollback/restore evidence:
- Checkpoint PASS/FAIL:
- Approved next phase:
```
