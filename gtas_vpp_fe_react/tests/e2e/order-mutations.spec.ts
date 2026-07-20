import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

const originalId = '33333333-3333-3333-3333-333333333333'
const revisedId = 'abababab-abab-abab-abab-abababababab'

test('editing creates a revision and cancellation preserves the workflow', async ({
  page,
}) => {
  const consoleIssues: string[] = []
  const pageErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error' || message.type() === 'warning') {
      consoleIssues.push(message.text())
    }
  })
  page.on('pageerror', (error) => pageErrors.push(error.message))

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
          userID: 1,
          userLogin: 'owner',
          fullName: 'Nguyen An Nam',
          accessToken: 'contract-fixture-token',
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
          userLogin: 'owner',
          fullName: 'Nguyen An Nam',
          accountStatus: 'Active',
          mustChangePassword: false,
          groupCode: 'SYSTEM_ADMIN',
          groupName: 'System Administrator',
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
            'REQUEST_VIEW_OWN',
            'REQUEST_CATALOG_VIEW',
            'REQUEST_UPDATE_OWN',
            'REQUEST_CANCEL_OWN',
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
          currentPeriodYear: 2026,
          currentPeriodMonth: 7,
          previousPeriodYear: 2026,
          previousPeriodMonth: 6,
          deadlineDate: '2026-08-05T00:00:00Z',
          isSubmissionOpen: true,
        },
      })
      return
    }
    if (path === '/api/vpprequest/my-orders') {
      await route.fulfill({ json: [] })
      return
    }
    if (path === '/api/vpprequest/products/lookup') {
      await route.fulfill({
        json: [
          {
            id: '88888888-8888-8888-8888-888888888888',
            vppCode: 'VPP-PAPER-A4',
            vppName: 'Giấy A4 trắng 80 gsm',
            vppCategoryName: 'Giấy và sản phẩm giấy',
            uomName: 'Ram',
          },
          {
            id: '99999999-9999-9999-9999-999999999999',
            vppCode: 'VPP-PEN-BLUE',
            vppName: 'Bút bi xanh 027',
            vppCategoryName: 'Bút viết',
            uomName: 'Cây',
          },
        ],
      })
      return
    }

    const detail = {
      id: path.includes(revisedId) ? revisedId : originalId,
      vppCode: 'DEMO-PPJ',
      year: 2026,
      month: 7,
      status: 1,
      revisionNumber: path.includes(revisedId) ? 3 : 2,
      isCurrentRevision: true,
      rowVersion: path.includes(revisedId) ? 'AAAAAAAAB+I=' : 'AAAAAAAAB9E=',
      canEdit: true,
      canCancel: true,
      totalLines: 1,
      totalQty: path.includes(revisedId) ? 4 : 3,
      submittedDate: '2026-07-06T19:00:00Z',
      isAdditionalOrder: false,
      items: [
        {
          id: '44444444-4444-4444-4444-444444444444',
          vppId: '88888888-8888-8888-8888-888888888888',
          vppCode: 'VPP-PAPER-A4',
          vppName: 'Giấy A4 trắng 80 gsm',
          uomName: 'Ram',
          qty: path.includes(revisedId) ? 4 : 3,
          currentSinglePrice: 82500,
        },
      ],
    }

    if (path === `/api/vpprequest/orders/${originalId}` && method === 'PUT') {
      const body = route.request().postDataJSON() as {
        rowVersion?: string
        idempotencyKey?: string
        items?: Array<{ qty?: number }>
      }
      expect(body.rowVersion).toBe('AAAAAAAAB9E=')
      expect(body.idempotencyKey).toBeTruthy()
      expect(body.items?.[0]?.qty).toBe(4)
      await route.fulfill({
        json: { ...detail, id: revisedId, revisionNumber: 3 },
      })
      return
    }
    if (
      path === `/api/vpprequest/orders/${revisedId}/cancel` &&
      method === 'POST'
    ) {
      const body = route.request().postDataJSON() as {
        rowVersion?: string
        reason?: string
        idempotencyKey?: string
      }
      expect(body.rowVersion).toBe('AAAAAAAAB+I=')
      expect(body.reason).toBe('Không còn nhu cầu sử dụng')
      expect(body.idempotencyKey).toBeTruthy()
      await route.fulfill({ status: 200, json: {} })
      return
    }
    if (
      (path === `/api/vpprequest/orders/${originalId}` ||
        path === `/api/vpprequest/orders/${revisedId}`) &&
      method === 'GET'
    ) {
      await route.fulfill({ json: detail })
      return
    }

    await route.fulfill({ status: 404, json: {} })
  })

  await page.goto('/login')
  await page.waitForTimeout(500)
  expect(pageErrors).toEqual([])
  await page.getByLabel('Tên đăng nhập').fill('owner')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders$/)
  await page.goto(`/app/orders/${originalId}`)

  await page.getByRole('link', { name: 'Chỉnh sửa' }).click()
  await expect(
    page.getByRole('heading', { name: 'Chỉnh sửa đơn' }),
  ).toBeVisible()
  await page.getByLabel('Số lượng của Giấy A4 trắng 80 gsm').fill('4')
  await page.getByRole('button', { name: 'Lưu phiên bản mới' }).click()
  await expect(page).toHaveURL(new RegExp(`/app/orders/${revisedId}$`))
  await expect(page.getByRole('heading', { name: 'DEMO-PPJ' })).toBeVisible()

  await page.getByRole('button', { name: 'Hủy đơn' }).click()
  await page
    .getByPlaceholder('Nêu ngắn gọn lý do hủy đơn')
    .fill('Không còn nhu cầu sử dụng')
  await page.getByRole('button', { name: 'Xác nhận hủy' }).click()
  await expect(page).toHaveURL(/\/app\/orders$/)
  await expect(
    page.getByRole('heading', { name: 'Đơn hàng của tôi' }),
  ).toBeVisible()
  await expect(page.getByText('Chưa có đơn trong kỳ này')).toBeVisible()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(consoleIssues).toEqual([])
})

test('a supplement is bound to the regular order and requires a reason', async ({
  page,
}) => {
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
          userID: 1,
          userLogin: 'owner',
          fullName: 'Nguyen An Nam',
          accessToken: 'contract-fixture-token',
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
          userLogin: 'owner',
          fullName: 'Nguyen An Nam',
          accountStatus: 'Active',
          mustChangePassword: false,
          primaryDepartmentCode: 'IT',
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
            'REQUEST_CATALOG_VIEW',
            'REQUEST_CREATE',
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
          currentPeriodYear: 2026,
          currentPeriodMonth: 7,
          previousPeriodYear: 2026,
          previousPeriodMonth: 6,
          deadlineDate: '2026-08-05T00:00:00Z',
          isSubmissionOpen: true,
          hasCurrentPeriodOrder: true,
          baseRequestId: originalId,
          baseRequestCode: 'DEMO-PPJ',
          canCreateOrder: false,
          canCreateAdditional: true,
          remainingSupplementAttempts: 2,
        },
      })
      return
    }
    if (path === '/api/vpprequest/products/lookup') {
      await route.fulfill({
        json: [
          {
            id: '99999999-9999-9999-9999-999999999999',
            vppCode: 'VPP-PEN-BLUE',
            vppName: 'Bút bi xanh 027',
            vppCategoryName: 'Bút viết',
            uomName: 'Cây',
          },
        ],
      })
      return
    }
    if (path === '/api/vpprequest/my-orders') {
      await route.fulfill({
        json: [
          {
            id: originalId,
            vppCode: 'DEMO-PPJ',
            year: 2026,
            month: 7,
            status: 1,
            isAdditionalOrder: false,
            totalLines: 1,
            totalQty: 3,
          },
        ],
      })
      return
    }
    if (path === '/api/vpprequest/orders' && method === 'POST') {
      const body = route.request().postDataJSON() as {
        isAdditionalOrder?: boolean
        baseRequestId?: string
        supplementReason?: string
        idempotencyKey?: string
        items?: Array<{ vppId?: string; qty?: number }>
      }
      expect(body.isAdditionalOrder).toBe(true)
      expect(body.baseRequestId).toBe(originalId)
      expect(body.supplementReason).toBe('Bổ sung bút cho nhân sự mới')
      expect(body.idempotencyKey).toBeTruthy()
      expect(body.items).toEqual([
        {
          vppId: '99999999-9999-9999-9999-999999999999',
          qty: 1,
          description: null,
        },
      ])
      await route.fulfill({
        json: {
          id: 'cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd',
          vppCode: 'VPP-202607-S1',
          year: 2026,
          month: 7,
          status: 3,
          isAdditionalOrder: true,
          baseRequestId: originalId,
          supplementReason: body.supplementReason,
          totalLines: 1,
          totalQty: 1,
        },
      })
      return
    }

    await route.fulfill({ status: 404, json: {} })
  })

  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('owner')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders$/)
  await page.goto('/app/orders/new')

  await expect(
    page.getByRole('button', { name: /Đơn bổ sung 2/ }),
  ).toHaveAttribute('aria-pressed', 'true')
  await page
    .locator('article')
    .filter({ hasText: 'VPP-PEN-BLUE' })
    .getByRole('button', { name: 'Thêm' })
    .click()
  await page
    .getByPlaceholder('Mô tả nhu cầu phát sinh sau khi đã gửi đơn thường')
    .fill('Bổ sung bút cho nhân sự mới')
  await page.getByRole('button', { name: 'Gửi đơn bổ sung' }).click()

  await expect(page).toHaveURL(/\/app\/orders$/)
  await expect(
    page.getByRole('heading', { name: 'Đơn hàng của tôi' }),
  ).toBeVisible()
})
