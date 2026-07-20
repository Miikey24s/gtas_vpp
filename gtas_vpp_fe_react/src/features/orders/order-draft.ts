import type { VppItemResDto, VppRequestDetailResDto } from '@/api/generated'

export type OrderDraftLine = {
  vppId: string
  vppCode: string
  vppName: string
  uomName: string
  qty: number
  description: string
}

export type OrderDraft = {
  description: string
  supplementReason: string
  lines: OrderDraftLine[]
}

export function productToDraftLine(product: VppItemResDto): OrderDraftLine {
  return {
    vppId: product.id ?? '',
    vppCode: product.vppCode ?? '',
    vppName: product.vppName ?? '',
    uomName: product.uomName ?? product.uomCode ?? '',
    qty: 1,
    description: '',
  }
}

export function previousItemToDraftLine(
  item: VppRequestDetailResDto,
): OrderDraftLine {
  return {
    vppId: item.vppId ?? '',
    vppCode: item.vppCode ?? '',
    vppName: item.vppName ?? '',
    uomName: item.uomName ?? item.uomCode ?? '',
    qty: Math.max(1, item.qty ?? 1),
    description: item.description ?? '',
  }
}

export function readOrderDraft(key: string): OrderDraft | null {
  try {
    const value = window.localStorage.getItem(key)
    if (!value) return null
    const draft = JSON.parse(value) as Partial<OrderDraft>
    if (!Array.isArray(draft.lines)) return null

    return {
      description:
        typeof draft.description === 'string' ? draft.description : '',
      supplementReason:
        typeof draft.supplementReason === 'string'
          ? draft.supplementReason
          : '',
      lines: draft.lines.filter((line): line is OrderDraftLine =>
        Boolean(
          line &&
          typeof line === 'object' &&
          typeof line.vppId === 'string' &&
          line.vppId &&
          typeof line.qty === 'number',
        ),
      ),
    }
  } catch {
    return null
  }
}

export function writeOrderDraft(key: string, draft: OrderDraft) {
  window.localStorage.setItem(key, JSON.stringify(draft))
}

export function removeOrderDraft(key: string) {
  window.localStorage.removeItem(key)
}
