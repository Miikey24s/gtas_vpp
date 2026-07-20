import { useQuery } from '@tanstack/react-query'
import {
  ArrowRight,
  CalendarRange,
  CircleAlert,
  Clock3,
  Landmark,
  ReceiptText,
  ShieldX,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiPeriodSettlement,
  getApiPeriodSettlementByYByM,
  getApiVppRequestPeriodInfo,
  type PeriodSettlementResDto,
} from '@/api/generated'
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
import { formatDateTime, formatPeriod } from '@/features/orders/order-format'
import {
  formatMoney,
  previousCalendarPeriod,
} from '@/features/periods/period-format'

function PeriodCard({
  period,
  language,
  featured = false,
}: {
  period: PeriodSettlementResDto
  language: string
  featured?: boolean
}) {
  const { t } = useTranslation()
  const year = period.year ?? 0
  const month = period.month ?? 0
  return (
    <article
      className={
        featured
          ? 'border-primary/30 bg-primary/[0.035] border p-5 sm:p-6'
          : 'border-border border p-5'
      }
    >
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-muted-foreground text-xs font-medium tracking-[0.12em] uppercase">
            {featured ? t('periods.targetPeriod') : t('periods.completedPeriod')}
          </p>
          <h2 className="mt-1 text-2xl font-semibold tracking-[-0.035em]">
            {formatPeriod(month, year)}
          </h2>
        </div>
        <Badge
          variant="outline"
          className={
            period.isSettled
              ? 'rounded-[4px] border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300'
              : period.pendingAdditionalCount
                ? 'rounded-[4px] border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-300'
                : 'rounded-[4px]'
          }
        >
          {period.isSettled
            ? t('periods.settled')
            : period.pendingAdditionalCount
              ? t('periods.blocked')
              : t('periods.ready')}
        </Badge>
      </div>
      <dl className="mt-6 grid grid-cols-2 gap-x-4 gap-y-5 text-sm sm:grid-cols-4">
        <div>
          <dt className="text-muted-foreground text-xs">{t('periods.orders')}</dt>
          <dd className="mt-1 font-semibold tabular-nums">{period.orderCount ?? 0}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground text-xs">
            {t('periods.pendingSupplements')}
          </dt>
          <dd className="mt-1 font-semibold tabular-nums">
            {period.pendingAdditionalCount ?? 0}
          </dd>
        </div>
        <div>
          <dt className="text-muted-foreground text-xs">{t('periods.total')}</dt>
          <dd className="mt-1 font-semibold tabular-nums">
            {period.isSettled
              ? formatMoney(period.grandTotal, language)
              : '—'}
          </dd>
        </div>
        <div>
          <dt className="text-muted-foreground text-xs">{t('periods.revision')}</dt>
          <dd className="mt-1 font-semibold tabular-nums">
            {period.revisionNumber ? `#${period.revisionNumber}` : '—'}
          </dd>
        </div>
      </dl>
      {period.isSettled ? (
        <p className="text-muted-foreground mt-5 text-xs">
          {period.primarySupplierName || period.priceListName || '—'} ·{' '}
          {formatDateTime(period.settledAt, language)}
        </p>
      ) : null}
      <div className="border-border mt-5 flex justify-end border-t pt-4">
        <Button asChild variant={featured ? 'default' : 'outline'} size="sm">
          <Link to={`/app/periods/${year}/${month}`} viewTransition>
            {t('periods.openPeriod')}
            <ArrowRight aria-hidden="true" />
          </Link>
        </Button>
      </div>
    </article>
  )
}

export function PeriodsPage() {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const language = i18n.resolvedLanguage ?? 'vi'
  const canSettle = hasPermission(permissions.periodSettle)
  const canReadOwnPeriod = hasPermission(permissions.requestViewOwn)

  const periodInfoQuery = useQuery({
    queryKey: ['vpp', 'period-info', 'settlement-target'],
    enabled: canSettle && canReadOwnPeriod,
    retry: false,
    queryFn: async () => {
      const result = await getApiVppRequestPeriodInfo()
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })
  const fallback = previousCalendarPeriod()
  const target = {
    year: periodInfoQuery.data?.previousPeriodYear ?? fallback.year,
    month: periodInfoQuery.data?.previousPeriodMonth ?? fallback.month,
  }

  const targetQuery = useQuery({
    queryKey: ['vpp', 'period-settlement', 'status', target.year, target.month],
    enabled: canSettle,
    queryFn: async () => {
      const result = await getApiPeriodSettlementByYByM({
        path: { y: target.year, m: target.month },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })
  const settledQuery = useQuery({
    queryKey: ['vpp', 'period-settlement', 'list'],
    enabled: canSettle,
    queryFn: async () => {
      const result = await getApiPeriodSettlement()
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  if (!canSettle) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('periods.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>{t('periods.forbiddenDescription')}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const settled = (settledQuery.data ?? []).filter(
    (item) => item.year !== target.year || item.month !== target.month,
  )
  const settledTotal = settled.reduce(
    (sum, item) => sum + (item.grandTotal ?? 0),
    0,
  )

  return (
    <div className="mx-auto w-full max-w-[92rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <header>
        <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
          {t('periods.eyebrow')}
        </p>
        <h1 className="mt-1 text-2xl font-semibold tracking-[-0.04em] sm:text-3xl">
          {t('periods.title')}
        </h1>
        <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
          {t('periods.description')}
        </p>
      </header>

      <section className="border-border mt-6 grid grid-cols-2 border lg:grid-cols-4">
        {[
          [CalendarRange, t('periods.historyCount'), settled.length],
          [ReceiptText, t('periods.currentOrders'), targetQuery.data?.orderCount ?? 0],
          [Landmark, t('periods.settledValue'), formatMoney(settledTotal, language)],
          [Clock3, t('periods.lastSettlement'), settled[0]?.settledAt ? formatDateTime(settled[0].settledAt, language) : '—'],
        ].map(([Icon, label, value], index) => {
          const MetricIcon = Icon as typeof CalendarRange
          return (
            <div
              key={String(label)}
              className={`min-w-0 p-4 sm:p-5 ${index % 2 === 1 ? 'border-l' : ''} ${index >= 2 ? 'border-t lg:border-t-0' : ''} ${index > 0 ? 'lg:border-l' : ''}`}
            >
              <MetricIcon className="text-muted-foreground size-4" aria-hidden="true" />
              <p className="mt-3 truncate text-lg font-semibold tabular-nums">{String(value)}</p>
              <p className="text-muted-foreground mt-1 truncate text-xs">{String(label)}</p>
            </div>
          )
        })}
      </section>

      <div className="mt-6">
        {targetQuery.isLoading ? <Skeleton className="h-72" /> : null}
        {targetQuery.isError ? (
          <Empty className="min-h-60 border">
            <EmptyHeader>
              <EmptyMedia variant="icon"><CircleAlert aria-hidden="true" /></EmptyMedia>
              <EmptyTitle>{t('periods.errorTitle')}</EmptyTitle>
              <EmptyDescription>{t('periods.errorDescription')}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}
        {targetQuery.data ? (
          <PeriodCard period={targetQuery.data} language={language} featured />
        ) : null}
      </div>

      <section className="mt-8">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold">{t('periods.historyTitle')}</h2>
            <p className="text-muted-foreground mt-1 text-sm">{t('periods.historyDescription')}</p>
          </div>
        </div>
        {settledQuery.isLoading ? (
          <div className="mt-4 grid gap-4 lg:grid-cols-2">
            <Skeleton className="h-64" /><Skeleton className="h-64" />
          </div>
        ) : null}
        {!settledQuery.isLoading && settled.length === 0 ? (
          <Empty className="mt-4 min-h-52 border">
            <EmptyHeader>
              <EmptyMedia variant="icon"><ReceiptText aria-hidden="true" /></EmptyMedia>
              <EmptyTitle>{t('periods.historyEmptyTitle')}</EmptyTitle>
              <EmptyDescription>{t('periods.historyEmptyDescription')}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}
        {settled.length > 0 ? (
          <div className="mt-4 grid gap-4 lg:grid-cols-2">
            {settled.map((period) => (
              <PeriodCard
                key={`${period.year}-${period.month}`}
                period={period}
                language={language}
              />
            ))}
          </div>
        ) : null}
      </section>
    </div>
  )
}
