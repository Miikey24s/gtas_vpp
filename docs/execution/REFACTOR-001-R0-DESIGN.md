# REFACTOR-001-R0-DESIGN — Thiết kế tối ưu tốc độ full E2E `gtas_vpp_fe.UITests`

Trạng thái: BẢN THIẾT KẾ (chưa triển khai). Phục vụ R-0.1→R-0.5 trong
`docs/execution/REFACTOR-001.md` mục 3. Mục tiêu: full suite ~25-28 phút → 10-14 phút,
**không giảm số assertion, không đổi env contract, không ép cổng, không chạy nhiều suite song song**.

Mọi số liệu thời gian trong tài liệu này là **ước tính từ đọc code**; R-0.1 (baseline trx)
phải chạy trước để hiệu chỉnh trước khi chốt từng bước.

---

## 1. Bản đồ hiện trạng

### 1.1. Phát hiện quan trọng nhất: overhead là **per-test-method**, không phải per-class

`TestBase` implement `IAsyncLifetime` trực tiếp trên test class
(`tests/Frontend.UiTests/Core/TestBase.cs:15`, `:37`). xUnit (v2 lẫn v3) tạo **một instance test
class mới cho mỗi `[Fact]`**, nên `InitializeAsync()` — tức toàn bộ chuỗi LocalDB + Aspire +
Chromium — chạy lại cho **từng test method**, không phải từng class. Plan mục 3 ghi
"mỗi test class dựng lại hệ thống"; thực tế còn tệ hơn ở các class nhiều fact:

- `AccountShellSmokeTests` (3 fact) → 3 lần dựng stack.
- `LibraryGridScrollTests` (3 fact) → 3 lần dựng stack.
- `HistoryTests` (2 fact) → 2 lần dựng stack.

Đếm hiện tại (grep `[Fact]|[Theory]`, 2026-07-26): **27 test / 22 class**, trong đó
`ComposeConfigurationSyntaxTests` (1 fact) không kế thừa `TestBase` → **26 lần dựng full stack**
mỗi lượt full suite.

### 1.2. Vòng đời một lần dựng (một `[Fact]`)

Chuỗi trong `TestBase.InitializeAsync` (`TestBase.cs:37-79`) khi `GTAS_E2E_ISOLATED=1`:

| # | Việc | Vị trí | Chi phí ước tính |
|---|---|---|---|
| 1 | `QaUiSafetyContract.EnsureRunAllowed` (kiểm env contract) | `TestBase.cs:48-52` | ~0 |
| 2 | `LocalDbQaFixture.RecoverStaleAsync` — quét manifest `%TEMP%\gtas-vpp-qa` | `TestBase.cs:234` | 1-3s (gọi `SqlLocalDB` cho từng manifest) |
| 3 | `LocalDbQaFixture.CreateAsync` — tạo instance LocalDB mới `GTASVPP_QA_<hex>` + start | `TestBase.cs:235`, `tests/TestSupport/LocalDbQaFixture.cs:39-90` | 8-15s |
| 4 | `CREATE DATABASE` ×2 (chính + companion `GTAS_MENU`) + marker + **EF migrate + seed** | `LocalDbQaFixture.cs:203-232`, `QaFixtureSeeder.MigrateAndSeedAsync` (`QaFixtureSeeder.cs:50-73`) | 10-20s |
| 5 | `DistributedApplicationTestingBuilder.CreateAsync<MyAspire_AppHost>` + `BuildAsync` + `StartAsync` — khởi động **backend + frontend** (2 process dotnet); backend tự chạy `DatabaseInitialization Mode=MigrateAndReference` (migrate lần 2 + nạp reference SQL/SP) | `TestBase.cs:242-252`, `src/Hosting/AppHost/AppHost.cs:6-27` | 20-35s |
| 6 | Identity handshake: poll `/internal/qa/database-identity`, bước nhảy **2s/attempt** | `TestBase.cs:269-306` | 2-8s (granularity thô) |
| 7 | Poll frontend ready, bước nhảy 2s | `TestBase.cs:391-406` | 0-4s |
| 8 | `Playwright.CreateAsync` + **launch Chromium mới** + `NewPageAsync` | `TestBase.cs:59-70` | 2-4s |

Tổng overhead/test ≈ **45-65s**. `DisposeAsync` (`TestBase.cs:81-115`) còn cộng thêm: đóng
browser, dispose DistributedApplication (kill 2 process), drop 2 database, stop+delete LocalDB
instance, xóa data dir + manifest (~5-10s/test).

26 lần dựng × ~50-60s ≈ **20-26 phút chỉ riêng overhead** — khớp với thực đo 25-28 phút, tức
**thân test (bodies) toàn suite chỉ chiếm ~5-8 phút**.

### 1.3. Phân loại 22 test class theo capability (grep `IAuthenticatedUiTest|IMutatingUiTest`)

| Nhóm | Class ([số fact]) | Đặc điểm |
|---|---|---|
| Không cần fixture | `ComposeConfigurationSyntaxTests` [1] | Không kế thừa `TestBase` |
| Anonymous (TestBase, không auth) | `AccountShellSmokeTests` [3], `LoginFeedbackTests` [1], `LogoutFlowTests` [1], `AccessibilitySmokeTests` [1] | Chỉ đụng trang login/register/logout; login fail dùng user không tồn tại (`LoginFeedbackTests.cs:46`) → không đổi state account seed. Khi `GTAS_E2E_ISOLATED=1` vẫn dựng full stack riêng (`TestBase.cs:54-57`) |
| Read-only authenticated (`IAuthenticatedUiTest`) — 13 class / 16 fact | `LoginTests` [1], `UserMenuVisualTests` [1], `ProductCatalogTests` [1], `ReconnectModalVisualTests` [1], `ShellNavigationRegressionTests` [1], `ShellResponsiveTests` [1], `HistoryTests` [2], `DepartmentSummaryTests` [1], `AllOrdersSummaryTests` [1], `DashboardMyOrdersVisualTests` [1], `AtlasWave1Tests` [1], `GlobalRenderFlowTests` [1], `LibraryGridScrollTests` [3] | Chỉ đọc dữ liệu seed; mutation phụ vô hại duy nhất là backend cập nhật `LastLoginAtUtc`/`AccessFailedCount=0` khi login thành công (không assertion nào đọc các cột này). `ReconnectModalVisualTests` chỉ ép CSS state phía client (`ReconnectModalVisualTests.cs:37-43`) — không đụng server |
| Mutating (`IMutatingUiTest`) — 4 class / 4 fact | `OrderCreateTests` [1] (sửa + hủy đơn seed QA-OWN → tạo revision 2/3), `OrderManagementTests` [1] (tạo 2 supplement + duyệt/từ chối), `AccountLifecycleTests` [1] (INSERT user `e2e.pending`), `PermissionToggleTests` [1] (bật/tắt `REPORT_VIEW`, có finally khôi phục qua UI) | Thay đổi bền vững trong DB; chạy lại trên cùng DB **không** cho kết quả như lượt 1 (vd đơn QA-OWN đã Cancelled, username `e2e.pending` đã tồn tại) |

Hệ quả cho thiết kế: nhóm read-only + anonymous (18 class / 23 fact) có thể dùng chung **một**
app + DB; nhóm mutating **bắt buộc** DB tươi (hoặc restore về checkpoint) giữa các test.

### 1.4. Kho hard-wait `WaitForTimeoutAsync` (grep toàn `gtas_vpp_fe.UITests`, 2026-07-26)

Chi tiết thay thế ở mục 2.5. Tổng cộng **30 call site**; nặng nhất là 2×750ms trong
`LoginPage` (trả phí **mỗi lần login**, ~25-30 login/suite ≈ 40-45s) và các wait trong vòng lặp
viewport/route (`ShellResponsiveTests.cs:95` 350ms × ~12 vòng, `AccountShellSmokeTests.cs:64`
250ms × 9 vòng).

---

## 2. Thiết kế đích

### 2.1. Kiến trúc fixture xUnit v3 (repo đang dùng `xunit.v3` 3.2.2 — hỗ trợ assembly fixture + collection fixture + ctor injection)

```
[assembly: AssemblyFixture(typeof(PlaywrightBrowserFixture))]   // 1 Chromium/suite
[assembly: CollectionBehavior(DisableTestParallelization = true)] // GIỮ NGUYÊN (ràng buộc owner)

Core/PlaywrightBrowserFixture.cs   // browser process dùng chung
Core/SharedE2EAppFixture.cs        // LocalDbQaFixture + DistributedApplication, lazy-start
Core/ReadOnlyE2ECollection.cs      // [CollectionDefinition("e2e-readonly")]  : ICollectionFixture<SharedE2EAppFixture>
Core/MutatingE2ECollection.cs      // [CollectionDefinition("e2e-mutating")] : ICollectionFixture<MutatingE2EAppFixture>
Core/TestBase.cs                   // v2: nhận fixture qua ctor, mỗi test 1 BrowserContext mới
```

- **`PlaywrightBrowserFixture` (assembly fixture)**: giữ `IPlaywright` + `IBrowser` (giữ nguyên
  logic `GetHeadlessMode`/`GetSlowMo`/`GetBrowserExecutablePath` từ `TestBase.cs:331-359`).
  Expose `Task<IBrowser> GetBrowserAsync()` có guard `Browser.IsConnected` — nếu Chromium chết
  giữa suite thì tự launch lại thay vì fail dây chuyền. Ước lợi: bỏ ~2-4s launch/test ≈ 1-1.5′.
- **`SharedE2EAppFixture` (collection fixture, nhóm read-only + anonymous)**: chuyển nguyên khối
  `StartIsolatedApplicationAsync` + `WaitForConfirmedIdentityAsync` + `WaitForBaseUrlReadyAsync`
  (`TestBase.cs:232-306`, `:391-406`) vào fixture, chạy **một lần cho cả collection**.
  Lazy-start (`GetOrStartAsync`) để: (a) lượt chạy chỉ có anonymous test không isolated vẫn resolve
  URL docker như cũ (`ResolveAnonymousLocalBaseUrlAsync`, `TestBase.cs:308-329`); (b) filter run
  một class không trả phí thừa. `RecoverStaleAsync` chỉ gọi **một lần** khi fixture khởi động.
- **`MutatingE2EAppFixture`**: app + LocalDB **riêng, RunId riêng** — không bao giờ chung DB với
  nhóm read-only. Cách reset giữa các test: mục 2.3.
- **`TestBase` v2**: `InitializeAsync` chỉ còn (1) kiểm `QaUiSafetyContract.EnsureRunAllowed`
  **giữ per-test** (defense-in-depth, không dời hết vào fixture), (2) lấy `BaseUrl` từ fixture,
  (3) `browser.NewContextAsync(Locale="vi-VN", TimezoneId="Asia/Ho_Chi_Minh")` + `NewPageAsync`.
  `DisposeAsync` chỉ đóng context/page. **Mỗi test một BrowserContext mới** → cookie, localStorage,
  circuit Blazor cách ly tuyệt đối như hiện tại (R-0.3 thỏa mà không giảm cách ly).

Env contract **không đổi**: `GTAS_E2E_ISOLATED=1` vẫn là điều kiện để fixture dựng app;
`GTAS_E2E_MUTATION_OPT_IN` vẫn kiểm per-test cho `IMutatingUiTest`; harness vẫn tự tạo
`GTASVPP_QA_*`; vẫn không đụng database TEST cá nhân; không ép cổng (Aspire vẫn cấp port động);
`DisableTestParallelization` giữ nguyên nên hai collection chạy **tuần tự**, tối đa một
DistributedApplication sống tại một thời điểm (collection fixture dispose khi collection xong).
Thứ tự collection: thêm `ITestCollectionOrderer` đơn giản cho read-only chạy trước mutating —
không bắt buộc cho tính đúng (hai DB tách biệt) nhưng giữ profile tài nguyên ổn định để so đo.

### 2.2. Nhóm read-only dùng chung app: điều kiện an toàn

- Mỗi test vẫn tự login trong context mới (hành vi login được `LoginTests` khóa riêng — số
  assertion không đổi). Ghi chú tùy chọn về sau (không thuộc R-0, không tính vào mục tiêu):
  cache `storageState` sau lần login đầu mỗi persona để bỏ bớt login lặp — chỉ làm khi owner
  duyệt riêng vì thay đổi đường vào của test.
- Audit một chiều đã làm ở mục 1.3: không class read-only nào ghi dữ liệu nghiệp vụ. Khi review
  PR bước 2 (mục 3), mỗi class phải được tick lại một dòng "không POST/PUT/DELETE nghiệp vụ"
  trước khi gắn vào collection.
- Test dùng nhiều persona (`SwitchUserAsync` trong `ShellResponsiveTests`,
  `OrderManagementTests`) không cần đổi: logout/login diễn ra trong context riêng của test đó.

### 2.3. Nhóm mutating: cách ly hai pha

**Pha A (an toàn, làm trước):** giữ nguyên hành vi hiện tại — mỗi class mutating tự dựng
`LocalDbQaFixture` + app riêng (per-test ≡ per-class vì cả 4 class đều 1 fact). Chi phí: 4 lần
dựng ≈ 4′. Không rủi ro mới, cách ly đã được chứng minh bởi hiện trạng.

**Pha B (R-0.4 đầy đủ, làm sau khi Pha A ổn định):** `MutatingE2EAppFixture` dựng **một** app +
DB, rồi reset dữ liệu giữa các test bằng **native `BACKUP DATABASE` / `RESTORE DATABASE`**:

1. Sau khi backend xác nhận identity (tức migrate + reference SQL/SP + seed đã xong), fixture
   chạy `BACKUP DATABASE [GTAS_VPP_TEST_QA_<run>] TO DISK = '<DataDirectory>\checkpoint.bak'`.
   Companion `GTAS_MENU` là reference thuần đọc — không cần backup.
2. Trước mỗi test mutating (trừ test đầu): `ALTER DATABASE ... SET SINGLE_USER WITH ROLLBACK
   IMMEDIATE; RESTORE DATABASE ...; ALTER DATABASE ... SET MULTI_USER;` + `SqlConnection.ClearAllPools()`.
   Ước tính 10-20s/lần với DB QA nhỏ — so với ~55s dựng lại cả stack.
3. Sau restore, gọi lại `/internal/qa/database-identity` để xác nhận marker `__GTASQARun` khớp
   RunId trước khi mở browser (tái dùng `WaitForConfirmedIdentityAsync`).

**Vì sao không dùng hai API sẵn có của fixture:**
- `LocalDbQaFixture.ResetAsync` (`LocalDbQaFixture.cs:104-109`) drop + tạo lại DB rồi
  migrate+seed bằng EF — nhưng **reference SQL/SP/view do backend nạp lúc startup**
  (`DatabaseInitialization__Mode=MigrateAndReference`, `AppHost.cs:9`); reset dưới một app đang
  sống sẽ mất SP/view mà không có gì nạp lại → app hỏng ngầm.
- `SeedAgainAsync` (`LocalDbQaFixture.cs:92-102`) chỉ upsert dữ liệu canonical — không dọn được
  revision 2/3 do `OrderCreateTests` tạo (GUID mới, giành `IsCurrentRevision`), không xóa user
  `e2e.pending` → lượt 2 fail. RESTORE về file vật lý là cách duy nhất trả lại đúng byte trạng
  thái seed, kể cả SP/view.

**Rủi ro riêng của Pha B** (và lý do phải có gate chứng minh, mục 4): cache in-memory phía
backend (permission cache, notification state) không bị RESTORE đụng tới. `PermissionToggleTests`
đã tự khôi phục qua UI trong `finally` (`PermissionToggleTests.cs:48-57`) nên cache và DB nhất
quán; các test order không có cache server-side tương ứng đã biết. Nếu bằng chứng 2-lượt (mục 4)
fail vì cache → Pha B lùi lại, giữ Pha A (vẫn đạt mục tiêu trên, xem mục 5).

### 2.4. Tách migrate khỏi seed (R-0.4 phần còn lại)

Với fixture dùng chung, migrate+seed chỉ còn chạy **2 lần/suite** (một cho mỗi collection) thay
vì 26 lần — phần "tách migrate (1 lần) khỏi seed (theo nhóm)" của plan đạt được bằng chính kiến
trúc collection, **không cần sửa `QaFixtureSeeder`/`LocalDbQaFixture`** (giữ nguyên hợp đồng
QA-001: idempotent seed, marker, guard). Điểm tối ưu duy nhất đáng làm thêm: backend đang migrate
**lần thứ hai** lúc startup (harness đã migrate ở bước 4 mục 1.2). Không đổi cấu hình
`MigrateAndReference` trong R-0 — nó là hợp đồng an toàn của backend; ghi nhận làm ứng viên đo
riêng ở R-1 nếu baseline cho thấy đáng kể.

### 2.5. Danh sách `WaitForTimeoutAsync` cứng và thay thế (R-0.5)

Phân hai loại. Loại **(A)** thay được bằng wait điều kiện; loại **(B)** là "quiet window" cho
negative assertion (chờ để chứng minh *không* có gì xuất hiện) — giữ nhưng ghi chú, vì bỏ là đổi
ngữ nghĩa assertion.

| Vị trí | ms | Loại | Thay thế đề xuất |
|---|---|---|---|
| `Pages/Auth/LoginPage.cs:35` | 750 | A | Chờ form login interactive theo mẫu `data-shell-ready` (thêm marker `data-account-ready` vào login card sau first render — cùng pattern `LeftSidebar.razor.cs`); hoặc heuristic `_bl_` attribute như `GetInteractiveButtonAsync` (`TestBase.cs:203-218`) |
| `Pages/Auth/LoginPage.cs:62` | 750 | A | Như trên — gộp về một chỗ chờ duy nhất trong `GotoAsync`, bỏ hẳn wait trong `LoginAsync` |
| `Tests/Auth/AccountLifecycleTests.cs:55` | 1000 | A | Chờ marker interactive của form register (cùng pattern trên) |
| `Tests/Auth/AccountLifecycleTests.cs:85` | 1500 | B→A | Thay bằng chờ alert lỗi đăng nhập hiện (`[role=alert]` như `LoginFeedbackTests.cs:48-53`) rồi assert URL vẫn `/Account/Login` — tín hiệu dương thay cho quiet window |
| `Tests/ShellResponsiveTests.cs:95` | 350 ×~12 vòng | A | Đã có `data-shell-ready` + `#main-content` (`:87-94`); thay 350ms bằng double-`requestAnimationFrame` qua `EvaluateAsync` (chờ DOM hết churn) |
| `Tests/ShellResponsiveTests.cs:202` | 5500 | A | `WaitForFunctionAsync("() => !document.querySelector('.rz-notification:visible')")` — chờ toast biến mất thay vì chờ cố định 5.5s |
| `Tests/ShellResponsiveTests.cs:226` | 800 | A | Loader đã chờ ẩn ở `:209-224`; thay bằng double-rAF + `document.fonts.ready` |
| `Tests/AccountShellSmokeTests.cs:64` | 250 ×9 vòng | A | Double-rAF sau `.vpp-login-card` visible |
| `Tests/AtlasWave1Tests.cs:23` | 500 | **B** | Negative assertion "không có toast lỗi" — giữ, nhưng thêm chờ tín hiệu dương trước (grid/empty-state render xong) để 500ms là quiet window thật |
| `Tests/AtlasWave1Tests.cs:67` | 350 | A | Double-rAF trong `GotoMainRouteAsync` |
| `Tests/ShellNavigationRegressionTests.cs:32` | 300 | A | Chờ class `sidebar-collapsed` biến mất trên `.vpp-sidebar` (`WaitForFunctionAsync`) |
| `Tests/ShellNavigationRegressionTests.cs:139` | 300 | A | Chờ `aria-expanded="false"` trên parent toggle |
| `Tests/ShellNavigationRegressionTests.cs:156` | 500 | A | Chờ trạng thái indicator đạt điều kiện collapsed (chính `IndicatorState` đang đọc ở `:158`) — chuyển phép đọc vào `WaitForFunctionAsync` |
| `Tests/ShellNavigationRegressionTests.cs:170` | 500 | A | Đối xứng: chờ indicator expanded |
| `Tests/ShellNavigationRegressionTests.cs:270` | 300 | A | Sau `WaitForFunctionAsync` `:263-268` đã có điều kiện; thay bằng double-rAF |
| `Tests/ShellNavigationRegressionTests.cs:276` | 300 | A | Chờ `.vpp-sidebar` có class `sidebar-collapsed` |
| `Tests/Order/HistoryTests.cs:48` | 80 | A | Đã có `WaitForFunctionAsync` scrollTop `:45-47`; thay bằng chờ scrollTop ổn định (2 lần đọc rAF liên tiếp bằng nhau) |
| `Tests/Order/HistoryTests.cs:164` | 60 | **B** | Negative assertion "không render loading-line" `:165` — giữ, ghi chú |
| `Tests/Order/HistoryTests.cs:270` | 50 | A | Double-rAF trước khi đo geometry popover |
| `Tests/Order/HistoryTests.cs:419` | 50 | A | Như `:48` — scrollTop ổn định |
| `Tests/Order/HistoryTests.cs:589` | 200 | A | Chờ dropdown đóng / selection phản ánh vào trigger label |
| `Tests/Order/HistoryTests.cs:761` | 240 | A | Chờ empty-state đã visible (`:759`) + double-rAF trước khi đo geometry |
| `Tests/LibraryGridScrollTests.cs:21` | 500 | A | Chờ `.rz-data-grid-data` có row hoặc empty-state visible |
| `Tests/LibraryGridScrollTests.cs:170` | 200 | A | Sau `SetViewportSizeAsync`: chờ `innerHeight` khớp viewport mới qua `WaitForFunctionAsync` |
| `Tests/LibraryGridScrollTests.cs:177` | 100 | A | Chờ `scrollTop` đạt `scrollHeight - clientHeight` (điều kiện chính xác của phép gán `:176`) |
| `Tests/LibraryGridScrollTests.cs:226` | 500 | A | Như `:21` |
| `Tests/LibraryGridScrollTests.cs:293` | 500 | A | Như `:170` |
| `Tests/LibraryGridScrollTests.cs:323` | 200 | A | Như `:170` |
| `Tests/LibraryGridScrollTests.cs:328` | 100 | A | Như `:177` |
| `Tests/LibraryGridScrollTests.cs:566` | 200 | A | Chờ `flexDirection` đổi (chính giá trị đang đọc `:567`) |

Các vòng poll `Task.Delay` (`TestBase.SwitchUserAsync:148`, `GetInteractiveButtonAsync:225`,
`LoginPage.WaitForDashboardAsync:54`, `OrderCreateTests.WaitForUrlMatchAsync:199`) đã là wait
điều kiện — không thuộc phạm vi R-0.5. Điểm đo hạ tầng: 2 poll **2s/bước** trong
`TestBase.cs:301` và `:402` giảm xuống 250-500ms/bước (tiết kiệm 2-6s mỗi lần dựng app, còn
2 lần/suite sau khi gộp fixture).

Ước lợi R-0.5: ~60-90s/suite (nặng nhất: 2×750ms/login × ~25-30 login, 5.5s price-list, các
vòng lặp viewport) + giảm flake do wait cố định quá ngắn khi máy chậm.

---

## 3. Trình tự triển khai an toàn

Nguyên tắc chung: mỗi bước là một PR nhỏ; gate tối thiểu mỗi bước = `dotnet build gtas_vpp.sln
-c Release` 0 warning + `dotnet test gtas_vpp_fe.Tests` + `gtas_vpp_be.Tests` xanh; **không
chạy hai suite E2E song song** — mọi lượt E2E (kể cả filtered smoke) chạy tuần tự trong cửa sổ
batch của owner. Full E2E chốt theo quy tắc batch (gom bước, một lượt chốt).

| Bước | Nội dung | Gate riêng | Rủi ro |
|---|---|---|---|
| S0 (R-0.1) | **Baseline**: chạy full suite một lượt với `--logger "trx;LogFileName=e2e-baseline.trx"`; script PowerShell nhỏ (scratch, không commit) tổng hợp duration per-test/per-class từ trx. Lưu bảng số vào mục 6 của file này | Suite xanh như hiện trạng | Không |
| S1 | `PlaywrightBrowserFixture` (assembly fixture) + `TestBase` chuyển sang `NewContextAsync` per-test. Không đụng app/DB lifecycle | Smoke filtered: 1 class anonymous + 1 read-only + 1 mutating (3 lượt `dotnet test --filter`, tuần tự) | Thấp — context mới ≡ cách ly cũ |
| S2 | `SharedE2EAppFixture` + collection `e2e-readonly` cho 13 class `IAuthenticatedUiTest` read-only. `TestBase` branch: `IMutatingUiTest` giữ nguyên đường per-test cũ | Chạy nguyên collection read-only 1 lượt; so per-class trx với baseline; checklist "không mutate" tick từng class trong PR | **Cao nhất toàn kế hoạch** — xem mục 4 |
| S3 | Gộp 4 class anonymous vào collection read-only (chỉ khi `GTAS_E2E_ISOLATED=1`; đường docker anonymous giữ nguyên) | Lượt collection read-only + lượt anonymous không-isolated | Thấp |
| S4 (R-0.5) | Thay hard wait loại A theo bảng 2.5, chia 3-4 PR theo file; loại B giữ + comment giải thích | Mỗi PR: chạy đúng các class bị sửa (filtered, tuần tự); đếm assertion trước/sau bằng diff (không dòng `Should()`/`WaitForAsync` nào bị xóa) | Trung bình — wait điều kiện sai còn tệ hơn wait cứng; mỗi thay thế phải trỏ đúng tín hiệu mà phép đo phía sau cần |
| S5 (Pha B của R-0.4, tùy kết quả S2) | `MutatingE2EAppFixture` + BACKUP/RESTORE checkpoint giữa 4 test mutating | Bằng chứng 2-lượt (mục 4) **bắt buộc** trước khi merge | Cao — cache backend, restore dưới app sống |
| S6 | Full E2E chốt batch, so tổng thời gian với baseline, cập nhật REFACTOR-001 mục 3 + decision log | Full suite xanh 2 lượt liên tiếp | — |

Bước rủi ro nhất: **S2**. Lý do: một class read-only bị phân loại sai (có side effect chưa thấy
khi đọc code) sẽ làm test khác trong collection flaky *không xác định*. Giảm rủi ro: (a) audit
từng class trong PR như gate ghi trên; (b) chạy collection read-only **2 lượt liên tiếp** trước
khi merge — nếu lượt 2 khác lượt 1 là có state rò rỉ; (c) thứ tự test trong collection giữ mặc
định xUnit (ổn định theo tên) để kết quả tái lập.

Cách đo trước/sau (R-0.1 và sau mỗi bước): cùng máy, cùng cấu hình env, cùng lệnh
`dotnet test gtas_vpp_fe.UITests -c Release --logger trx`; so ba con số: tổng wall time,
tổng duration per-class, top-5 test chậm nhất. Trx là nguồn số duy nhất; không dùng cảm quan.

---

## 4. Rủi ro và cách chứng minh cách ly nhóm mutate

| Rủi ro | Ảnh hưởng | Đối sách |
|---|---|---|
| Class read-only thực ra có mutate ẩn | Flake ngẫu nhiên trong collection dùng chung | Audit + chạy 2 lượt liên tiếp ở gate S2; nếu phát hiện → chuyển class đó sang `IMutatingUiTest` (sửa phân loại, không sửa test) |
| Cache in-memory backend giữ state giữa các test mutating (Pha B) | RESTORE trả DB về seed nhưng backend vẫn nhớ state cũ | Bằng chứng 2-lượt (dưới); nếu fail → bỏ Pha B, giữ Pha A |
| RESTORE dưới app sống: connection đang mở làm SINGLE_USER treo | Test mutating treo/timeout | `ClearAllPools()` + fixture đảm bảo không request nào đang bay (restore chạy giữa 2 test, browser context đã đóng); timeout cứng 120s cho lệnh restore |
| Chromium/assembly fixture chết giữa suite | Fail dây chuyền toàn bộ test còn lại | Guard `IsConnected` + relaunch trong `GetBrowserAsync()` |
| App dùng chung chết giữa collection (backend crash) | Fail dây chuyền collection | Fixture health-check `BaseUrl` trước mỗi test (HTTP GET nhanh, đã có `IsBaseUrlReadyAsync`); fail thì fail nhanh với message rõ thay vì timeout 120s từng test |
| LocalDB instance rò khi suite bị kill | Rác `GTASVPP_QA_*` | Không đổi: manifest + `RecoverStaleAsync` (nay chạy 1 lần/fixture) + kiểm `SqlLocalDB info` sau suite theo QA-001 |
| Trx timing nhiễu do máy dev | Kết luận sai về lợi ích | Mỗi mốc đo 2 lượt, lấy trung bình; đo trên cùng máy owner vẫn chạy batch E2E |

**Bằng chứng cách ly nhóm mutate (bắt buộc trước khi merge S5, lặp lại ở S6):**

1. Chạy `dotnet test --filter` đúng collection `e2e-mutating` **2 lượt liên tiếp, cùng lệnh,
   cùng env** → cả 2 lượt xanh và cùng số test pass. Lượt 2 chính là phép thử "restore có trả
   đúng trạng thái seed không" (vd `AccountLifecycleTests` đăng ký lại `e2e.pending` phải thành
   công lượt 2 — user cũ phải biến mất sau restore).
2. Sau 2 lượt mutating, chạy collection read-only 1 lượt → xanh (chứng minh hai DB thực sự tách,
   không có đường rò qua file/manifest).
3. Số assertion: diff PR không xóa dòng assertion nào; tổng test = 27 như baseline.

Ở Pha A, bằng chứng 1 tự thỏa (mỗi class DB mới) nhưng vẫn chạy như quy trình để giữ chuẩn so
sánh cho Pha B.

---

## 5. Ước tính thời gian đạt được

Mô hình (hiệu chỉnh lại bằng số R-0.1):

| Thành phần | Hiện trạng | Sau Pha A (S1-S4) | Sau Pha B (S5) |
|---|---|---|---|
| Dựng stack | 26 × ~50-60s ≈ 20-26′ | 1 (read-only) + 4 (mutating) ≈ 5-6′ | 2 ≈ 2-3′ + 3 restore ≈ 0.5-1′ |
| Launch Chromium | 26 × ~2-4s ≈ 1-1.5′ | 1 lần ≈ 0 | như Pha A |
| Thân test | ~5-8′ | ~4-7′ (trừ 1-1.5′ hard wait) | như Pha A |
| **Tổng** | **25-28′ (thực đo)** | **~10-14′** | **~8-11′** |

Kết luận: **Pha A (S1-S4) đã đủ chạm mục tiêu 10-14 phút** của R-0; Pha B là dự phòng nếu số đo
Pha A rơi vào cận trên, đổi lại rủi ro cao hơn — quyết định làm S5 hay không chốt bằng số trx
sau S4, ghi vào decision log REFACTOR-001.

## 6. Số đo baseline (điền sau khi chạy R-0.1)

| Mốc | Ngày | Tổng wall time | Top 5 class chậm nhất | Ghi chú |
|---|---|---|---|---|
| Baseline (S0) | _chưa đo_ | | | |
| Sau S2 | | | | |
| Sau S4 | | | | |
| Sau S5 (nếu làm) | | | | |
