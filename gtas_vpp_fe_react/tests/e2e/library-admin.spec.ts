import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

const uomCategoryId = '10000000-0000-0000-0000-000000000001'
const uomId = '10000000-0000-0000-0000-000000000002'
const categoryId = '20000000-0000-0000-0000-000000000001'
const supplierId = '30000000-0000-0000-0000-000000000001'
const departmentId = '40000000-0000-0000-0000-000000000001'
const itemId = '50000000-0000-0000-0000-000000000001'
const priceListId = '60000000-0000-0000-0000-000000000001'
const priceMappingId = '70000000-0000-0000-0000-000000000001'

test('library CRUD, catalog, price book, and item-price lifecycle', async ({
  page,
}) => {
  const consoleIssues: string[] = []
  const networkIssues: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleIssues.push(message.text())
  })
  page.on('pageerror', (error) => consoleIssues.push(error.message))
  page.on('response', (response) => {
    if (response.status() >= 400) {
      networkIssues.push(`${response.status()} ${response.url()}`)
    }
  })

  const classes: Array<Record<string, unknown>> = [
    {
      id: uomCategoryId,
      code: 'Uom',
      name: 'Đơn vị tính',
      moduleName: 'VPP',
      description: 'Đơn vị tính mặt hàng',
      isDeleted: false,
    },
  ]
  const categories: Array<Record<string, unknown>> = [
    {
      id: categoryId,
      vppCategoryCode: 'PAPER',
      vppCategoryName: 'Giấy và sổ',
      description: 'Nhóm giấy văn phòng',
      isDeleted: false,
    },
  ]
  const suppliers: Array<Record<string, unknown>> = [
    {
      id: supplierId,
      supplierShortName: 'PPJ',
      supplierName: 'Công ty Cổ phần Quốc tế Phong Phú',
      city: 'TP. Hồ Chí Minh',
      isDeleted: false,
    },
  ]
  const departments: Array<Record<string, unknown>> = [
    {
      id: departmentId,
      code: 'IT',
      name: 'Công nghệ thông tin',
      parentDepartmentId: null,
      isDeleted: false,
    },
  ]
  const uoms = [
    {
      id: uomId,
      lookupCategoryId: uomCategoryId,
      code: 'Uom_01',
      value: 'Ram',
      sort: 1,
      isDeleted: false,
    },
  ]
  const items: Array<Record<string, unknown>> = []
  const priceLists: Array<Record<string, unknown>> = []
  const prices: Array<Record<string, unknown>> = []

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
          userLogin: 'library-admin',
          fullName: 'Library Admin',
          accessToken: 'library-token',
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
          userLogin: 'library-admin',
          fullName: 'Library Admin',
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
          permissions: ['LIBRARY_VIEW', 'LIBRARY_MANAGE'],
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

    const libraryMatch = path.match(/^\/api\/library\/([^/]+)(?:\/([^/]+))?$/)
    if (libraryMatch) {
      const tableCode = libraryMatch[1]
      const recordId = libraryMatch[2]
      const store =
        tableCode === 'lookup-categories'
          ? classes
          : tableCode === 'lookup-values'
            ? uoms
            : tableCode === 'vpp-categories'
              ? categories
              : tableCode === 'suppliers'
                ? suppliers
                : departments
      if (method === 'GET') {
        await route.fulfill({
          headers: { 'X-Total-Count': String(store.length) },
          json: store,
        })
        return
      }
      const body = request.postDataJSON() as Record<string, unknown>
      if (method === 'POST') {
        const created = {
          ...body,
          id: '80000000-0000-0000-0000-000000000001',
          isDeleted: false,
        }
        store.push(created)
        await route.fulfill({ json: created })
        return
      }
      if (method === 'PUT') {
        const index = store.findIndex((entry) => entry.id === body.id)
        if (index >= 0) store[index] = { ...store[index], ...body }
        await route.fulfill({ json: store[index] })
        return
      }
      if (method === 'PATCH' && recordId) {
        const index = store.findIndex((entry) => entry.id === recordId)
        if (index >= 0) store[index] = { ...store[index], ...body }
        await route.fulfill({ json: store[index] })
        return
      }
    }

    if (path === '/api/catalog/items' && method === 'GET') {
      await route.fulfill({
        headers: { 'X-Total-Count': String(items.length) },
        json: items,
      })
      return
    }
    if (path === '/api/catalog/items' && method === 'POST') {
      const body = request.postDataJSON() as Record<string, unknown>
      const created = {
        ...body,
        id: itemId,
        vppCategoryName: 'Giấy và sổ',
        uomName: 'Ram',
        supplierCount: 0,
        isDeleted: false,
      }
      items.push(created)
      await route.fulfill({ status: 201, json: created })
      return
    }
    const itemStatusMatch = path.match(
      /^\/api\/catalog\/items\/([^/]+)\/status$/,
    )
    if (itemStatusMatch && method === 'PATCH') {
      const body = request.postDataJSON() as { isDeleted?: boolean }
      const item = items.find((entry) => entry.id === itemStatusMatch[1])!
      item.isDeleted = body.isDeleted
      await route.fulfill({ json: item })
      return
    }

    if (path === '/api/vpppricelist' && method === 'GET') {
      await route.fulfill({
        headers: { 'X-Total-Count': String(priceLists.length) },
        json: priceLists,
      })
      return
    }
    if (path === '/api/vpppricelist' && method === 'POST') {
      const body = request.postDataJSON() as Record<string, unknown>
      const created = {
        ...body,
        id: priceListId,
        priceListCode: body.code,
        priceListName: body.name,
        supplierName: 'Công ty Cổ phần Quốc tế Phong Phú',
        status: 'Draft',
        itemCount: 0,
        rowVersion: 'AQIDBA==',
        isDeleted: false,
      }
      priceLists.push(created)
      await route.fulfill({ json: created })
      return
    }
    const publishMatch = path.match(/^\/api\/vpppricelist\/([^/]+)\/publish$/)
    if (publishMatch && method === 'POST') {
      const body = request.postDataJSON() as { reason?: string }
      expect(body.reason).toBe('Đã kiểm tra đủ giá và điều khoản')
      const priceList = priceLists.find(
        (entry) => entry.id === publishMatch[1],
      )!
      priceList.status = 'Published'
      priceList.statusReason = body.reason
      priceList.rowVersion = 'BQYHCA=='
      await route.fulfill({ json: priceList })
      return
    }

    if (path === '/api/vppprice/item-prices' && method === 'GET') {
      await route.fulfill({
        headers: { 'X-Total-Count': String(prices.length) },
        json: prices,
      })
      return
    }
    if (path === '/api/vppprice' && method === 'POST') {
      const body = request.postDataJSON() as Record<string, unknown>
      const created = {
        ...body,
        id: priceMappingId,
        vppItemName: 'Giấy A4 80gsm',
        supplierName: 'Công ty Cổ phần Quốc tế Phong Phú',
        priceListName: 'Bảng giá kiểm thử 2026',
      }
      prices.push({
        vppId: itemId,
        vppCode: 'VPP-A4-80',
        vppName: 'Giấy A4 80gsm',
        categoryName: 'Giấy và sổ',
        uomName: 'Ram',
        priceMappingId,
        price: body.price,
        netPrice: body.netPrice,
        vatRate: body.vatRate,
        minimumOrderQuantity: body.minimumOrderQuantity,
        leadTimeDays: body.leadTimeDays,
        supplierSku: body.supplierSku,
        isDefault: body.isDefault,
        isDeleted: false,
      })
      priceLists[0].itemCount = 1
      await route.fulfill({ json: created })
      return
    }

    await route.fulfill({ status: 404, json: {} })
  })

  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('library-admin')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders/)

  await page.getByRole('link', { name: 'Quản trị danh mục' }).click()
  await expect(
    page.getByRole('heading', { name: 'Định nghĩa lớp dữ liệu' }),
  ).toBeVisible()
  await page.getByRole('button', { name: 'Tạo mới' }).click()
  await page.getByLabel(/^Mã/).fill('BRAND')
  await page.getByLabel(/^Tên/).fill('Thương hiệu')
  await page.getByLabel('Phân hệ').fill('VPP')
  await page.getByRole('button', { name: 'Lưu thay đổi' }).click()
  await expect(page.getByText('Thương hiệu').first()).toBeVisible()

  await page.getByRole('link', { name: 'Mặt hàng', exact: true }).click()
  await expect(
    page.getByRole('heading', { name: 'Danh mục mặt hàng' }),
  ).toBeVisible()
  await page.getByRole('button', { name: 'Tạo mới' }).click()
  await page.getByLabel('Mã mặt hàng *').fill('VPP-A4-80')
  await page.getByLabel('Tên mặt hàng *').fill('Giấy A4 80gsm')
  await page.getByLabel('Nhóm mặt hàng *').click()
  await page.getByRole('option', { name: 'Giấy và sổ' }).click()
  await page.getByLabel('Đơn vị tính *').click()
  await page.getByRole('option', { name: 'Ram' }).click()
  await page.getByRole('button', { name: 'Lưu thay đổi' }).click()
  await expect(page.getByText('Giấy A4 80gsm').first()).toBeVisible()

  await page.getByRole('link', { name: 'Bảng giá' }).click()
  await expect(
    page.getByRole('heading', { name: 'Bảng giá nhà cung cấp' }),
  ).toBeVisible()
  await page.getByRole('button', { name: 'Tạo mới' }).click()
  await page.getByLabel('Mã bảng giá *').fill('PPJ-TEST-2026')
  await page.getByLabel('Tên bảng giá *').fill('Bảng giá kiểm thử 2026')
  await page.getByLabel('Nhà cung cấp *').click()
  await page
    .getByRole('option', { name: 'Công ty Cổ phần Quốc tế Phong Phú' })
    .click()
  await page.getByRole('button', { name: 'Lưu thay đổi' }).click()
  await expect(page.getByText('Bảng giá kiểm thử 2026').first()).toBeVisible()

  await page.getByRole('link', { name: 'Quản lý giá' }).click()
  await expect(
    page.getByRole('heading', { name: 'Giá theo mặt hàng' }),
  ).toBeVisible()
  await page.getByRole('button', { name: 'Tạo mới' }).click()
  await page.getByLabel('Tên mặt hàng *').click()
  await page.getByRole('option', { name: /Giấy A4 80gsm/ }).click()
  await page.getByLabel('Giá niêm yết').fill('85000')
  await page.getByLabel('Giá trước VAT').fill('85000')
  await page.getByLabel('Mã hàng của nhà cung cấp').fill('PPJ-A4-80')
  await page.getByLabel('Đặt mặc định').click()
  await page.getByRole('button', { name: 'Lưu thay đổi' }).click()
  await expect(page.getByText('85.000 ₫').first()).toBeVisible()

  await page.getByRole('link', { name: 'Bảng giá' }).click()
  await expect(
    page.getByRole('heading', { name: 'Bảng giá nhà cung cấp' }),
  ).toBeVisible()
  const priceListRow = page.getByRole('row', { name: /Bảng giá kiểm thử 2026/ })
  await priceListRow.getByRole('button', { name: 'Thao tác' }).click()
  await page.getByRole('menuitem', { name: 'Phát hành' }).click()
  await page
    .getByLabel('Lý do thay đổi')
    .fill('Đã kiểm tra đủ giá và điều khoản')
  await page.getByRole('button', { name: 'Phát hành' }).last().click()
  await expect(priceListRow.getByText('Đã phát hành')).toBeVisible()

  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.getByRole('link', { name: 'Phòng ban' }).click()
  await expect(page.getByRole('heading', { name: 'Phòng ban' })).toBeVisible()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(networkIssues).toEqual([])
  expect(consoleIssues).toEqual([])
})
