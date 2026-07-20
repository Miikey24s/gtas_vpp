import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

const pendingApproveId = '10101010-1010-1010-1010-101010101010'
const pendingRejectId = '20202020-2020-2020-2020-202020202020'

test('management scopes and supplement decisions stay permission-aware', async ({
  page,
}) => {
  let pendingIds = [pendingApproveId, pendingRejectId]
  const consoleIssues: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error' || message.type() === 'warning') {
      consoleIssues.push(message.text())
    }
  })

  const managementOrders = [
    {
      id: '30303030-3030-3030-3030-303030303030',
      vppCode: 'VPP-IT-0726',
      requesterName: 'Nguyen An Nam',
      departmentCode: 'IT',
      year: 2026,
      month: 7,
      status: 1,
      totalLines: 3,
      totalQty: 18,
      totalAmount: 420000,
      submittedDate: '2026-07-06T10:00:00Z',
      isAdditionalOrder: false,
    },
    {
      id: '40404040-4040-4040-4040-404040404040',
      vppCode: 'VPP-HR-0726',
      requesterName: 'Tran Minh Anh',
      departmentCode: 'HR',
      year: 2026,
      month: 7,
      status: 7,
      totalLines: 2,
      totalQty: 9,
      totalAmount: 275000,
      submittedDate: '2026-07-07T09:15:00Z',
      isAdditionalOrder: true,
    },
  ]

  const pendingOrders = () =>
    [
      {
        id: pendingApproveId,
        vppCode: 'VPP-IT-S1',
        requesterName: 'Le Quang Huy',
        departmentCode: 'IT',
        year: 2026,
        month: 7,
        status: 6,
        rowVersion: 'AAAAAAAACAE=',
        totalLines: 2,
        totalQty: 5,
        supplementReason: 'Bổ sung vật tư cho nhân sự mới',
        submittedDate: '2026-07-18T08:00:00Z',
        isAdditionalOrder: true,
      },
      {
        id: pendingRejectId,
        vppCode: 'VPP-HR-S1',
        requesterName: 'Pham Thu Ha',
        departmentCode: 'HR',
        year: 2026,
        month: 7,
        status: 6,
        rowVersion: 'AAAAAAAACAI=',
        totalLines: 1,
        totalQty: 2,
        supplementReason: 'Bổ sung giấy màu cho chương trình nội bộ',
        submittedDate: '2026-07-18T09:00:00Z',
        isAdditionalOrder: true,
      },
    ].filter((order) => pendingIds.includes(order.id))

  await page.route('**/api/**', async (route) => {
    const url = new URL(route.request().url())
    const path = url.pathname.toLowerCase()
    const method = route.request().method()
    if (!path.startsWith('/api/')) {
      await route.continue()
      return
    }

    if (path === '/api/auth/login') {
      await route.fulfill({
        json: {
          userID: 9,
          userLogin: 'manager',
          fullName: 'Manager Demo',
          accessToken: 'management-fixture-token',
          accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
          mustChangePassword: false,
        },
      })
      return
    }
    if (path === '/api/auth/me') {
      await route.fulfill({
        json: {
          userId: 9,
          userLogin: 'manager',
          fullName: 'Manager Demo',
          accountStatus: 'Active',
          mustChangePassword: false,
          primaryDepartmentCode: 'IT',
          primaryDepartmentName: 'Information Technology',
        },
      })
      return
    }
    if (path === '/api/auth/me/permissions') {
      await route.fulfill({
        json: {
          version: 1,
          permissions: [
            'REQUEST_VIEW_DEPARTMENT',
            'REQUEST_VIEW_ALL',
            'REQUEST_APPROVE',
            'REQUEST_REJECT',
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
    if (path === '/api/vpprequest/department-orders') {
      await route.fulfill({
        headers: {
          'X-Total-Count': '1',
          'X-Total-Lines': '3',
          'X-Total-Qty': '18',
        },
        json: [managementOrders[0]],
      })
      return
    }
    if (path === '/api/vpprequest/all-orders') {
      const department = url.searchParams.get('departmentCode')
      const orders = department
        ? managementOrders.filter(
            (order) => order.departmentCode === department,
          )
        : managementOrders
      await route.fulfill({
        headers: {
          'X-Total-Count': String(orders.length),
          'X-Total-Lines': String(
            orders.reduce((total, order) => total + order.totalLines, 0),
          ),
          'X-Total-Qty': String(
            orders.reduce((total, order) => total + order.totalQty, 0),
          ),
          'X-Total-Amount': String(
            orders.reduce((total, order) => total + order.totalAmount, 0),
          ),
        },
        json: orders,
      })
      return
    }
    if (path === '/api/vpprequest/additional-orders/pending') {
      const orders = pendingOrders()
      await route.fulfill({
        headers: {
          'X-Total-Count': String(orders.length),
          'X-Total-Lines': String(
            orders.reduce((total, order) => total + order.totalLines, 0),
          ),
          'X-Total-Qty': String(
            orders.reduce((total, order) => total + order.totalQty, 0),
          ),
        },
        json: orders,
      })
      return
    }
    if (
      path ===
        `/api/vpprequest/additional-orders/${pendingApproveId}/approve` &&
      method === 'POST'
    ) {
      const body = route.request().postDataJSON() as {
        rowVersion?: string
        idempotencyKey?: string
      }
      expect(body.rowVersion).toBe('AAAAAAAACAE=')
      expect(body.idempotencyKey).toBeTruthy()
      pendingIds = pendingIds.filter((id) => id !== pendingApproveId)
      await route.fulfill({ status: 200, json: {} })
      return
    }
    if (
      path === `/api/vpprequest/additional-orders/${pendingRejectId}/reject` &&
      method === 'POST'
    ) {
      const body = route.request().postDataJSON() as {
        rowVersion?: string
        reason?: string
        idempotencyKey?: string
      }
      expect(body.rowVersion).toBe('AAAAAAAACAI=')
      expect(body.reason).toBe('Nhu cầu chưa đủ căn cứ')
      expect(body.idempotencyKey).toBeTruthy()
      pendingIds = pendingIds.filter((id) => id !== pendingRejectId)
      await route.fulfill({ status: 200, json: {} })
      return
    }

    await route.fulfill({ status: 404, json: {} })
  })

  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('manager')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders/)

  await page.goto('/app/management/department')
  await expect(
    page.getByRole('heading', { name: 'Đơn hàng phòng ban' }),
  ).toBeVisible()
  await expect(page.locator('tbody').getByText('VPP-IT-0726')).toBeVisible()

  await page.goto('/app/management/company')
  await expect(
    page.getByRole('heading', { name: 'Đơn hàng toàn công ty' }),
  ).toBeVisible()
  await expect(page.getByText('695.000 ₫')).toBeVisible()
  await page.getByPlaceholder('Ví dụ: IT').fill('IT')
  await expect(page.locator('tbody').getByText('VPP-HR-0726')).toHaveCount(0)

  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/app/management/department')
  await expect(
    page.locator('article').filter({ hasText: 'VPP-IT-0726' }),
  ).toBeVisible()
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    ),
  ).toBe(false)

  await page.setViewportSize({ width: 1366, height: 768 })
  await page.goto('/app/management/supplements')
  await expect(
    page.getByRole('heading', { name: 'Duyệt đơn bổ sung' }),
  ).toBeVisible()
  await page
    .locator('article')
    .filter({ hasText: 'VPP-IT-S1' })
    .getByRole('button', { name: 'Duyệt' })
    .click()
  await page.getByRole('button', { name: 'Xác nhận duyệt' }).click()
  await expect(page.getByText('VPP-IT-S1')).toHaveCount(0)

  await page
    .locator('article')
    .filter({ hasText: 'VPP-HR-S1' })
    .getByRole('button', { name: 'Từ chối' })
    .click()
  await page
    .getByPlaceholder('Tối thiểu 5 ký tự')
    .fill('Nhu cầu chưa đủ căn cứ')
  await page.getByRole('button', { name: 'Xác nhận từ chối' }).click()
  await expect(page.getByText('Không có đơn bổ sung đang chờ')).toBeVisible()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(consoleIssues).toEqual([])
})
