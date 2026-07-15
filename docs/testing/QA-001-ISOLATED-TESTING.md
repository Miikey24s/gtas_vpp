# QA-001 - Huong dan test SQL va UI co lap

QA-001 dung `Path: ALTERNATIVE`: SQL Server Express LocalDB tren Windows thay
cho Docker/service container. Duong chay nay chi danh cho local/test, tao mot
instance va database rieng cho tung run, va khong duoc tai su dung cho staging,
DigitalOcean, database dung chung hoac production.

## Safety contract

Moi fixture co danh tinh duy nhat:

- instance: `(localdb)\GTASVPP_QA_<12_HEX>`;
- database chinh: `GTAS_VPP_TEST_QA_<12_HEX>`;
- database phu can cho cac view/SP hien tai: `GTAS_MENU`, nam trong cung instance
  co lap va co file vat ly rieng duoi `%TEMP%\gtas-vpp-qa`;
- marker trong database chinh: bang `dbo.__GTASQARun` voi purpose
  `GTAS_VPP_QA_TEST`, run ID va fixture version `qa-001-v1`.

Fixture chi chap nhan Windows integrated security. Guard tu choi server remote,
instance LocalDB mac dinh, database sai ten, SQL username/password,
`AttachDbFilename`, `LiveEnv`, host `Development`/`Production`, database thieu
marker va instance co database ngoai danh sach cho phep. Cleanup chi drop hai
database trong instance duy nhat sau khi xac minh marker; no khong dung den
instance `MSSQLLocalDB` hoac SQL Server service co dinh.

Tai khoan QA, JWT key va encryption key duoc tao ngau nhien trong RAM moi run.
Manifest crash recovery chi luu danh tinh khong nhay cam va process owner; khong
luu password, token hay connection string. Mot run UI moi se thu don manifest
stale cua process da chet truoc khi tao fixture moi, va bo qua fixture van co
owner dang song.

## Cong cu can co

- Windows va .NET SDK 10.
- SQL Server Express LocalDB, kem `SqlLocalDB.exe`. Co the chi dinh duong dan
  bang `GTAS_SQLLOCALDB_EXECUTABLE` neu tool khong nam trong vi tri Microsoft
  mac dinh.
- Chromium do Playwright quan ly, hoac Chrome/Edge local duoc chi dinh bang
  `UITEST_BROWSER_EXECUTABLE`.
- Docker khong bat buoc cho alternative nay.

Kiem tra nhanh:

```powershell
dotnet --info
SqlLocalDB info
Test-Path 'C:\Program Files\Google\Chrome\Application\chrome.exe'
```

Neu muon Playwright tai browser dung version cua package sau khi build:

```powershell
pwsh .\gtas_vpp_fe.UITests\bin\Release\net10.0\playwright.ps1 install chromium
```

## Ba tang test

### 1. Safety contracts mac dinh

Lenh nay portable va duoc CI chay. Ba test can LocalDB se `SKIP` neu khong co
opt-in; cac test fail-closed van phai pass.

```powershell
dotnet test .\gtas_vpp_be.IntegrationTests\gtas_vpp_be.IntegrationTests.csproj `
  -c Release
```

### 2. Full disposable SQL integration

Chi chay tren may Windows co LocalDB. Suite tu tao, migrate, nap reference SQL/SP,
seed persona va du lieu scope, seed lai, reset, chay hai fixture song song, mo
phong crash recovery va cleanup.

```powershell
$env:GTAS_QA_SQL_INTEGRATION = '1'
try {
  dotnet test .\gtas_vpp_be.IntegrationTests\gtas_vpp_be.IntegrationTests.csproj `
    -c Release
}
finally {
  Remove-Item Env:GTAS_QA_SQL_INTEGRATION -ErrorAction SilentlyContinue
}
```

Sau test, `SqlLocalDB info` khong duoc con ten bat dau bang `GTASVPP_QA_`, va
`%TEMP%\gtas-vpp-qa` khong duoc con manifest/data cua run da ket thuc.

### 3. UI/E2E tren Aspire + LocalDB

Moi class can dang nhap phai implement `IAuthenticatedUiTest`. Class co the thay
doi server/database phai implement `IMutatingUiTest`; interface nay bao gom ca
yeu cau authenticated.

Authenticated read-only/login run:

```powershell
$env:GTAS_E2E_ISOLATED = '1'
$env:UITEST_BROWSER_EXECUTABLE = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$env:PLAYWRIGHT_HEADLESS = 'true'
try {
  dotnet test .\gtas_vpp_fe.UITests\gtas_vpp_fe.UITests.csproj `
    -c Release --filter 'FullyQualifiedName~LoginTests'
}
finally {
  Remove-Item Env:GTAS_E2E_ISOLATED -ErrorAction SilentlyContinue
  Remove-Item Env:UITEST_BROWSER_EXECUTABLE -ErrorAction SilentlyContinue
  Remove-Item Env:PLAYWRIGHT_HEADLESS -ErrorAction SilentlyContinue
}
```

Mutating run can them chuoi xac nhan chinh xac:

```powershell
$env:GTAS_E2E_ISOLATED = '1'
$env:GTAS_E2E_MUTATION_OPT_IN = 'I_UNDERSTAND_THIS_MUTATES_QA_DATA'
$env:UITEST_BROWSER_EXECUTABLE = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
try {
  dotnet test .\gtas_vpp_fe.UITests\gtas_vpp_fe.UITests.csproj `
    -c Release --filter 'FullyQualifiedName~OrderCreateTests'
}
finally {
  Remove-Item Env:GTAS_E2E_ISOLATED -ErrorAction SilentlyContinue
  Remove-Item Env:GTAS_E2E_MUTATION_OPT_IN -ErrorAction SilentlyContinue
  Remove-Item Env:UITEST_BROWSER_EXECUTABLE -ErrorAction SilentlyContinue
}
```

Truoc khi mo browser, harness bat buoc xac minh backend va frontend deu la URL
HTTP(S) loopback, sau do goi endpoint an `/internal/qa/database-identity` va doi chieu
purpose, run ID, version, environment va database name voi fixture dang so huu.
Thieu opt-in hoac identity khong khop thi test dung truoc browser/mutation.
`UITEST_BASE_URL` chi duoc dung cho anonymous smoke va van phai la HTTP(S) loopback;
authenticated test khong tai su dung server dang chay ben ngoai fixture.

## Persona va du lieu deterministic

Fixture tao sau persona khong phai du lieu that:

| Persona | Username | Scope mau |
|---|---|---|
| Employee | `qa_employee` | own |
| Department peer | `qa_employee_peer` | cung phong ban |
| Other-department employee | `qa_employee_other` | khac phong ban, cung cong ty |
| Manager | `qa_manager` | department |
| Procurement | `qa_procurement` | company |
| System admin | `qa_sysadmin` | quan tri/E2E mac dinh |

Email dung domain `example.invalid`; password khong co gia tri co dinh va khong
duoc ghi vao tai lieu/log. Ba request co GUID co dinh cung tao lan luot own,
department va company scope de integration test co the doi chieu chinh xac.

## CI va gioi han alternative

Workflow Linux build solution va chay 14 safety contracts; 3 LocalDB integration
tests skip mac dinh. Full SQL va browser evidence duoc chay tren Windows local
theo cac lenh tren. Neu sau nay them SQL Server service container cho CI, adapter
moi phai giu nguyen cac invariant: database duy nhat theo run, marker TEST, seed
idempotent, concurrent isolation, stale cleanup, loopback/test endpoint va
backend identity handshake. Khong chi thay connection string roi bo guard.

Tai lieu tham khao:

- [Microsoft .NET Aspire testing overview](https://learn.microsoft.com/en-us/dotnet/aspire/testing/overview)
- [SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Playwright browser management](https://playwright.dev/dotnet/docs/browsers)
