import { afterEach, describe, expect, it } from 'vitest'

import {
  previousItemToDraftLine,
  productToDraftLine,
  readOrderDraft,
  removeOrderDraft,
  writeOrderDraft,
} from '@/features/orders/order-draft'

const key = 'gtas-vpp:test-order-draft'

describe('order draft', () => {
  afterEach(() => removeOrderDraft(key))

  it('maps generated product and previous-order DTOs into one draft shape', () => {
    expect(
      productToDraftLine({
        id: 'product-1',
        vppCode: 'VPP-A4',
        vppName: 'Giấy A4',
        uomName: 'Ram',
      }),
    ).toEqual({
      vppId: 'product-1',
      vppCode: 'VPP-A4',
      vppName: 'Giấy A4',
      uomName: 'Ram',
      qty: 1,
      description: '',
    })

    expect(
      previousItemToDraftLine({
        vppId: 'product-1',
        vppCode: 'VPP-A4',
        vppName: 'Giấy A4',
        uomName: 'Ram',
        qty: 3,
        description: 'In hợp đồng',
      }),
    ).toMatchObject({ qty: 3, description: 'In hợp đồng' })
  })

  it('persists a valid draft and ignores malformed storage', () => {
    const draft = {
      description: 'Nhu cầu tháng 7',
      supplementReason: '',
      lines: [
        {
          vppId: 'product-1',
          vppCode: 'VPP-A4',
          vppName: 'Giấy A4',
          uomName: 'Ram',
          qty: 2,
          description: '',
        },
      ],
    }

    writeOrderDraft(key, draft)
    expect(readOrderDraft(key)).toEqual(draft)

    window.localStorage.setItem(key, '{not-json')
    expect(readOrderDraft(key)).toBeNull()
  })
})
