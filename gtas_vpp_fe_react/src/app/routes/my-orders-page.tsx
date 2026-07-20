import { useQuery } from '@tanstack/react-query'
import {
  Boxes,
  CalendarDays,
  CircleAlert,
  Clock3,
  Layers3,
  PackageOpen,
  Plus,
  RefreshCw,
  ShieldX,
} from 'lucide-react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestMyOrders,
  getApiVppRequestPeriodInfo,
  type VppRequestResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { PrintControl } from '@/components/app/header-controls'
import { OrderCard } from '@/features/orders/order-card'
import { formatDate, formatPeriod } from '@/features/orders/order-format'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
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

function sumLineItems(orders: VppRequestResDto[]) {
  return orders.reduce(
    (total, order) => total + (order.totalLines ?? order.items?.length ?? 0),
    0,
  )
}

function sumQuantity(orders: VppRequestResDto[]) {
  return orders.reduce(
    (total, order) =>
      total +
      (order.totalQty ??
        order.items?.reduce(
          (itemTotal, item) => itemTotal + (item.qty ?? 0),
          0,
        ) ??
        0),
    0,
  )
}

function getTopItem(orders: VppRequestResDto[]) {
  return orders
    .flatMap((order) => order.items ?? [])
    .sort((left, right) => (right.qty ?? 0) - (left.qty ?? 0))[0]
}

function OrdersSkeleton() {
  return (
    <div className="space-y-5" aria-label="Loading">
      <Skeleton className="h-56 w-full" />
      <Skeleton className="h-12 w-64" />
      <Skeleton className="h-44 w-full" />
      <Skeleton className="h-44 w-full" />
    </div>
  )
}

export function MyOrdersPage() {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const canView = hasPermission(permissions.requestViewOwn)
  const canCreate = hasPermission(permissions.requestCreate)
  const language = i18n.resolvedLanguage ?? 'vi'

  const periodQuery = useQuery({
    queryKey: ['vpp', 'period-info'],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiVppRequestPeriodInfo()
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const period = periodQuery.data
  const ordersQuery = useQuery({
    queryKey: [
      'vpp',
      'my-orders',
      period?.currentPeriodYear,
      period?.currentPeriodMonth,
      period?.previousPeriodYear,
      period?.previousPeriodMonth,
    ],
    enabled: canView && Boolean(period),
    queryFn: async () => {
      const result = await getApiVppRequestMyOrders({
        query: {
          years: [period!.currentPeriodYear!, period!.previousPeriodYear!],
          months: [period!.currentPeriodMonth!, period!.previousPeriodMonth!],
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const derived = useMemo(() => {
    const orders = ordersQuery.data ?? []
    const currentOrders = orders.filter(
      (order) =>
        order.year === period?.currentPeriodYear &&
        order.month === period?.currentPeriodMonth,
    )
    const previousOrders = orders.filter(
      (order) =>
        !order.isAdditionalOrder &&
        order.year === period?.previousPeriodYear &&
        order.month === period?.previousPeriodMonth,
    )
    const currentRegular = currentOrders.filter(
      (order) => !order.isAdditionalOrder,
    )
    const currentAdditional = currentOrders.filter(
      (order) => order.isAdditionalOrder,
    )
    const latestSubmission = currentOrders
      .map((order) => order.submittedDate)
      .filter((value): value is string => Boolean(value))
      .sort((left, right) => Date.parse(right) - Date.parse(left))[0]

    return {
      currentOrders,
      currentRegular,
      currentAdditional,
      previousOrders,
      totalLines: sumLineItems(currentOrders),
      totalQuantity: sumQuantity(currentOrders),
      topItem: getTopItem(currentOrders),
      latestSubmission,
    }
  }, [ordersQuery.data, period])

  if (!canView) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orders.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orders.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const isLoading = periodQuery.isLoading || ordersQuery.isLoading
  const error = periodQuery.error ?? ordersQuery.error
  const refresh = () => {
    void Promise.all([periodQuery.refetch(), ordersQuery.refetch()])
  }

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <div className="mb-6 flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold tracking-[0.14em] text-[#0369a1] uppercase dark:text-sky-300">
            {t('orders.workspace')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.03em] sm:text-3xl">
            {t('orders.title')}
          </h1>
        </div>
        <div className="flex items-center gap-2">
          {canCreate && period?.canCreateOrder ? (
            <Button asChild size="sm" className="no-print">
              <Link to="/app/orders/new" viewTransition>
                <Plus aria-hidden="true" />
                {t('orders.createOrder')}
              </Link>
            </Button>
          ) : null}
          {canCreate &&
          !period?.canCreateOrder &&
          period?.canCreateAdditional ? (
            <Button asChild size="sm" className="no-print">
              <Link to="/app/orders/new?type=additional" viewTransition>
                <Plus aria-hidden="true" />
                {t('orders.createAdditional', {
                  count: period.remainingSupplementAttempts ?? 0,
                })}
              </Link>
            </Button>
          ) : null}
          <PrintControl />
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="no-print"
            onClick={refresh}
            disabled={isLoading}
          >
            <RefreshCw
              className={isLoading ? 'animate-spin' : ''}
              aria-hidden="true"
            />
            <span className="hidden sm:inline">{t('actions.refresh')}</span>
          </Button>
        </div>
      </div>

      {isLoading ? <OrdersSkeleton /> : null}

      {!isLoading && error ? (
        <Alert variant="destructive" className="px-4 py-4">
          <CircleAlert aria-hidden="true" />
          <AlertTitle>{t('orders.loadErrorTitle')}</AlertTitle>
          <AlertDescription>
            {t('orders.loadErrorDescription')}
          </AlertDescription>
        </Alert>
      ) : null}

      {!isLoading && !error && period ? (
        <div className="space-y-7">
          <section
            className="bg-card border-border border"
            aria-labelledby="orders-story-title"
          >
            <div className="p-5 sm:p-6">
              <div>
                <div className="flex flex-wrap items-center gap-3">
                  <span className="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
                    <CalendarDays
                      className="text-primary size-4"
                      aria-hidden="true"
                    />
                    {t('orders.currentPeriod')}
                  </span>
                  <strong className="text-primary text-2xl tracking-[-0.04em]">
                    {formatPeriod(
                      period.currentPeriodMonth,
                      period.currentPeriodYear,
                    )}
                  </strong>
                </div>
                <h2
                  id="orders-story-title"
                  className="mt-5 text-2xl font-semibold tracking-[-0.035em] sm:text-3xl"
                >
                  {t(
                    derived.currentOrders.length > 0
                      ? 'orders.storySubmitted'
                      : 'orders.storyEmpty',
                  )}
                </h2>
                <div className="text-muted-foreground mt-4 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm">
                  <span className="flex items-center gap-2">
                    <Clock3 className="size-4" aria-hidden="true" />
                    {t('orders.deadline', {
                      value: formatDate(period.deadlineDate, language),
                    })}
                  </span>
                  <Badge
                    variant="outline"
                    className={
                      period.isSubmissionOpen && !period.isDeadlinePassed
                        ? 'rounded-[4px] border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300'
                        : 'rounded-[4px]'
                    }
                  >
                    {t(
                      period.isSubmissionOpen && !period.isDeadlinePassed
                        ? 'orders.periodOpen'
                        : 'orders.periodClosed',
                    )}
                  </Badge>
                </div>
              </div>
            </div>

            <div className="border-border grid grid-cols-2 border-t lg:grid-cols-4">
              {[
                {
                  icon: Layers3,
                  value: derived.totalLines,
                  label: t('orders.totalItems'),
                },
                {
                  icon: Boxes,
                  value: derived.totalQuantity,
                  label: t('orders.totalQuantity'),
                },
                {
                  icon: Clock3,
                  value: derived.latestSubmission
                    ? new Intl.DateTimeFormat(
                        language === 'en' ? 'en-GB' : 'vi-VN',
                        {
                          hour: '2-digit',
                          minute: '2-digit',
                        },
                      ).format(new Date(derived.latestSubmission))
                    : '—',
                  label: t('orders.latestSubmission'),
                },
                {
                  icon: PackageOpen,
                  value: derived.topItem?.qty ?? '—',
                  label: derived.topItem?.vppName || t('orders.topItem'),
                },
              ].map((metric, index) => (
                <article
                  key={metric.label}
                  className={`min-w-0 p-4 sm:p-5 ${index % 2 !== 0 ? 'border-l' : ''} ${index >= 2 ? 'border-t lg:border-t-0' : ''} ${index > 0 ? 'lg:border-l' : ''}`}
                >
                  <metric.icon
                    className="text-muted-foreground mb-4 size-4"
                    aria-hidden="true"
                  />
                  <p className="text-xl font-semibold tabular-nums">
                    {metric.value}
                  </p>
                  <p
                    className="text-muted-foreground mt-1 truncate text-xs"
                    title={metric.label}
                  >
                    {metric.label}
                  </p>
                </article>
              ))}
            </div>
          </section>

          <section aria-labelledby="current-orders-title">
            <div className="mb-3 flex items-center gap-2">
              <h2 id="current-orders-title" className="text-base font-semibold">
                {t('orders.currentDetails')}
              </h2>
              <Badge variant="outline" className="rounded-[4px]">
                {derived.currentRegular.length}
              </Badge>
            </div>
            {derived.currentRegular.length > 0 ? (
              <div className="space-y-3">
                {derived.currentRegular.map((order, index) => (
                  <OrderCard
                    key={order.id ?? index}
                    order={order}
                    defaultOpen={index === 0}
                  />
                ))}
              </div>
            ) : (
              <Empty className="min-h-48 border">
                <EmptyHeader>
                  <EmptyMedia variant="icon">
                    <PackageOpen aria-hidden="true" />
                  </EmptyMedia>
                  <EmptyTitle>{t('orders.emptyCurrentTitle')}</EmptyTitle>
                  <EmptyDescription>
                    {t('orders.emptyCurrentDescription')}
                  </EmptyDescription>
                </EmptyHeader>
              </Empty>
            )}
          </section>

          {derived.currentAdditional.length > 0 ? (
            <section aria-labelledby="additional-orders-title">
              <div className="mb-3 flex items-center gap-2">
                <h2
                  id="additional-orders-title"
                  className="text-base font-semibold"
                >
                  {t('orders.additionalOrders')}
                </h2>
                <Badge variant="outline" className="rounded-[4px]">
                  {derived.currentAdditional.length}
                </Badge>
              </div>
              <div className="space-y-3">
                {derived.currentAdditional.map((order, index) => (
                  <OrderCard key={order.id ?? index} order={order} />
                ))}
              </div>
            </section>
          ) : null}

          <details className="bg-card border-border group border">
            <summary className="hover:bg-muted/40 focus-visible:ring-ring flex cursor-pointer list-none items-center justify-between px-4 py-4 focus-visible:ring-2 focus-visible:outline-none sm:px-5 [&::-webkit-details-marker]:hidden">
              <span>
                <strong className="text-sm">
                  {t('orders.previousPeriod')} ·{' '}
                  {formatPeriod(
                    period.previousPeriodMonth,
                    period.previousPeriodYear,
                  )}
                </strong>
                <span className="text-muted-foreground ml-2 text-xs">
                  {t('orders.orderCount', {
                    count: derived.previousOrders.length,
                  })}
                </span>
              </span>
              <span className="text-muted-foreground text-xs">
                {t('orders.expand')}
              </span>
            </summary>
            <div className="border-border space-y-3 border-t p-3 sm:p-4">
              {derived.previousOrders.length > 0 ? (
                derived.previousOrders.map((order, index) => (
                  <OrderCard key={order.id ?? index} order={order} />
                ))
              ) : (
                <p className="text-muted-foreground px-2 py-4 text-sm">
                  {t('orders.emptyPrevious')}
                </p>
              )}
            </div>
          </details>
        </div>
      ) : null}
    </div>
  )
}
