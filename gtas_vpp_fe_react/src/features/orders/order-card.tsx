import { ChevronDown, Clock3, ExternalLink, Package } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import type { VppRequestResDto } from '@/api/generated'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  formatDateTime,
  getOrderStatusLabel,
  getStatusClass,
} from '@/features/orders/order-format'
import { cn } from '@/lib/utils'

export function OrderCard({
  order,
  defaultOpen = false,
}: {
  order: VppRequestResDto
  defaultOpen?: boolean
}) {
  const { i18n, t } = useTranslation()
  const items = order.items ?? []

  return (
    <details
      className="group bg-card border-border overflow-hidden border"
      open={defaultOpen}
    >
      <summary className="hover:bg-muted/40 focus-visible:ring-ring flex cursor-pointer list-none items-center justify-between gap-4 px-4 py-4 focus-visible:ring-2 focus-visible:outline-none sm:px-5 [&::-webkit-details-marker]:hidden">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className="truncate font-semibold tracking-tight">
              {order.vppCode || '—'}
            </span>
            {order.isAdditionalOrder ? (
              <Badge variant="outline" className="rounded-[4px]">
                {t('orders.additional')}
              </Badge>
            ) : null}
            <Badge
              variant="outline"
              className={cn('rounded-[4px]', getStatusClass(order.status))}
            >
              {getOrderStatusLabel(
                order.status,
                order.isDeadlinePassed,
                order.isAdditionalOrder,
                t,
              )}
            </Badge>
          </div>
          <p className="text-muted-foreground mt-1 text-xs">
            {t('orders.submittedAt', {
              value: formatDateTime(
                order.submittedDate,
                i18n.resolvedLanguage ?? 'vi',
              ),
            })}
          </p>
        </div>
        <div className="flex shrink-0 items-center gap-4">
          <div className="hidden text-right sm:block">
            <p className="text-sm font-semibold tabular-nums">
              {order.totalQty ?? 0}
            </p>
            <p className="text-muted-foreground text-[11px]">
              {t('orders.totalQuantity')}
            </p>
          </div>
          <ChevronDown
            className="text-muted-foreground size-4 transition-transform group-open:rotate-180"
            aria-hidden="true"
          />
        </div>
      </summary>

      <div className="border-border border-t">
        {items.length === 0 ? (
          <div className="text-muted-foreground flex items-center gap-2 px-5 py-5 text-sm">
            <Package className="size-4" aria-hidden="true" />
            {t('orders.noLineItems')}
          </div>
        ) : (
          <>
            <div className="divide-border divide-y sm:hidden">
              {items.map((item, index) => (
                <div
                  key={item.id ?? `${order.id}-${index}`}
                  className="grid grid-cols-[1fr_auto] gap-3 px-4 py-3 text-sm"
                >
                  <div className="min-w-0">
                    <p className="truncate font-medium">
                      {item.vppName || '—'}
                    </p>
                    <p className="text-muted-foreground mt-0.5 truncate text-xs">
                      {item.vppCode}
                    </p>
                  </div>
                  <p className="text-right font-medium tabular-nums">
                    {item.qty ?? 0}{' '}
                    <span className="text-muted-foreground text-xs font-normal">
                      {item.uomName || item.uomCode}
                    </span>
                  </p>
                </div>
              ))}
            </div>

            <div className="hidden overflow-x-auto sm:block">
              <table className="w-full min-w-[42rem] border-collapse text-left text-sm">
                <thead className="bg-muted/35 text-muted-foreground text-xs">
                  <tr>
                    <th className="w-14 px-5 py-2.5 font-medium">#</th>
                    <th className="px-3 py-2.5 font-medium">
                      {t('orders.item')}
                    </th>
                    <th className="w-32 px-3 py-2.5 text-right font-medium">
                      {t('orders.quantity')}
                    </th>
                    <th className="w-36 px-5 py-2.5 font-medium">
                      {t('orders.unit')}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-border divide-y">
                  {items.map((item, index) => (
                    <tr key={item.id ?? `${order.id}-${index}`}>
                      <td className="text-muted-foreground px-5 py-3 tabular-nums">
                        {index + 1}
                      </td>
                      <td className="px-3 py-3">
                        <p className="font-medium">{item.vppName || '—'}</p>
                        <p className="text-muted-foreground mt-0.5 text-xs">
                          {item.vppCode}
                        </p>
                      </td>
                      <td className="px-3 py-3 text-right font-medium tabular-nums">
                        {item.qty ?? 0}
                      </td>
                      <td className="px-5 py-3">
                        {item.uomName || item.uomCode || '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
        {order.id ? (
          <div className="border-border flex flex-wrap justify-end gap-2 border-t px-4 py-3 sm:px-5">
            <Button asChild variant="ghost" size="sm">
              <Link to={`/app/orders/${order.id}/history`} viewTransition>
                <Clock3 aria-hidden="true" />
                {t('orders.viewHistory')}
              </Link>
            </Button>
            <Button asChild variant="outline" size="sm">
              <Link to={`/app/orders/${order.id}`} viewTransition>
                <ExternalLink aria-hidden="true" />
                {t('orders.viewDetails')}
              </Link>
            </Button>
          </div>
        ) : null}
      </div>
    </details>
  )
}
