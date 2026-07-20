import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

const systemAdminId = '10000000-0000-0000-0000-000000000001'
const companyManagerId = '10000000-0000-0000-0000-000000000002'
const departmentManagerId = '10000000-0000-0000-0000-000000000003'
const employeeId = '10000000-0000-0000-0000-000000000004'
const departmentId = '20000000-0000-0000-0000-000000000001'

test('account activation, role overview, and UI permission workflow', async ({
  page,
}) => {
  const consoleIssues: string[] = []
  const networkIssues: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleIssues.push(message.text())
  })
  page.on('pageerror', (error) => consoleIssues.push(error.message))
  page.on('response', (response) => {
    if (response.status() >= 400)
      networkIssues.push(`${response.status()} ${response.url()}`)
  })

  const groups = [
    {
      id: systemAdminId,
      groupCode: 'SYSTEM_ADMIN',
      groupName: 'Quản trị hệ thống',
      description: 'Quản trị danh tính, quyền truy cập và dữ liệu tham chiếu.',
      memberCompanyCode: 1,
      isDeleted: false,
    },
    {
      id: companyManagerId,
      groupCode: 'COMPANY_MANAGER',
      groupName: 'Quản lý công ty',
      description: 'Theo dõi và quản lý nhu cầu toàn công ty.',
      memberCompanyCode: 1,
      isDeleted: false,
    },
    {
      id: departmentManagerId,
      groupCode: 'DEPARTMENT_MANAGER',
      groupName: 'Quản lý phòng ban',
      description: 'Quản lý nhu cầu thuộc phòng ban chính.',
      memberCompanyCode: 1,
      isDeleted: false,
    },
    {
      id: employeeId,
      groupCode: 'EMPLOYEE',
      groupName: 'Nhân viên',
      description: 'Tạo và theo dõi nhu cầu cá nhân.',
      memberCompanyCode: 1,
      isDeleted: false,
    },
  ]
  const departments = [
    {
      id: departmentId,
      code: 'IT',
      name: 'Công nghệ thông tin',
      isDeleted: false,
    },
  ]
  const users: Array<Record<string, unknown>> = [
    {
      id: '00000000-0000-0000-0000-000000000000',
      userId: 2,
      userLogin: 'pending.user',
      fullName: 'Tài khoản chờ',
      email: 'pending@ppj-international.com',
      groupId: '00000000-0000-0000-0000-000000000000',
      accountStatus: 'PendingApproval',
      sessionVersion: 0,
      isActive: false,
      rowVersion: null,
    },
    {
      id: '30000000-0000-0000-0000-000000000001',
      userId: 3,
      userLogin: 'active.user',
      fullName: 'Người dùng đang hoạt động',
      email: 'active@ppj-international.com',
      groupId: employeeId,
      groupCode: 'EMPLOYEE',
      groupName: 'Nhân viên',
      departmentId,
      departmentName: 'Công nghệ thông tin',
      accountStatus: 'Active',
      sessionVersion: 2,
      isActive: true,
      rowVersion: 'AQIDBA==',
    },
  ]
  const components = [
    {
      groupId: systemAdminId,
      pageId: '40000000-0000-0000-0000-000000000001',
      pageCode: 'Dashboard',
      pageName: 'Bảng điều khiển',
      components: [
        {
          componentId: '50000000-0000-0000-0000-000000000001',
          componentCode: 'MENU_DASHBOARD',
          componentName: 'Điều hướng bảng điều khiển',
          groupId: systemAdminId,
          pageId: '40000000-0000-0000-0000-000000000001',
          groupPageComponentMappingId: '60000000-0000-0000-0000-000000000001',
          isVisible: true,
          isEnable: true,
          isActionGrant: false,
          canConfigure: true,
          administrationMode: 'Configurable',
        },
        {
          componentId: '50000000-0000-0000-0000-000000000002',
          componentCode: 'PERMISSION_MANAGE',
          componentName: 'Quản trị quyền API',
          groupId: systemAdminId,
          pageId: '40000000-0000-0000-0000-000000000001',
          groupPageComponentMappingId: '60000000-0000-0000-0000-000000000002',
          isVisible: true,
          isEnable: true,
          isActionGrant: true,
          canConfigure: false,
          administrationMode: 'ActionMatrix',
        },
      ],
    },
  ]

  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname.toLowerCase()
    const method = request.method()

    if (!path.startsWith('/api/')) {
      await route.continue()
      return
    }

    if (path === '/api/auth/login') {
      await route.fulfill({
        json: {
          userID: 1,
          userLogin: 'system.admin',
          fullName: 'System Admin',
          accessToken: 'access-token',
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
          userLogin: 'system.admin',
          fullName: 'System Admin',
          accountStatus: 'Active',
          mustChangePassword: false,
          groupId: systemAdminId,
          groupCode: 'SYSTEM_ADMIN',
          groupName: 'Quản trị hệ thống',
          primaryDepartmentId: departmentId,
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
          permissions: ['PERMISSION_VIEW', 'PERMISSION_MANAGE'],
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

    if (path === '/api/permission/groups' && method === 'GET') {
      await route.fulfill({
        headers: { 'X-Total-Count': String(groups.length) },
        json: groups,
      })
      return
    }
    if (
      path.match(/^\/api\/permission\/groups\/[^/]+\/page-components$/) &&
      method === 'GET'
    ) {
      await route.fulfill({ json: components })
      return
    }
    if (path === '/api/permission/users' && method === 'GET') {
      const accountStatus = url.searchParams.get('accountStatus')
      const groupId = url.searchParams.get('groupId')
      const departmentFilter = url.searchParams.get('departmentId')
      const membership = url.searchParams.get('hasActiveMembership')
      const search = url.searchParams.get('search')?.toLowerCase()
      const skip = Number(url.searchParams.get('skip') ?? 0)
      const top = Number(url.searchParams.get('top') ?? 20)
      const filtered = users.filter((user) => {
        if (accountStatus && user.accountStatus !== accountStatus) return false
        if (groupId && user.groupId !== groupId) return false
        if (departmentFilter && user.departmentId !== departmentFilter)
          return false
        if (membership === 'true' && !user.isActive) return false
        if (membership === 'false' && user.isActive) return false
        if (search) {
          const haystack = [user.fullName, user.userLogin, user.email]
            .join(' ')
            .toLowerCase()
          if (!haystack.includes(search)) return false
        }
        return true
      })
      await route.fulfill({
        headers: { 'X-Total-Count': String(filtered.length) },
        json: filtered.slice(skip, skip + top),
      })
      return
    }
    if (path === '/api/library/departments' && method === 'GET') {
      await route.fulfill({
        headers: { 'X-Total-Count': String(departments.length) },
        json: departments,
      })
      return
    }
    if (path === '/api/account/admin/activate' && method === 'POST') {
      const body = request.postDataJSON() as {
        accountId: number
        groupId: string
        primaryDepartmentId: string
      }
      const target = users.find((user) => user.userId === body.accountId)!
      Object.assign(target, {
        id: '30000000-0000-0000-0000-000000000002',
        accountStatus: 'Active',
        isActive: true,
        groupId: body.groupId,
        groupCode: 'EMPLOYEE',
        groupName: 'Nhân viên',
        departmentId: body.primaryDepartmentId,
        departmentName: 'Công nghệ thông tin',
        rowVersion: 'BQYHCA==',
        sessionVersion: 1,
      })
      await route.fulfill({
        json: {
          membershipId: target.id,
          accountId: body.accountId,
          groupId: body.groupId,
          primaryDepartmentId: body.primaryDepartmentId,
          isActive: true,
          rowVersion: target.rowVersion,
        },
      })
      return
    }
    if (path === '/api/permission/memberships' && method === 'PUT') {
      await route.fulfill({ json: request.postDataJSON() })
      return
    }
    if (
      path === '/api/permission/memberships/deactivate' &&
      method === 'POST'
    ) {
      await route.fulfill({ json: request.postDataJSON() })
      return
    }
    if (path === '/api/account/admin/reset-password' && method === 'POST') {
      await route.fulfill({
        json: {
          accountId: 3,
          accountStatus: 'Active',
          mustChangePassword: true,
          emailConfirmed: true,
        },
      })
      return
    }
    if (path === '/api/permission/component-mapping' && method === 'PATCH') {
      const body = request.postDataJSON() as {
        pageComponentMappingId: string
        isVisible: boolean
        isEnable: boolean
      }
      const component = components[0].components.find(
        (item) =>
          item.groupPageComponentMappingId === body.pageComponentMappingId,
      )!
      component.isVisible = body.isVisible
      component.isEnable = body.isEnable
      await route.fulfill({ json: component })
      return
    }

    await route.fulfill({ status: 404, json: { message: path } })
  })

  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('system.admin')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders/)

  await page.getByRole('link', { name: 'Quản trị truy cập' }).click()
  await expect(
    page.getByRole('heading', { name: 'Tài khoản và thành viên' }),
  ).toBeVisible()
  await expect(page.getByText('Tài khoản chờ').first()).toBeVisible()

  await page
    .getByRole('button', { name: 'Mở thao tác cho Tài khoản chờ' })
    .click()
  await page.getByRole('menuitem', { name: 'Kích hoạt' }).click()
  const activationSheet = page.getByRole('dialog')
  await activationSheet.getByLabel('Nhóm quyền', { exact: true }).click()
  await page.getByRole('option', { name: 'Nhân viên' }).click()
  await activationSheet.getByLabel('Phòng ban chính').click()
  await page.getByRole('option', { name: 'Công nghệ thông tin' }).click()
  await activationSheet.getByRole('button', { name: 'Kích hoạt' }).click()
  await expect(page.getByText('Tài khoản đã được kích hoạt.')).toBeVisible()

  await page.getByRole('link', { name: 'Nhóm quyền' }).click()
  await expect(
    page.getByRole('heading', { name: 'Nhóm quyền chuẩn' }),
  ).toBeVisible()
  await expect(page.getByText('SYSTEM_ADMIN').first()).toBeVisible()
  await expect(page.getByText('Quyền API cố định').first()).toBeVisible()

  await page.getByRole('link', { name: 'Quyền giao diện' }).click()
  await expect(
    page.getByRole('heading', { name: 'Quyền giao diện theo vai trò' }),
  ).toBeVisible()
  const visibleSwitch = page.getByRole('switch', { name: 'Hiển thị' }).first()
  await expect(visibleSwitch).toBeChecked()
  await visibleSwitch.click()
  await expect(visibleSwitch).not.toBeChecked()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(consoleIssues).toEqual([])
  expect(networkIssues).toEqual([])
})
