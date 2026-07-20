import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

const currentUser = {
  userId: 1,
  userLogin: 'owner',
  fullName: 'Nguyen An Nam',
  accountStatus: 'Active',
  mustChangePassword: false,
  sessionVersion: 1,
  groupId: '11111111-1111-1111-1111-111111111111',
  groupCode: 'SYSTEM_ADMIN',
  groupName: 'System Administrator',
  memberCompanyCode: 1,
  primaryDepartmentId: '22222222-2222-2222-2222-222222222222',
  primaryDepartmentCode: 'IT',
  primaryDepartmentName: 'Information Technology',
}

test('login, app shell and My Orders remain responsive and accessible', async ({
  page,
}) => {
  let notificationRead = false
  const consoleIssues: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error' || message.type() === 'warning') {
      consoleIssues.push(message.text())
    }
  })

  await page.route('**/api/**', async (route) => {
    const url = new URL(route.request().url())
    const path = url.pathname.toLowerCase()
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
      await route.fulfill({ json: currentUser })
      return
    }
    if (path === '/api/auth/me/permissions') {
      await route.fulfill({
        json: {
          version: 1,
          groupId: currentUser.groupId,
          memberCompanyCode: 1,
          permissions: [
            'REQUEST_VIEW_OWN',
            'REQUEST_CATALOG_VIEW',
            'REQUEST_CREATE',
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
        json: {
          items: [
            {
              id: '66666666-6666-6666-6666-666666666666',
              type: 'additional-order.approved',
              title: 'Đơn bổ sung đã được duyệt',
              message: 'Đơn DEMO-PPJ đã được duyệt.',
              route: '/dashboard?tab=1',
              createdAt: '2026-07-20T08:00:00Z',
              readAt: notificationRead ? '2026-07-20T08:01:00Z' : null,
              isRead: notificationRead,
            },
          ],
          totalCount: 1,
          unreadCount: notificationRead ? 0 : 1,
        },
      })
      return
    }
    if (
      path === '/api/notifications/66666666-6666-6666-6666-666666666666/read'
    ) {
      notificationRead = true
      await route.fulfill({ status: 204 })
      return
    }
    if (path === '/api/notifications/read-all') {
      notificationRead = true
      await route.fulfill({ status: 200, json: { changed: 1 } })
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
          deadlineDate: '2026-08-05T00:00:00Z',
          isDeadlinePassed: false,
          isSubmissionOpen: true,
          canCreateOrder: true,
          canCreateAdditional: false,
          hasPreviousOrder: true,
          canCopyPrevious: true,
        },
      })
      return
    }
    if (path === '/api/vpprequest/categories') {
      await route.fulfill({
        json: [
          {
            id: '77777777-7777-7777-7777-777777777777',
            vppCategoryCode: 'PAPER',
            vppCategoryName: 'Giấy và sản phẩm giấy',
          },
        ],
      })
      return
    }
    if (path === '/api/vpprequest/products') {
      await route.fulfill({
        headers: { 'X-Total-Count': '2' },
        json: [
          {
            id: '88888888-8888-8888-8888-888888888888',
            vppCode: 'VPP-PAPER-A4',
            vppName: 'Giấy A4 trắng 80 gsm',
            vppCategoryName: 'Giấy và sản phẩm giấy',
            uomName: 'Ram',
            defaultSupplierName: 'Công ty Văn phòng phẩm Minh Long',
            defaultPrice: 82500,
            description: 'Giấy in văn phòng định lượng 80 gsm.',
          },
          {
            id: '99999999-9999-9999-9999-999999999999',
            vppCode: 'VPP-PEN-BLUE',
            vppName: 'Bút bi xanh 027',
            vppCategoryName: 'Bút viết',
            uomName: 'Cây',
            defaultSupplierName: 'Nhà sách Phương Nam',
            defaultPrice: 4500,
          },
        ],
      })
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
            defaultPrice: 82500,
          },
          {
            id: '99999999-9999-9999-9999-999999999999',
            vppCode: 'VPP-PEN-BLUE',
            vppName: 'Bút bi xanh 027',
            vppCategoryName: 'Bút viết',
            uomName: 'Cây',
            defaultPrice: 4500,
          },
        ],
      })
      return
    }
    if (path === '/api/vpprequest/orders/previous-items') {
      await route.fulfill({
        json: {
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          year: 2026,
          month: 6,
          items: [
            {
              vppId: '88888888-8888-8888-8888-888888888888',
              vppCode: 'VPP-PAPER-A4',
              vppName: 'Giấy A4 trắng 80 gsm',
              uomName: 'Ram',
              qty: 2,
            },
          ],
        },
      })
      return
    }
    if (
      path === '/api/vpprequest/orders' &&
      route.request().method() === 'POST'
    ) {
      const requestBody = route.request().postDataJSON() as {
        year?: number
        month?: number
        idempotencyKey?: string
        items?: Array<{ qty?: number }>
      }
      expect(requestBody.year).toBe(2026)
      expect(requestBody.month).toBe(7)
      expect(requestBody.idempotencyKey).toBeTruthy()
      expect(requestBody.items?.[0]?.qty).toBe(2)
      await route.fulfill({
        json: {
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          vppCode: 'VPP-202607-NEW',
          year: 2026,
          month: 7,
          status: 1,
          isAdditionalOrder: false,
          totalLines: 1,
          totalQty: 1,
        },
      })
      return
    }
    if (
      path === '/api/vpprequest/orders/33333333-3333-3333-3333-333333333333' &&
      route.request().method() === 'GET'
    ) {
      await route.fulfill({
        json: {
          id: '33333333-3333-3333-3333-333333333333',
          vppCode: 'DEMO-PPJ',
          year: 2026,
          month: 7,
          status: 1,
          revisionNumber: 2,
          isCurrentRevision: true,
          rowVersion: 'AAAAAAAAB9E=',
          canEdit: true,
          canCancel: true,
          totalLines: 2,
          totalQty: 13,
          totalAmount: 292500,
          submittedDate: '2026-07-06T19:00:00Z',
          isAdditionalOrder: false,
          items: [
            {
              id: '44444444-4444-4444-4444-444444444444',
              vppId: '88888888-8888-8888-8888-888888888888',
              vppCode: 'VPP-PAPER-A4',
              vppName: 'Giấy A4 trắng 80 gsm',
              uomName: 'Ram',
              qty: 3,
              currentSinglePrice: 82500,
            },
            {
              id: '55555555-5555-5555-5555-555555555555',
              vppId: '99999999-9999-9999-9999-999999999999',
              vppCode: 'VPP-PEN-BLUE',
              vppName: 'Bút bi xanh 027',
              uomName: 'Cây',
              qty: 10,
              currentSinglePrice: 4500,
            },
          ],
        },
      })
      return
    }
    if (
      path ===
      '/api/vpprequest/orders/33333333-3333-3333-3333-333333333333/history'
    ) {
      await route.fulfill({
        json: {
          requestSeriesId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          currentRequestId: '33333333-3333-3333-3333-333333333333',
          revisions: [
            {
              id: '33333333-3333-3333-3333-333333333333',
              vppCode: 'DEMO-PPJ',
              revisionNumber: 2,
              isCurrentRevision: true,
              status: 1,
              totalLines: 2,
              totalQty: 13,
              updatedAtUtc: '2026-07-06T19:00:00Z',
              requesterName: 'Nguyen An Nam',
            },
            {
              id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
              vppCode: 'DEMO-PPJ-R1',
              revisionNumber: 1,
              isCurrentRevision: false,
              status: 1,
              totalLines: 1,
              totalQty: 3,
              updatedAtUtc: '2026-07-05T09:00:00Z',
              requesterName: 'Nguyen An Nam',
            },
          ],
          timeline: [
            {
              id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
              action: 'UPDATE',
              occurredAt: '2026-07-06T19:00:00Z',
              actorName: 'Nguyen An Nam',
              revisionNumber: 2,
            },
            {
              id: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
              action: 'CREATE',
              occurredAt: '2026-07-05T09:00:00Z',
              actorName: 'Nguyen An Nam',
              revisionNumber: 1,
            },
          ],
        },
      })
      return
    }
    if (path === '/api/vpprequest/my-orders') {
      await route.fulfill({
        json: [
          {
            id: '33333333-3333-3333-3333-333333333333',
            vppCode: 'DEMO-PPJ',
            year: 2026,
            month: 7,
            status: 1,
            totalLines: 2,
            totalQty: 13,
            submittedDate: '2026-07-06T19:00:00Z',
            isAdditionalOrder: false,
            items: [
              {
                id: '44444444-4444-4444-4444-444444444444',
                vppCode: 'VPP-PAPER-A4',
                vppName: 'Giấy A4 trắng 80 gsm',
                uomName: 'Ram',
                qty: 3,
              },
              {
                id: '55555555-5555-5555-5555-555555555555',
                vppCode: 'VPP-PEN-BLUE',
                vppName: 'Bút bi xanh 027',
                uomName: 'Cây',
                qty: 10,
              },
            ],
          },
        ],
      })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })

  for (const viewport of [
    { width: 390, height: 844 },
    { width: 768, height: 1024 },
    { width: 1366, height: 768 },
  ]) {
    await page.setViewportSize(viewport)
    await page.goto('/login')
    await page.getByLabel('Tên đăng nhập').fill('owner')
    await page
      .getByLabel('Mật khẩu', { exact: true })
      .fill('Correct-Password-123!')
    await page.getByRole('button', { name: 'Đăng nhập' }).click()
    await expect(
      page.getByRole('heading', { name: 'Đơn hàng của tôi' }),
    ).toBeVisible()
    await expect(page.getByText('DEMO-PPJ')).toBeVisible()

    if (viewport.width < 768) {
      await page.goto('/app/catalog')
    } else {
      await page.getByRole('link', { name: 'Danh mục mặt hàng' }).click()
    }
    await expect(
      page.getByRole('heading', { name: 'Danh mục mặt hàng' }),
    ).toBeVisible()
    if (viewport.width < 768) {
      await expect(
        page.locator('article').filter({ hasText: 'VPP-PAPER-A4' }),
      ).toBeVisible()
    } else {
      await expect(
        page.locator('tbody').getByText('Giấy A4 trắng 80 gsm'),
      ).toBeVisible()
      await expect(
        page.getByRole('columnheader', { name: 'Đơn giá' }),
      ).toBeVisible()
    }

    await page.getByRole('link', { name: 'Tạo đơn' }).click()
    await expect(
      page.getByRole('heading', { name: 'Lập nhu cầu kỳ này' }),
    ).toBeVisible()
    await expect(
      page.getByText('Giấy A4 trắng 80 gsm', { exact: true }).first(),
    ).toBeVisible()
    const createOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    )
    expect(createOverflow).toBe(false)

    if (viewport.width === 1366) {
      await page.getByRole('button', { name: 'Dùng lại kỳ trước' }).click()
      await expect(page.getByText('1 mặt hàng', { exact: true })).toBeVisible()
      await page.getByRole('button', { name: 'Gửi đơn thường' }).click()
      await expect(page).toHaveURL(/\/app\/orders$/)
      await expect(
        page.getByRole('heading', { name: 'Đơn hàng của tôi' }),
      ).toBeVisible()
    } else {
      await page.goto('/app/orders')
    }

    await expect(
      page.getByRole('heading', { name: 'Đơn hàng của tôi' }),
    ).toBeVisible()

    await page.getByRole('link', { name: 'Xem chi tiết' }).first().click()
    await expect(page.getByRole('heading', { name: 'DEMO-PPJ' })).toBeVisible()
    await expect(page.getByText('292.500 ₫')).toBeVisible()
    const detailOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    )
    expect(detailOverflow).toBe(false)

    await page.getByRole('link', { name: 'Lịch sử' }).click()
    await expect(
      page.getByRole('heading', { name: 'Lịch sử đơn hàng' }),
    ).toBeVisible()
    await expect(page.getByText('Phiên bản 2')).toBeVisible()
    await page.goto('/app/orders')

    if (viewport.width === 1366) {
      await page
        .getByRole('button', { name: /Mở thông báo, 1 mục chưa đọc/ })
        .click()
      await expect(
        page.getByRole('heading', { name: 'Thông báo' }),
      ).toBeVisible()
      await page
        .getByRole('button', { name: /Đơn bổ sung đã được duyệt/ })
        .click()
      await expect(page).toHaveURL(/\/app\/orders$/)
      expect(notificationRead).toBe(true)

      await page.evaluate(() => {
        window.sessionStorage.clear()
        window.dispatchEvent(new Event('focus'))
      })
      await expect(page).toHaveURL(/\/login/)
    }

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    )
    expect(hasHorizontalOverflow).toBe(false)
    await page.evaluate(() => window.sessionStorage.clear())
  }

  const accessibility = await new AxeBuilder({ page }).analyze()
  const seriousViolations = accessibility.violations.filter((violation) =>
    ['critical', 'serious'].includes(violation.impact ?? ''),
  )

  expect(seriousViolations).toEqual([])
  expect(consoleIssues).toEqual([])
})
