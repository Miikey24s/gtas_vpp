import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  CircleAlert,
  Clock3,
  ExternalLink,
  FileClock,
  ShieldX,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router-dom'

import { isApiRequestError, toApiRequestError } from '@/api/api-error'
import { getApiVppRequestOrdersByIdHistory } from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import {
  formatDateTime,
  getOrderStatusLabel,
  getStatusClass,
} from '@/features/orders/order-format'
import { cn } from '@/lib/utils'

function getActionLabel(
  action: string | null | undefined,
  t: (key: string) => string,
) {
  const key = action?.trim().toUpperCase()
  const known: Record<string, string> = {
    CREATE: 'orderHistory.actions.create',
    UPDATE: 'orderHistory.actions.update',
    REPLACE: 'orderHistory.actions.replace',
    CANCEL: 'orderHistory.actions.cancel',
    APPROVE: 'orderHistory.actions.approve',
    REJECT: 'orderHistory.actions.reject',
  }
  return t((key && known[key]) || 'orderHistory.actions.other')
}

export function OrderHistoryPage() {
  const { t, i18n } = useTranslation()
  const { orderId } = useParams()
  const [searchParams] = useSearchParams()
  const { hasPermission } = useAuth()
  const canView = [
    permissions.requestViewOwn,
    permissions.requestViewDepartment,
    permissions.requestViewAll,
  ].some(hasPermission)
  const language = i18n.resolvedLanguage ?? 'vi'
  const source = searchParams.get('from')
  const sourceQuery = source ? `?from=${encodeURIComponent(source)}` : ''

  const historyQuery = useQuery({
    queryKey: ['vpp', 'orders', orderId, 'history'],
    enabled: canView && Boolean(orderId),
    queryFn: async () => {
      const result = await getApiVppRequestOrdersByIdHistory({
        path: { id: orderId! },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
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
            <EmptyTitle>{t('orderHistory.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderHistory.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  if (historyQuery.isLoading) {
    return (
      <div className="mx-auto max-w-6xl space-y-5 px-4 py-6 sm:px-6 lg:px-8">
        <Skeleton className="h-28" />
        <Skeleton className="h-96" />
      </div>
    )
  }

  if (historyQuery.isError || !historyQuery.data) {
    const notFound =
      isApiRequestError(historyQuery.error) && historyQuery.error.status === 404
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              {notFound ? (
                <FileClock aria-hidden="true" />
              ) : (
                <CircleAlert aria-hidden="true" />
              )}
            </EmptyMedia>
            <EmptyTitle>
              {t(
                notFound
                  ? 'orderHistory.notFoundTitle'
                  : 'orderHistory.errorTitle',
              )}
            </EmptyTitle>
            <EmptyDescription>
              {t(
                notFound
                  ? 'orderHistory.notFoundDescription'
                  : 'orderHistory.errorDescription',
              )}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const history = historyQuery.data
  const revisions = [...(history.revisions ?? [])].sort(
    (left, right) => (right.revisionNumber ?? 0) - (left.revisionNumber ?? 0),
  )
  const timeline = [...(history.timeline ?? [])].sort(
    (left, right) =>
      Date.parse(right.occurredAt ?? '') - Date.parse(left.occurredAt ?? ''),
  )

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <Link
        to={`/app/orders/${orderId}${sourceQuery}`}
        viewTransition
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-2 text-sm transition-colors"
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('orderHistory.back')}
      </Link>
      <header className="mt-4 flex flex-col gap-4 border-b pb-5 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {t('orderHistory.eyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.035em] sm:text-3xl">
            {t('orderHistory.title')}
          </h1>
          <p className="text-muted-foreground mt-2 text-sm">
            {t('orderHistory.description', { count: revisions.length })}
          </p>
        </div>
        {history.currentRequestId ? (
          <Button asChild variant="outline">
            <Link
              to={`/app/orders/${history.currentRequestId}${sourceQuery}`}
              viewTransition
            >
              <ExternalLink aria-hidden="true" />
              {t('orderHistory.currentVersion')}
            </Link>
          </Button>
        ) : null}
      </header>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)] lg:items-start">
        <section aria-labelledby="timeline-title">
          <h2 id="timeline-title" className="text-base font-semibold">
            {t('orderHistory.timeline')}
          </h2>
          <div className="border-border mt-3 border">
            {timeline.length > 0 ? (
              <ol className="divide-border divide-y">
                {timeline.map((event, index) => (
                  <li key={event.id ?? index} className="flex gap-3 p-4">
                    <span className="bg-primary/10 text-primary mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full">
                      <Clock3 className="size-4" aria-hidden="true" />
                    </span>
                    <div className="min-w-0">
                      <p className="text-sm font-medium">
                        {getActionLabel(event.action, t)}
                      </p>
                      <p className="text-muted-foreground mt-1 text-xs">
                        {formatDateTime(event.occurredAt, language)}
                        {event.actorName ? ` · ${event.actorName}` : ''}
                      </p>
                      {event.reason ? (
                        <p className="text-muted-foreground mt-2 text-sm leading-6">
                          {event.reason}
                        </p>
                      ) : null}
                    </div>
                  </li>
                ))}
              </ol>
            ) : (
              <p className="text-muted-foreground p-5 text-sm">
                {t('orderHistory.emptyTimeline')}
              </p>
            )}
          </div>
        </section>

        <section aria-labelledby="revisions-title">
          <h2 id="revisions-title" className="text-base font-semibold">
            {t('orderHistory.revisions')}
          </h2>
          <div className="mt-3 space-y-3">
            {revisions.map((revision, index) => (
              <article
                key={revision.id ?? index}
                className="border-border border p-4 sm:p-5"
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="font-semibold">
                        {t('orderHistory.revisionLabel', {
                          number: revision.revisionNumber ?? index + 1,
                        })}
                      </h3>
                      {revision.isCurrentRevision ? (
                        <Badge className="rounded-[4px]">
                          {t('orderHistory.current')}
                        </Badge>
                      ) : null}
                      <Badge
                        variant="outline"
                        className={cn(
                          'rounded-[4px]',
                          getStatusClass(revision.status),
                        )}
                      >
                        {getOrderStatusLabel(
                          revision.status,
                          revision.isDeadlinePassed,
                          revision.isAdditionalOrder,
                          t,
                        )}
                      </Badge>
                    </div>
                    <p className="text-muted-foreground mt-1 text-xs">
                      {revision.vppCode || '—'} ·{' '}
                      {formatDateTime(
                        revision.updatedAtUtc || revision.submittedDate,
                        language,
                      )}
                    </p>
                  </div>
                  {revision.id ? (
                    <Button asChild variant="ghost" size="sm">
                      <Link
                        to={`/app/orders/${revision.id}${sourceQuery}`}
                        viewTransition
                      >
                        {t('orderHistory.openRevision')}
                        <ExternalLink aria-hidden="true" />
                      </Link>
                    </Button>
                  ) : null}
                </div>
                <dl className="border-border mt-4 grid grid-cols-3 border-t pt-4 text-sm">
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('orderHistory.items')}
                    </dt>
                    <dd className="mt-1 font-medium tabular-nums">
                      {revision.totalLines ?? revision.items?.length ?? 0}
                    </dd>
                  </div>
                  <div className="border-border border-l pl-4">
                    <dt className="text-muted-foreground text-xs">
                      {t('orderHistory.quantity')}
                    </dt>
                    <dd className="mt-1 font-medium tabular-nums">
                      {revision.totalQty ?? 0}
                    </dd>
                  </div>
                  <div className="border-border border-l pl-4">
                    <dt className="text-muted-foreground text-xs">
                      {t('orderHistory.actor')}
                    </dt>
                    <dd className="mt-1 truncate font-medium">
                      {revision.requesterName || '—'}
                    </dd>
                  </div>
                </dl>
              </article>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}
