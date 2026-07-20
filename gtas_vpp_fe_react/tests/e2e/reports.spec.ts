import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

test('reports reconcile data, export, print, and disclose AI fallback evidence', async ({
  page,
}) => {
  const consoleIssues: string[] = []
  const networkIssues: string[] = []
  let insightCalls = 0

  page.on('console', (message) => {
    if (message.type() === 'error') consoleIssues.push(message.text())
  })
  page.on('pageerror', (error) => consoleIssues.push(error.message))
  page.on('response', (response) => {
    if (
      response.status() >= 400 &&
      !response.url().includes('/api/reports/insights')
    ) {
      networkIssues.push(`${response.status()} ${response.url()}`)
    }
  })

  await page.addInitScript(() => {
    window.print = () => {
      document.body.dataset.printCalled = 'true'
    }
  })

  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname.toLowerCase()

    if (!path.startsWith('/api/')) {
      await route.continue()
      return
    }
    if (path === '/api/auth/login') {
      await route.fulfill({
        json: {
          userID: 1,
          userLogin: 'report.owner',
          fullName: 'Report Owner',
          accessToken: 'report-fixture-token',
          accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
          mustChangePassword: false,
        },
      })
      return
    }
    if (path === '/api/auth/me') {
      await route.fulfill({
        json: {
          userId: 1,
          userLogin: 'report.owner',
          fullName: 'Report Owner',
          accountStatus: 'Active',
          mustChangePassword: false,
          primaryDepartmentCode: 'IT',
          primaryDepartmentName: 'Công nghệ thông tin',
        },
      })
      return
    }
    if (path === '/api/auth/me/permissions') {
      await route.fulfill({
        json: {
          version: 1,
          permissions: [
            'REQUEST_VIEW_OWN',
            'REPORT_VIEW_OWN',
            'REPORT_VIEW_DEPARTMENT',
            'REPORT_VIEW_ALL',
            'REPORT_EXPORT',
          ],
          pages: [],
        },
      })
      return
    }
    if (path === '/api/notifications') {
      await route.fulfill({
        json: { items: [], totalCount: 0, unreadCount: 0 },
      })
      return
    }
    if (path === '/api/vpprequest/period-info') {
      await route.fulfill({
        json: {
          periodState: 'Open',
          currentPeriodYear: 2026,
          currentPeriodMonth: 7,
          previousPeriodYear: 2026,
          previousPeriodMonth: 6,
          canCreateOrder: false,
          canCreateAdditional: false,
        },
      })
      return
    }
    if (path === '/api/vpprequest/my-orders') {
      await route.fulfill({
        headers: { 'X-Total-Count': '0' },
        json: [],
      })
      return
    }
    if (path === '/api/reports/summary') {
      await route.fulfill({
        json: {
          scope: url.searchParams.get('scope') ?? 'all',
          year: url.searchParams.get('year')
            ? Number(url.searchParams.get('year'))
            : null,
          month: url.searchParams.get('month')
            ? Number(url.searchParams.get('month'))
            : null,
          generatedAt: '2026-07-20T08:30:00Z',
          availableYears: [2026, 2025],
          totalOrders: 18,
          totalDepartments: 3,
          totalRequesters: 12,
          totalLines: 46,
          totalQuantity: 268,
          totalAmount: 12850000,
          isSettlementReconciled: true,
          settlementId: '10000000-0000-0000-0000-000000000001',
          settlementRevisionNumber: 2,
          settlementPrimarySupplierName: 'Nhà cung cấp Phong Phú',
          settlementGrandTotal: 12850000,
          settlementAllocationTotal: 12850000,
          settlementVariance: 0,
          periodTrend: [
            {
              year: 2026,
              month: 5,
              period: '05/2026',
              orderCount: 14,
              totalQuantity: 210,
              totalAmount: 10200000,
            },
            {
              year: 2026,
              month: 6,
              period: '06/2026',
              orderCount: 16,
              totalQuantity: 244,
              totalAmount: 11500000,
            },
            {
              year: 2026,
              month: 7,
              period: '07/2026',
              orderCount: 18,
              totalQuantity: 268,
              totalAmount: 12850000,
            },
          ],
          statusBreakdown: [
            { status: 1, resourceKey: 'Submitted', orderCount: 12 },
            { status: 3, resourceKey: 'Approved', orderCount: 6 },
          ],
          departmentBreakdown: [
            {
              code: 'IT',
              orderCount: 7,
              totalQuantity: 108,
              totalAmount: 5200000,
            },
            {
              code: 'HR',
              orderCount: 6,
              totalQuantity: 90,
              totalAmount: 4100000,
            },
            {
              code: 'ACC',
              orderCount: 5,
              totalQuantity: 70,
              totalAmount: 3550000,
            },
          ],
          topProducts: [
            {
              productCode: 'VPP-PAPER-A4',
              productName: 'Giấy A4 trắng 80 gsm',
              totalQuantity: 80,
              totalAmount: 6400000,
            },
            {
              productCode: 'VPP-PEN-027',
              productName: 'Bút bi xanh 027',
              totalQuantity: 64,
              totalAmount: 768000,
            },
          ],
        },
      })
      return
    }
    if (path === '/api/reports/insights') {
      insightCalls += 1
      if (insightCalls === 1) {
        await route.fulfill({
          status: 429,
          json: { message: 'Rate limit fixture' },
        })
        return
      }
      if (insightCalls === 2) {
        await route.fulfill({
          json: {
            summary:
              'Giá trị nhu cầu tăng qua ba kỳ; giấy A4 là mặt hàng đóng góp lớn nhất.',
            highlights: ['Giá trị kỳ 07/2026 đạt 12.850.000 ₫.'],
            risks: ['Nhu cầu giấy A4 chiếm tỷ trọng cao.'],
            recommendations: ['Theo dõi tồn kho giấy trước kỳ tiếp theo.'],
            source: 'rules',
            model: null,
            generatedAt: '2026-07-20T08:31:00Z',
            isAiGenerated: false,
          },
        })
        return
      }
      await route.fulfill({
        json: {
          summary:
            'AI xác nhận xu hướng tăng và đề xuất rà soát định mức giấy.',
          highlights: ['Ba kỳ liên tiếp tăng giá trị nhu cầu.'],
          risks: ['Tập trung chi tiêu vào một mặt hàng.'],
          recommendations: ['Đối chiếu định mức theo phòng ban.'],
          source: 'gemini',
          model: 'gemini-test',
          generatedAt: '2026-07-20T08:32:00Z',
          isAiGenerated: true,
        },
      })
      return
    }
    if (path === '/api/reports/export.xlsx') {
      await route.fulfill({
        headers: {
          'Content-Type':
            'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          'Content-Disposition':
            'attachment; filename="GTAS-VPP-all-2026-07.xlsx"',
        },
        body: 'xlsx-fixture',
      })
      return
    }

    await route.fulfill({ status: 404, json: { message: path } })
  })

  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('report.owner')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders/)

  const reportsLink = page.getByRole('link', { name: 'Báo cáo và phân tích' })
  await expect(reportsLink).toHaveAttribute('href', '/app/reports')
  // Direct navigation keeps this fixture focused on the report contract; the
  // shell link itself is still asserted above and is covered by the shared shell journey.
  await page.goto('/app/reports')
  await expect(
    page.getByRole('heading', { name: 'Báo cáo và phân tích' }),
  ).toBeVisible()
  await expect(
    page.getByText('18 đơn · 12.850.000 ₫ giá trị nhu cầu'),
  ).toBeVisible()
  await expect(page.getByText('Giấy A4 trắng 80 gsm')).toBeVisible()

  await page.getByLabel('Năm').click()
  await page.getByRole('option', { name: '2026' }).click()
  await page.getByLabel('Tháng').click()
  await page.getByRole('option', { name: /tháng 7/i }).click()
  await expect(page.getByText('Đã khớp ở phiên bản 2.')).toBeVisible()

  const downloadPromise = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Xuất Excel' }).click()
  const download = await downloadPromise
  expect(download.suggestedFilename()).toBe('GTAS-VPP-all-2026-07.xlsx')

  await page.getByRole('button', { name: 'In trang này' }).click()
  await expect(page.locator('body')).toHaveAttribute(
    'data-print-called',
    'true',
  )

  await expect(page.getByText('Chưa chạy phân tích')).toBeVisible()
  await page.getByRole('button', { name: 'Tạo insight' }).click()
  await expect(page.getByText('Không tạo được insight')).toBeVisible()
  await page.getByRole('button', { name: 'Thử lại' }).click()
  await expect(page.getByText('Rules fallback')).toBeVisible()
  await expect(
    page.getByText(/Bằng chứng: Toàn công ty, 07\/2026/),
  ).toBeVisible()
  await page.getByRole('button', { name: 'Tạo lại' }).click()
  await expect(page.getByText('AI tạo')).toBeVisible()
  await expect(page.getByText('gemini-test')).toBeVisible()

  await page.getByRole('button', { name: 'EN', exact: true }).click()
  await expect(
    page.getByRole('heading', { name: 'Reports and analytics' }),
  ).toBeVisible()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(
    consoleIssues.filter(
      (issue) => !issue.includes('status of 429 (Too Many Requests)'),
    ),
  ).toEqual([])
  expect(networkIssues).toEqual([])
})
