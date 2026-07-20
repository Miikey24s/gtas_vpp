import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

test('account lifecycle covers public recovery and required password change', async ({
  page,
}) => {
  let mustChangePassword = true
  let permissionCalls = 0

  await page.route('**/api/**', async (route) => {
    const url = new URL(route.request().url())
    const path = url.pathname.toLowerCase()
    if (!path.startsWith('/api/')) {
      await route.continue()
      return
    }

    if (path === '/api/account/register') {
      await route.fulfill({
        status: 202,
        json: { code: 'REGISTRATION_ACCEPTED' },
      })
      return
    }
    if (path === '/api/account/password/recovery') {
      await route.fulfill({
        status: 202,
        json: { code: 'PASSWORD_RESET_REQUEST_ACCEPTED' },
      })
      return
    }
    if (path === '/api/account/password/reset') {
      await route.fulfill({ status: 200, json: { code: 'PASSWORD_RESET' } })
      return
    }
    if (path === '/api/account/confirm-email') {
      await route.fulfill({ status: 200, json: { code: 'EMAIL_CONFIRMED' } })
      return
    }
    if (path === '/api/auth/login') {
      await route.fulfill({
        json: {
          userID: 1,
          userLogin: 'owner',
          fullName: 'Nguyen An Nam',
          accessToken: 'temporary-session-token',
          accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
          mustChangePassword: true,
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
          mustChangePassword,
          sessionVersion: 1,
          groupId: '11111111-1111-1111-1111-111111111111',
          groupCode: 'SYSTEM_ADMIN',
          groupName: 'System Administrator',
          memberCompanyCode: 1,
          primaryDepartmentId: '22222222-2222-2222-2222-222222222222',
          primaryDepartmentCode: 'IT',
          primaryDepartmentName: 'Information Technology',
        },
      })
      return
    }
    if (path === '/api/auth/me/permissions') {
      permissionCalls += 1
      await route.fulfill({
        json: {
          version: 1,
          permissions: ['REQUEST_VIEW_OWN'],
          pages: [],
        },
      })
      return
    }
    if (path === '/api/account/password/change') {
      mustChangePassword = false
      await route.fulfill({ status: 200, json: { code: 'PASSWORD_CHANGED' } })
      return
    }
    if (path === '/api/auth/logout') {
      await route.fulfill({ status: 204 })
      return
    }

    await route.fulfill({ status: 404, json: {} })
  })

  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/register')
  await page.getByLabel('Tên đăng nhập').fill('annam')
  await page.getByLabel('Họ tên').fill('Nguyen An Nam')
  await page.getByLabel('Email công ty').fill('annam@example.com')
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Valid-Password1!')
  await page.getByLabel('Nhập lại mật khẩu').fill('Valid-Password1!')
  await page.getByRole('button', { name: 'Đăng ký' }).click()
  await expect(
    page.getByText(/Yêu cầu đăng ký đã được tiếp nhận/),
  ).toBeVisible()

  await page.goto('/forgot-password')
  await page.getByLabel('Email công ty').fill('annam@example.com')
  await page.getByRole('button', { name: 'Gửi' }).click()
  await expect(page.getByText(/Nếu tài khoản tồn tại/)).toBeVisible()

  await page.goto('/reset-password?userId=1&token=valid-token')
  await page.getByLabel('Mật khẩu mới').fill('Another-Password2!')
  await page.getByLabel('Nhập lại mật khẩu').fill('Another-Password2!')
  await page.getByRole('button', { name: 'Đặt lại mật khẩu' }).click()
  await expect(page.getByText(/Mật khẩu đã được đặt lại/)).toBeVisible()

  await page.goto('/account/confirm-email?userId=1&token=valid-token')
  await expect(page.getByText('Email đã được xác nhận.')).toBeVisible()

  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('owner')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Temporary-Password1!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/change-password\?required=1$/)
  await expect(page.getByText(/mật khẩu tạm thời/)).toBeVisible()
  expect(permissionCalls).toBe(0)

  await page.getByLabel('Mật khẩu hiện tại').fill('Temporary-Password1!')
  await page.getByLabel('Mật khẩu mới').fill('Permanent-Password2!')
  await page.getByLabel('Nhập lại mật khẩu').fill('Permanent-Password2!')
  await page.getByRole('button', { name: 'Lưu mật khẩu mới' }).click()
  await expect(page).toHaveURL(/\/login\?passwordChanged=1$/)
  await expect(page.getByText(/Mật khẩu đã được đổi/)).toBeVisible()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    ),
  ).toBe(false)
})

test('account surfaces respect the operating system reduced-motion preference', async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.goto('/login')

  await expect(page.getByRole('heading', { name: 'Đăng nhập' })).toBeVisible()
  expect(
    await page.evaluate(
      () => window.matchMedia('(prefers-reduced-motion: reduce)').matches,
    ),
  ).toBe(true)

  const transitionDuration = await page
    .locator('[data-slot="account-surface"]')
    .evaluate((element) => getComputedStyle(element).transitionDuration)
  expect(['0s', '0.00001s', '0.01ms', '1e-05s']).toContain(transitionDuration)
})
