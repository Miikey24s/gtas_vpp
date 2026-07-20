import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  CalendarDays,
  CircleAlert,
  Clock3,
  FileClock,
  PackageOpen,
  Pencil,
  ShieldX,
  Trash2,
} from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'

import { isApiRequestError, toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestOrdersById,
  postApiVppRequestOrdersByIdCancel,
  type VppRequestDetailResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import {
  formatDateTime,
  formatPeriod,
  getOrderStatusLabel,
  getStatusClass,
} from '@/features/orders/order-format'
import { cn } from '@/lib/utils'

function formatCurrency(value: number | null | undefined, language: string) {
  if (value == null) return '—'
  return new Intl.NumberFormat(language === 'en' ? 'en-US' : 'vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function OrderLine({
  item,
  language,
}: {
  item: VppRequestDetailResDto
  language: string
}) {
  const { t } = useTranslation()
  const total = (item.qty ?? 0) * (item.currentSinglePrice ?? 0)

  return (
    <tr className="border-border border-b last:border-b-0">
      <td className="px-4 py-3">
        <p className="font-medium">{item.vppName || '—'}</p>
        <p className="text-muted-foreground mt-0.5 text-xs">{item.vppCode}</p>
      </td>
      <td className="px-4 py-3 text-right tabular-nums">{item.qty ?? 0}</td>
      <td className="px-4 py-3">{item.uomName || item.uomCode || '—'}</td>
      <td className="px-4 py-3 text-right tabular-nums">
        {formatCurrency(item.currentSinglePrice, language)}
      </td>
      <td className="px-4 py-3 text-right font-medium tabular-nums">
        {formatCurrency(total, language)}
      </td>
      <td className="text-muted-foreground max-w-72 px-4 py-3">
        {item.description || t('orderDetail.noItemNote')}
      </td>
    </tr>
  )
}

export function OrderDetailPage() {
  const { t, i18n } = useTranslation()
  const { orderId } = useParams()
  const [searchParams] = useSearchParams()
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [cancelOpen, setCancelOpen] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const canView = [
    permissions.requestViewOwn,
    permissions.requestViewDepartment,
    permissions.requestViewAll,
  ].some(hasPermission)
  const language = i18n.resolvedLanguage ?? 'vi'
  const source = searchParams.get('from')
  const backTarget =
    source === 'department'
      ? '/app/management/department'
      : source === 'company'
        ? '/app/management/company'
        : source === 'supplements'
          ? '/app/management/supplements'
          : '/app/orders'
  const backLabel =
    source === 'department'
      ? 'orderDetail.backDepartment'
      : source === 'company'
        ? 'orderDetail.backCompany'
        : source === 'supplements'
          ? 'orderDetail.backSupplements'
          : 'orderDetail.back'
  const sourceQuery = source ? `?from=${encodeURIComponent(source)}` : ''

  const orderQuery = useQuery({
    queryKey: ['vpp', 'orders', orderId],
    enabled: canView && Boolean(orderId),
    queryFn: async () => {
      const result = await getApiVppRequestOrdersById({
        path: { id: orderId! },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })
  const order = orderQuery.data
  const canEdit =
    Boolean(order?.canEdit) && hasPermission(permissions.requestUpdateOwn)
  const canCancel =
    Boolean(order?.canCancel) && hasPermission(permissions.requestCancelOwn)

  const cancelMutation = useMutation({
    mutationFn: async () => {
      if (!orderId || !order?.rowVersion) throw new Error('Missing order state')
      const result = await postApiVppRequestOrdersByIdCancel({
        path: { id: orderId },
        body: {
          rowVersion: order.rowVersion,
          reason: cancelReason.trim() || null,
          idempotencyKey: crypto.randomUUID(),
        },
      })
      if (result.error) throw toApiRequestError(result.error, result.response)
    },
    onSuccess: async () => {
      setCancelOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['vpp'] })
      toast.success(t('orderDetail.cancelSuccess'))
      navigate('/app/orders', { replace: true, viewTransition: true })
    },
    onError: (error) => {
      toast.error(
        isApiRequestError(error) && error.status === 409
          ? t('orderDetail.cancelConflict')
          : t('orderDetail.cancelError'),
      )
    },
  })

  if (!canView) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orderDetail.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderDetail.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  if (orderQuery.isLoading) {
    return (
      <div className="mx-auto max-w-[110rem] space-y-5 px-4 py-6 sm:px-6 lg:px-8">
        <Skeleton className="h-32" />
        <Skeleton className="h-72" />
      </div>
    )
  }

  if (orderQuery.isError || !order) {
    const notFound =
      isApiRequestError(orderQuery.error) && orderQuery.error.status === 404
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              {notFound ? (
                <PackageOpen aria-hidden="true" />
              ) : (
                <CircleAlert aria-hidden="true" />
              )}
            </EmptyMedia>
            <EmptyTitle>
              {t(
                notFound
                  ? 'orderDetail.notFoundTitle'
                  : 'orderDetail.errorTitle',
              )}
            </EmptyTitle>
            <EmptyDescription>
              {t(
                notFound
                  ? 'orderDetail.notFoundDescription'
                  : 'orderDetail.errorDescription',
              )}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const items = order.items ?? []

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <Link
        to={backTarget}
        viewTransition
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-2 text-sm transition-colors"
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t(backLabel)}
      </Link>

      <header className="mt-4 flex flex-col gap-5 border-b pb-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-2xl font-semibold tracking-[-0.035em] sm:text-3xl">
              {order.vppCode || t('orderDetail.untitled')}
            </h1>
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
            {order.isAdditionalOrder ? (
              <Badge variant="outline" className="rounded-[4px]">
                {t('orders.additional')}
              </Badge>
            ) : null}
          </div>
          <div className="text-muted-foreground mt-3 flex flex-wrap gap-x-5 gap-y-2 text-sm">
            <span className="inline-flex items-center gap-2">
              <CalendarDays className="size-4" aria-hidden="true" />
              {formatPeriod(order.month, order.year)}
            </span>
            <span className="inline-flex items-center gap-2">
              <Clock3 className="size-4" aria-hidden="true" />
              {t('orderDetail.submitted', {
                value: formatDateTime(order.submittedDate, language),
              })}
            </span>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button asChild variant="outline">
            <Link
              to={`/app/orders/${orderId}/history${sourceQuery}`}
              viewTransition
            >
              <FileClock aria-hidden="true" />
              {t('orderDetail.history')}
            </Link>
          </Button>
          {canEdit ? (
            <Button asChild variant="outline">
              <Link to={`/app/orders/${orderId}/edit`} viewTransition>
                <Pencil aria-hidden="true" />
                {t('orderDetail.edit')}
              </Link>
            </Button>
          ) : null}
          {canCancel ? (
            <Button variant="destructive" onClick={() => setCancelOpen(true)}>
              <Trash2 aria-hidden="true" />
              {t('orderDetail.cancel')}
            </Button>
          ) : null}
        </div>
      </header>

      <section className="border-border mt-5 grid grid-cols-2 border md:grid-cols-4">
        {[
          [t('orderDetail.totalItems'), order.totalLines ?? items.length],
          [t('orderDetail.totalQuantity'), order.totalQty ?? 0],
          [
            t('orderDetail.totalAmount'),
            formatCurrency(order.totalAmount, language),
          ],
          [t('orderDetail.revision'), order.revisionNumber ?? 1],
        ].map(([label, value], index) => (
          <div
            key={String(label)}
            className={cn(
              'min-w-0 p-4 sm:p-5',
              index % 2 === 1 && 'border-l',
              index >= 2 && 'border-t md:border-t-0',
              index > 0 && 'md:border-l',
            )}
          >
            <p className="text-lg font-semibold tabular-nums sm:text-xl">
              {value}
            </p>
            <p className="text-muted-foreground mt-1 truncate text-xs">
              {label}
            </p>
          </div>
        ))}
      </section>

      {order.supplementReason || order.description || order.cancelReason ? (
        <section className="border-border mt-5 grid gap-4 border p-4 sm:p-5 md:grid-cols-2">
          <div>
            <h2 className="text-sm font-semibold">
              {t('orderDetail.context')}
            </h2>
            <p className="text-muted-foreground mt-2 text-sm leading-6">
              {order.supplementReason || order.description || '—'}
            </p>
          </div>
          {order.cancelReason ? (
            <div>
              <h2 className="text-sm font-semibold">
                {t('orderDetail.cancelReason')}
              </h2>
              <p className="text-muted-foreground mt-2 text-sm leading-6">
                {order.cancelReason}
              </p>
            </div>
          ) : null}
        </section>
      ) : null}

      <section className="mt-6" aria-labelledby="order-items-title">
        <div className="mb-3 flex items-center gap-2">
          <h2 id="order-items-title" className="text-base font-semibold">
            {t('orderDetail.items')}
          </h2>
          <Badge variant="outline" className="rounded-[4px]">
            {items.length}
          </Badge>
        </div>
        {items.length === 0 ? (
          <Alert>
            <PackageOpen aria-hidden="true" />
            <AlertTitle>{t('orderDetail.noItemsTitle')}</AlertTitle>
            <AlertDescription>
              {t('orderDetail.noItemsDescription')}
            </AlertDescription>
          </Alert>
        ) : (
          <div className="border-border overflow-x-auto border">
            <table className="w-full min-w-[60rem] border-collapse text-left text-sm">
              <thead className="bg-muted/35 text-muted-foreground text-xs">
                <tr>
                  <th className="px-4 py-3 font-medium">{t('orders.item')}</th>
                  <th className="px-4 py-3 text-right font-medium">
                    {t('orders.quantity')}
                  </th>
                  <th className="px-4 py-3 font-medium">{t('orders.unit')}</th>
                  <th className="px-4 py-3 text-right font-medium">
                    {t('orderDetail.unitPrice')}
                  </th>
                  <th className="px-4 py-3 text-right font-medium">
                    {t('orderDetail.lineTotal')}
                  </th>
                  <th className="px-4 py-3 font-medium">
                    {t('orderDetail.itemNote')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.map((item, index) => (
                  <OrderLine
                    key={item.id ?? index}
                    item={item}
                    language={language}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <Dialog open={cancelOpen} onOpenChange={setCancelOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('orderDetail.cancelTitle')}</DialogTitle>
            <DialogDescription>
              {t('orderDetail.cancelDescription')}
            </DialogDescription>
          </DialogHeader>
          <label>
            <span className="mb-1.5 block text-xs font-medium">
              {t('orderDetail.cancelReasonLabel')}
            </span>
            <Textarea
              value={cancelReason}
              maxLength={500}
              onChange={(event) => setCancelReason(event.target.value)}
              placeholder={t('orderDetail.cancelReasonPlaceholder')}
            />
          </label>
          {!order.rowVersion ? (
            <Alert variant="destructive">
              <CircleAlert aria-hidden="true" />
              <AlertDescription>
                {t('orderDetail.missingVersion')}
              </AlertDescription>
            </Alert>
          ) : null}
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelOpen(false)}>
              {t('actions.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => cancelMutation.mutate()}
              disabled={cancelMutation.isPending || !order.rowVersion}
            >
              {cancelMutation.isPending ? <Spinner aria-hidden="true" /> : null}
              {t('orderDetail.confirmCancel')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
