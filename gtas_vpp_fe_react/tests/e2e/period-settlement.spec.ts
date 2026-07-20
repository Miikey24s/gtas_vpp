import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

const periodYear = 2026
const periodMonth = 7
const settlementOneId = '10000000-0000-0000-0000-000000000001'
const settlementTwoId = '10000000-0000-0000-0000-000000000002'
const supplierPrimaryId = '20000000-0000-0000-0000-000000000001'
const supplierAlternateId = '20000000-0000-0000-0000-000000000002'
const priceBookPrimaryId = '30000000-0000-0000-0000-000000000001'
const priceBookAlternateId = '30000000-0000-0000-0000-000000000002'
const missingItemId = '40000000-0000-0000-0000-000000000001'

test('period preview, confirmation, supplier exception, and immutable correction history', async ({
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

  let currentRevision = 0
  const revisions = () => {
    if (currentRevision === 0) return []
    const original = {
      id: settlementOneId,
      periodId: '50000000-0000-0000-0000-000000000001',
      year: periodYear,
      month: periodMonth,
      revisionNumber: 1,
      isCurrentRevision: currentRevision === 1,
      isCorrection: false,
      primarySupplierId: supplierPrimaryId,
      primarySupplierName: 'Nhà cung cấp Phong Phú',
      priceListId: priceBookPrimaryId,
      priceListName: 'Bảng giá PPJ 2026',
      priceListVersion: 1,
      priceAsOfUtc: '2026-07-16T05:00:00Z',
      calculationVersion: 'price-vat-v2-vnd-whole',
      inputHash: 'HASH-PRIMARY-202607',
      currencyCode: 'VND',
      subtotal: 1000000,
      discountAmount: 50000,
      rebateAmount: 0,
      feeAmount: 0,
      shippingAmount: 20000,
      vatAmount: 97000,
      roundingAdjustment: 0,
      grandTotal: 1067000,
      confirmedAtUtc: '2026-07-18T04:00:00Z',
      confirmedByUserId: 9,
      itemCount: 6,
      allocationCount: 3,
    }
    if (currentRevision === 1) return [original]
    return [
      {
        ...original,
        id: settlementTwoId,
        revisionNumber: 2,
        isCurrentRevision: true,
        isCorrection: true,
        supersedesSettlementId: settlementOneId,
        correctionReason: 'Điều chỉnh nguồn cung cho mặt hàng thiếu giá',
        primarySupplierId: supplierAlternateId,
        primarySupplierName: 'Nhà cung cấp Minh Long',
        priceListId: priceBookAlternateId,
        priceListName: 'Bảng giá Minh Long 2026',
        inputHash: 'HASH-EXCEPTION-202607',
        grandTotal: 1085000,
        confirmedAtUtc: '2026-07-19T04:00:00Z',
        confirmedByUserId: 10,
      },
      { ...original, isCurrentRevision: false },
    ]
  }

  const quotePrimary = {
    rank: 1,
    priceListId: priceBookPrimaryId,
    priceListCode: 'PPJ-2026',
    version: 1,
    supplierId: supplierPrimaryId,
    supplierName: 'Nhà cung cấp Phong Phú',
    currencyCode: 'VND',
    effectiveFromUtc: '2026-01-01T00:00:00Z',
    isEligible: true,
    coveredItemCount: 6,
    requestedItemCount: 6,
    coveragePercent: 100,
    subtotal: 1000000,
    discountAmount: 50000,
    rebateAmount: 0,
    feeAmount: 0,
    shippingAmount: 20000,
    vatAmount: 97000,
    grandTotal: 1067000,
    maximumLeadTimeDays: 3,
    missingVppIds: [],
    blockers: [],
  }
  const quoteAlternate = {
    rank: 2,
    priceListId: priceBookAlternateId,
    priceListCode: 'ML-2026',
    version: 1,
    supplierId: supplierAlternateId,
    supplierName: 'Nhà cung cấp Minh Long',
    currencyCode: 'VND',
    effectiveFromUtc: '2026-01-01T00:00:00Z',
    isEligible: false,
    coveredItemCount: 5,
    requestedItemCount: 6,
    coveragePercent: 83.33,
    subtotal: 880000,
    discountAmount: 0,
    rebateAmount: 0,
    feeAmount: 0,
    shippingAmount: 15000,
    vatAmount: 88000,
    grandTotal: 983000,
    maximumLeadTimeDays: 2,
    missingVppIds: [missingItemId],
    blockers: ['MISSING_ITEMS:1'],
  }

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
          userLogin: 'procurement',
          fullName: 'Procurement Demo',
          accessToken: 'period-fixture-token',
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
          userLogin: 'procurement',
          fullName: 'Procurement Demo',
          accountStatus: 'Active',
          mustChangePassword: false,
          primaryDepartmentCode: 'PROC',
          primaryDepartmentName: 'Procurement',
        },
      })
      return
    }
    if (path === '/api/auth/me/permissions') {
      await route.fulfill({
        json: {
          version: 1,
          permissions: ['PERIOD_SETTLE', 'REQUEST_VIEW_OWN'],
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
          currentPeriodMonth: 8,
          previousPeriodYear: periodYear,
          previousPeriodMonth: periodMonth,
          periodState: 'Open',
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
    if (path === '/api/periodsettlement' && method === 'GET') {
      await route.fulfill({ json: currentRevision ? [status()] : [] })
      return
    }
    if (path === `/api/periodsettlement/${periodYear}/${periodMonth}`) {
      await route.fulfill({ json: status() })
      return
    }
    const otherPeriodMatch = path.match(
      /^\/api\/periodsettlement\/(\d{4})\/(\d{1,2})$/,
    )
    if (otherPeriodMatch) {
      await route.fulfill({
        json: {
          year: Number(otherPeriodMatch[1]),
          month: Number(otherPeriodMatch[2]),
          isSettled: false,
          orderCount: 0,
          pendingAdditionalCount: 0,
        },
      })
      return
    }
    if (path === `/api/periodsettlement/current/${periodYear}/${periodMonth}`) {
      const current = revisions().find((revision) => revision.isCurrentRevision)
      if (!current) {
        await route.fulfill({ status: 204 })
      } else {
        await route.fulfill({ json: current })
      }
      return
    }
    if (
      path === `/api/periodsettlement/revisions/${periodYear}/${periodMonth}`
    ) {
      await route.fulfill({ json: revisions() })
      return
    }
    if (path === `/api/catalog/items/${missingItemId}`) {
      await route.fulfill({
        json: {
          id: missingItemId,
          vppCode: 'VPP-PIN-2A',
          vppName: 'Pin đồng hồ 2A',
          uomCode: 'Vỉ',
        },
      })
      return
    }
    if (path === '/api/periodsettlement/preview' && method === 'POST') {
      const body = route.request().postDataJSON() as {
        priceListId?: string
        primarySupplierId?: string
        exceptions?: Array<{
          vppId?: string
          supplierId?: string
          reason?: string
        }>
      }
      if (
        body.priceListId === priceBookAlternateId &&
        body.exceptions?.length
      ) {
        expect(body.exceptions[0]).toMatchObject({
          vppId: missingItemId,
          supplierId: supplierPrimaryId,
          reason: 'Nguồn thay thế đã có giá hợp đồng',
        })
        await route.fulfill({
          json: {
            year: periodYear,
            month: periodMonth,
            priceAsOfUtc: '2026-07-16T05:00:00Z',
            inputHash: 'HASH-EXCEPTION-202607',
            requestedItemCount: 6,
            requestedLineCount: 12,
            pendingAdditionalCount: 0,
            primarySupplierId: supplierAlternateId,
            primaryPriceListId: priceBookAlternateId,
            primaryPriceListVersion: 1,
            primaryQuote: {
              ...quoteAlternate,
              isEligible: true,
              coveredItemCount: 6,
              coveragePercent: 100,
              grandTotal: 1085000,
              missingVppIds: [],
              blockers: [],
            },
            quotes: [quotePrimary, quoteAlternate],
            blockers: [],
            exceptions: [
              {
                vppId: missingItemId,
                supplierId: supplierPrimaryId,
                reason: body.exceptions[0].reason,
                isValid: true,
                grossAmount: 102000,
              },
            ],
          },
        })
        return
      }
      if (body.priceListId === priceBookAlternateId) {
        await route.fulfill({
          json: {
            year: periodYear,
            month: periodMonth,
            priceAsOfUtc: '2026-07-16T05:00:00Z',
            inputHash: 'HASH-ALT-INCOMPLETE',
            requestedItemCount: 6,
            requestedLineCount: 12,
            pendingAdditionalCount: 0,
            quotes: [quotePrimary, quoteAlternate],
            blockers: ['PRICE_BOOK_NOT_COVERED'],
            exceptions: [],
          },
        })
        return
      }
      await route.fulfill({
        json: {
          year: periodYear,
          month: periodMonth,
          priceAsOfUtc: '2026-07-16T05:00:00Z',
          inputHash: 'HASH-PRIMARY-202607',
          requestedItemCount: 6,
          requestedLineCount: 12,
          pendingAdditionalCount: 0,
          primarySupplierId: supplierPrimaryId,
          primaryPriceListId: priceBookPrimaryId,
          primaryPriceListVersion: 1,
          primaryQuote: quotePrimary,
          quotes: [quotePrimary, quoteAlternate],
          blockers: [],
          exceptions: [],
        },
      })
      return
    }
    if (path === '/api/periodsettlement/confirm' && method === 'POST') {
      const body = route.request().postDataJSON() as {
        inputHash?: string
        idempotencyKey?: string
      }
      expect(body.inputHash).toBe('HASH-PRIMARY-202607')
      expect(body.idempotencyKey).toBeTruthy()
      currentRevision = 1
      await route.fulfill({ json: revisions()[0] })
      return
    }
    if (
      path === `/api/periodsettlement/${settlementOneId}/correct` &&
      method === 'POST'
    ) {
      const body = route.request().postDataJSON() as {
        inputHash?: string
        reason?: string
        idempotencyKey?: string
      }
      expect(body.inputHash).toBe('HASH-EXCEPTION-202607')
      expect(body.reason).toBe('Điều chỉnh nguồn cung cho mặt hàng thiếu giá')
      expect(body.idempotencyKey).toBeTruthy()
      currentRevision = 2
      await route.fulfill({ json: revisions()[0] })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })

  function status() {
    const current = revisions().find((revision) => revision.isCurrentRevision)
    return {
      year: periodYear,
      month: periodMonth,
      isSettled: currentRevision > 0,
      settledAt: current?.confirmedAtUtc,
      settledByUserId: current?.confirmedByUserId,
      priceListId: current?.priceListId,
      priceListName: current?.priceListName,
      orderCount: 4,
      pendingAdditionalCount: 0,
      settlementId: current?.id,
      revisionNumber: current?.revisionNumber,
      grandTotal: current?.grandTotal,
      primarySupplierId: current?.primarySupplierId,
      primarySupplierName: current?.primarySupplierName,
    }
  }

  await page.goto('/login')
  await page.getByLabel('Tên đăng nhập').fill('procurement')
  await page
    .getByLabel('Mật khẩu', { exact: true })
    .fill('Correct-Password-123!')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/app\/orders/)

  await page.goto('/app/periods')
  await expect(
    page.getByRole('heading', { name: 'Kỳ và quyết toán' }),
  ).toBeVisible()
  await expect(page.getByText('07/2026', { exact: true }).first()).toBeVisible()
  await page.getByRole('link', { name: 'Mở kỳ' }).first().click()
  await expect(page.getByRole('heading', { name: '07/2026' })).toBeVisible()
  await page.getByRole('link', { name: 'Bắt đầu preview' }).click()

  await expect(
    page.getByRole('heading', { name: 'Preview kỳ 07/2026' }),
  ).toBeVisible()
  await expect(page.getByText('1.067.000 ₫').first()).toBeVisible()
  await page.getByRole('button', { name: 'Xác nhận quyết toán' }).click()
  await page.getByRole('button', { name: 'Xác nhận quyết toán' }).last().click()

  await expect(
    page.getByRole('heading', { name: 'Quyết toán kỳ 07/2026' }),
  ).toBeVisible()
  await expect(page.getByText('Revision #1 hiện hành')).toBeVisible()
  await page.getByRole('link', { name: 'Tạo revision hiệu chỉnh' }).click()

  await expect(
    page.getByRole('heading', { name: 'Hiệu chỉnh kỳ 07/2026' }),
  ).toBeVisible()
  await page.getByRole('button', { name: /Nhà cung cấp Minh Long/ }).click()
  await expect(page.getByText('Pin đồng hồ 2A')).toBeVisible()
  await page.getByLabel('Nhà cung cấp thay thế').click()
  await page.getByRole('option', { name: 'Nhà cung cấp Phong Phú' }).click()
  await page
    .getByLabel('Lý do ngoại lệ')
    .fill('Nguồn thay thế đã có giá hợp đồng')
  await page.getByRole('button', { name: 'Kiểm tra ngoại lệ' }).click()
  await expect(page.getByText('Sẵn sàng xác nhận')).toBeVisible()
  await page.getByRole('button', { name: 'Tạo revision hiệu chỉnh' }).click()
  await page
    .getByLabel('Lý do hiệu chỉnh')
    .fill('Điều chỉnh nguồn cung cho mặt hàng thiếu giá')
  await page.getByRole('button', { name: 'Xác nhận hiệu chỉnh' }).click()

  await expect(page.getByText('Revision #2 hiện hành')).toBeVisible()
  await expect(page.getByText('Revision #1')).toBeVisible()
  await expect(
    page.getByText('Điều chỉnh nguồn cung cho mặt hàng thiếu giá'),
  ).toBeVisible()

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(
    accessibility.violations.filter((violation) =>
      ['critical', 'serious'].includes(violation.impact ?? ''),
    ),
  ).toEqual([])
  expect(networkIssues).toEqual([])
  expect(consoleIssues).toEqual([])
})
