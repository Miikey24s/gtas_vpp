import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Check,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  ExternalLink,
  RefreshCw,
  ShieldCheck,
  ShieldX,
  X,
} from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'

import { isApiRequestError, toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestAdditionalOrdersPending,
  postApiVppRequestAdditionalOrdersByIdApprove,
  postApiVppRequestAdditionalOrdersByIdReject,
  type VppRequestResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
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
import { formatDateTime, formatPeriod } from '@/features/orders/order-format'

const pageSize = 12

function parseHeader(response: Response | undefined, name: string) {
  const value = Number(response?.headers.get(name))
  return Number.isFinite(value) ? value : 0
}

export function SupplementApprovalsPage() {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const [page, setPage] = useState(0)
  const [selectedOrder, setSelectedOrder] = useState<VppRequestResDto | null>(
    null,
  )
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const decisionKeys = useRef(new Map<string, string>())
  const language = i18n.resolvedLanguage ?? 'vi'
  const canApprove = hasPermission(permissions.requestApprove)
  const canReject = hasPermission(permissions.requestReject)
  const canView = canApprove || canReject

  const pendingQuery = useQuery({
    queryKey: ['vpp', 'management', 'supplements', page],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiVppRequestAdditionalOrdersPending({
        query: { skip: page * pageSize, top: pageSize },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return {
        orders: result.data,
        totalCount: parseHeader(result.response, 'X-Total-Count'),
        totalLines: parseHeader(result.response, 'X-Total-Lines'),
        totalQty: parseHeader(result.response, 'X-Total-Qty'),
      }
    },
  })

  const getDecisionKey = (orderId: string, action: string) => {
    const mapKey = `${orderId}:${action}`
    const existing = decisionKeys.current.get(mapKey)
    if (existing) return existing
    const created = crypto.randomUUID()
    decisionKeys.current.set(mapKey, created)
    return created
  }

  const closeDialog = () => {
    setSelectedOrder(null)
    setDecision(null)
    setRejectReason('')
  }

  const decisionMutation = useMutation({
    mutationFn: async () => {
      if (!selectedOrder?.id || !selectedOrder.rowVersion || !decision) {
        throw new Error('Missing decision state')
      }
      if (decision === 'approve') {
        const result = await postApiVppRequestAdditionalOrdersByIdApprove({
          path: { id: selectedOrder.id },
          body: {
            rowVersion: selectedOrder.rowVersion,
            idempotencyKey: getDecisionKey(selectedOrder.id, decision),
          },
        })
        if (result.error) throw toApiRequestError(result.error, result.response)
        return
      }

      const normalizedReason = rejectReason.trim()
      if (normalizedReason.length < 5) {
        throw new Error('REASON_TOO_SHORT')
      }
      const result = await postApiVppRequestAdditionalOrdersByIdReject({
        path: { id: selectedOrder.id },
        body: {
          rowVersion: selectedOrder.rowVersion,
          reason: normalizedReason,
          idempotencyKey: getDecisionKey(selectedOrder.id, decision),
        },
      })
      if (result.error) throw toApiRequestError(result.error, result.response)
    },
    onSuccess: async () => {
      const action = decision
      closeDialog()
      await queryClient.invalidateQueries({
        queryKey: ['vpp', 'management', 'supplements'],
      })
      toast.success(
        t(
          action === 'approve'
            ? 'supplements.approveSuccess'
            : 'supplements.rejectSuccess',
        ),
      )
    },
    onError: (error) => {
      if (error instanceof Error && error.message === 'REASON_TOO_SHORT') {
        toast.error(t('supplements.reasonTooShort'))
        return
      }
      toast.error(
        isApiRequestError(error) && error.status === 409
          ? t('supplements.conflict')
          : t('supplements.decisionError'),
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
            <EmptyTitle>{t('supplements.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('supplements.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const orders = pendingQuery.data?.orders ?? []
  const total = pendingQuery.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  const departmentCount = new Set(orders.map((order) => order.departmentCode))
    .size

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {t('supplements.eyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.035em] sm:text-3xl">
            {t('supplements.title')}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t('supplements.description')}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => void pendingQuery.refetch()}
          disabled={pendingQuery.isFetching}
        >
          <RefreshCw
            className={pendingQuery.isFetching ? 'animate-spin' : ''}
            aria-hidden="true"
          />
          {t('actions.refresh')}
        </Button>
      </header>

      <section className="border-border mt-6 grid grid-cols-2 border lg:grid-cols-4">
        {[
          [t('supplements.pendingOrders'), total],
          [t('supplements.totalItems'), pendingQuery.data?.totalLines ?? 0],
          [t('supplements.totalQuantity'), pendingQuery.data?.totalQty ?? 0],
          [t('supplements.departments'), departmentCount],
        ].map(([label, value], index) => (
          <div
            key={String(label)}
            className={`min-w-0 p-4 sm:p-5 ${index % 2 === 1 ? 'border-l' : ''} ${index >= 2 ? 'border-t lg:border-t-0' : ''} ${index > 0 ? 'lg:border-l' : ''}`}
          >
            <p className="text-xl font-semibold tabular-nums">{value}</p>
            <p className="text-muted-foreground mt-1 truncate text-xs">
              {label}
            </p>
          </div>
        ))}
      </section>

      <div className="mt-5">
        {pendingQuery.isLoading ? (
          <div className="grid gap-4 lg:grid-cols-2">
            {Array.from({ length: 4 }).map((_, index) => (
              <Skeleton key={index} className="h-64" />
            ))}
          </div>
        ) : null}
        {pendingQuery.isError ? (
          <Empty className="min-h-72 border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <CircleAlert aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{t('supplements.errorTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('supplements.errorDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}
        {!pendingQuery.isLoading &&
        !pendingQuery.isError &&
        orders.length === 0 ? (
          <Empty className="min-h-72 border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <ShieldCheck aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{t('supplements.emptyTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('supplements.emptyDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}
        {orders.length > 0 ? (
          <div className="grid gap-4 lg:grid-cols-2">
            {orders.map((order, index) => (
              <article
                key={order.id ?? index}
                className="border-border flex min-w-0 flex-col border"
              >
                <div className="p-4 sm:p-5">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <h2 className="font-semibold">
                          {order.vppCode || '—'}
                        </h2>
                        <Badge
                          variant="outline"
                          className="rounded-[4px] border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-300"
                        >
                          {t('orders.status.pending')}
                        </Badge>
                      </div>
                      <p className="text-muted-foreground mt-1 text-xs">
                        {order.requesterName || '—'} ·{' '}
                        {order.departmentCode || '—'} ·{' '}
                        {formatPeriod(order.month, order.year)}
                      </p>
                    </div>
                    <Button asChild variant="ghost" size="sm">
                      <Link
                        to={`/app/orders/${order.id}?from=supplements`}
                        viewTransition
                      >
                        {t('supplements.details')}
                        <ExternalLink aria-hidden="true" />
                      </Link>
                    </Button>
                  </div>

                  <div className="bg-muted/35 mt-4 p-3">
                    <p className="text-muted-foreground text-xs font-medium uppercase">
                      {t('supplements.reason')}
                    </p>
                    <p className="mt-1 text-sm leading-6">
                      {order.supplementReason || order.description || '—'}
                    </p>
                  </div>

                  <dl className="border-border mt-4 grid grid-cols-3 border-t pt-4 text-sm">
                    <div>
                      <dt className="text-muted-foreground text-xs">
                        {t('supplements.items')}
                      </dt>
                      <dd className="mt-1 font-medium tabular-nums">
                        {order.totalLines ?? order.items?.length ?? 0}
                      </dd>
                    </div>
                    <div className="border-border border-l pl-4">
                      <dt className="text-muted-foreground text-xs">
                        {t('supplements.quantity')}
                      </dt>
                      <dd className="mt-1 font-medium tabular-nums">
                        {order.totalQty ?? 0}
                      </dd>
                    </div>
                    <div className="border-border border-l pl-4">
                      <dt className="text-muted-foreground text-xs">
                        {t('supplements.submitted')}
                      </dt>
                      <dd className="mt-1 truncate text-xs font-medium">
                        {formatDateTime(order.submittedDate, language)}
                      </dd>
                    </div>
                  </dl>
                </div>

                <div className="border-border mt-auto flex justify-end gap-2 border-t p-3 sm:px-5">
                  {canReject ? (
                    <Button
                      variant="outline"
                      onClick={() => {
                        setSelectedOrder(order)
                        setDecision('reject')
                      }}
                      disabled={!order.rowVersion}
                    >
                      <X aria-hidden="true" />
                      {t('supplements.reject')}
                    </Button>
                  ) : null}
                  {canApprove ? (
                    <Button
                      onClick={() => {
                        setSelectedOrder(order)
                        setDecision('approve')
                      }}
                      disabled={!order.rowVersion}
                    >
                      <Check aria-hidden="true" />
                      {t('supplements.approve')}
                    </Button>
                  ) : null}
                </div>
              </article>
            ))}
          </div>
        ) : null}
      </div>

      {total > pageSize ? (
        <nav
          className="mt-5 flex items-center justify-between gap-4"
          aria-label={t('supplements.pagination')}
        >
          <p className="text-muted-foreground text-xs">
            {t('supplements.pageSummary', { page: page + 1, pageCount, total })}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((value) => Math.max(0, value - 1))}
              disabled={page === 0 || pendingQuery.isFetching}
            >
              <ChevronLeft aria-hidden="true" />
              {t('actions.previous')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((value) => value + 1)}
              disabled={page + 1 >= pageCount || pendingQuery.isFetching}
            >
              {t('actions.next')}
              <ChevronRight aria-hidden="true" />
            </Button>
          </div>
        </nav>
      ) : null}

      <Dialog
        open={Boolean(selectedOrder && decision)}
        onOpenChange={(open: boolean) => !open && closeDialog()}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {t(
                decision === 'approve'
                  ? 'supplements.approveTitle'
                  : 'supplements.rejectTitle',
              )}
            </DialogTitle>
            <DialogDescription>
              {t(
                decision === 'approve'
                  ? 'supplements.approveDescription'
                  : 'supplements.rejectDescription',
                { code: selectedOrder?.vppCode || '—' },
              )}
            </DialogDescription>
          </DialogHeader>
          {decision === 'reject' ? (
            <label>
              <span className="mb-1.5 block text-xs font-medium">
                {t('supplements.rejectReason')}
              </span>
              <Textarea
                value={rejectReason}
                onChange={(event) => setRejectReason(event.target.value)}
                minLength={5}
                maxLength={500}
                placeholder={t('supplements.rejectReasonPlaceholder')}
                aria-invalid={
                  rejectReason.length > 0 && rejectReason.trim().length < 5
                }
              />
              <span className="text-destructive mt-1 block min-h-4 text-xs">
                {rejectReason.length > 0 && rejectReason.trim().length < 5
                  ? t('supplements.reasonTooShort')
                  : ''}
              </span>
            </label>
          ) : null}
          {!selectedOrder?.rowVersion ? (
            <p className="text-destructive text-sm">
              {t('supplements.missingVersion')}
            </p>
          ) : null}
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>
              {t('actions.cancel')}
            </Button>
            <Button
              variant={decision === 'reject' ? 'destructive' : 'default'}
              onClick={() => decisionMutation.mutate()}
              disabled={
                decisionMutation.isPending ||
                !selectedOrder?.rowVersion ||
                (decision === 'reject' && rejectReason.trim().length < 5)
              }
            >
              {decisionMutation.isPending ? (
                <Spinner aria-hidden="true" />
              ) : null}
              {t(
                decision === 'approve'
                  ? 'supplements.confirmApprove'
                  : 'supplements.confirmReject',
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
