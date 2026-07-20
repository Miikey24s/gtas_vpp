import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  ArrowRight,
  BadgeCheck,
  CircleAlert,
  Clock3,
  FileSearch,
  Landmark,
  PackageCheck,
  ReceiptText,
  ShieldAlert,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiPeriodSettlementByYByM,
  getApiPeriodSettlementCurrentByYByM,
} from '@/api/generated'
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
  parsePeriodParams,
} from '@/features/periods/period-format'

export function PeriodOverviewPage() {
  const { t, i18n } = useTranslation()
  const params = useParams()
  const period = parsePeriodParams(params.year, params.month)
  const language = i18n.resolvedLanguage ?? 'vi'

  const statusQuery = useQuery({
    queryKey: [
      'vpp',
      'period-settlement',
      'status',
      period?.year,
      period?.month,
    ],
    enabled: Boolean(period),
    queryFn: async () => {
      const result = await getApiPeriodSettlementByYByM({
        path: { y: period!.year, m: period!.month },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })
  const revisionQuery = useQuery({
    queryKey: [
      'vpp',
      'period-settlement',
      'current',
      period?.year,
      period?.month,
    ],
    enabled: Boolean(period),
    retry: false,
    queryFn: async () => {
      const result = await getApiPeriodSettlementCurrentByYByM({
        path: { y: period!.year, m: period!.month },
      })
      if (result.response?.status === 204) return null
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  if (!period) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('periods.invalidTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('periods.invalidDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  if (statusQuery.isLoading || revisionQuery.isLoading) {
    return (
      <div className="mx-auto w-full max-w-[92rem] px-4 py-8 sm:px-6 lg:px-8">
        <Skeleton className="h-8 w-44" />
        <Skeleton className="mt-6 h-80" />
      </div>
    )
  }

  if (statusQuery.isError || !statusQuery.data) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('periods.errorTitle')}</EmptyTitle>
            <EmptyDescription>{t('periods.errorDescription')}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const status = statusQuery.data
  const revision = revisionQuery.data
  const canPreview = (status.pendingAdditionalCount ?? 0) === 0

  return (
    <div className="mx-auto w-full max-w-[92rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <Button asChild variant="ghost" size="sm" className="-ml-3">
        <Link to="/app/periods" viewTransition>
          <ArrowLeft aria-hidden="true" />
          {t('periods.back')}
        </Link>
      </Button>

      <section className="border-border mt-4 overflow-hidden border">
        <div className="bg-muted/25 flex flex-col gap-5 p-5 sm:p-7 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
              {t('periods.periodLabel')}
            </p>
            <h1 className="mt-1 text-3xl font-semibold tracking-[-0.045em] sm:text-4xl">
              {formatPeriod(period.month, period.year)}
            </h1>
            <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-6">
              {status.isSettled
                ? t('periods.overviewSettledDescription')
                : t('periods.overviewOpenDescription')}
            </p>
          </div>
          <Badge
            variant="outline"
            className={
              status.isSettled
                ? 'rounded-[4px] border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300'
                : status.pendingAdditionalCount
                  ? 'rounded-[4px] border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-300'
                  : 'rounded-[4px]'
            }
          >
            {status.isSettled
              ? t('periods.settled')
              : status.pendingAdditionalCount
                ? t('periods.blocked')
                : t('periods.ready')}
          </Badge>
        </div>

        <div className="grid grid-cols-2 border-t lg:grid-cols-4">
          {[
            [PackageCheck, t('periods.orders'), status.orderCount ?? 0],
            [
              ShieldAlert,
              t('periods.pendingSupplements'),
              status.pendingAdditionalCount ?? 0,
            ],
            [
              Landmark,
              t('periods.total'),
              status.isSettled ? formatMoney(status.grandTotal, language) : '—',
            ],
            [
              ReceiptText,
              t('periods.revision'),
              status.revisionNumber ? `#${status.revisionNumber}` : '—',
            ],
          ].map(([Icon, label, value], index) => {
            const MetricIcon = Icon as typeof PackageCheck
            return (
              <div
                key={String(label)}
                className={`p-4 sm:p-5 ${index % 2 === 1 ? 'border-l' : ''} ${index >= 2 ? 'border-t lg:border-t-0' : ''} ${index > 0 ? 'lg:border-l' : ''}`}
              >
                <MetricIcon
                  className="text-muted-foreground size-4"
                  aria-hidden="true"
                />
                <p className="mt-3 text-lg font-semibold tabular-nums">
                  {String(value)}
                </p>
                <p className="text-muted-foreground mt-1 text-xs">
                  {String(label)}
                </p>
              </div>
            )
          })}
        </div>
      </section>

      {status.pendingAdditionalCount ? (
        <div className="mt-5 flex gap-3 border border-amber-200 bg-amber-50 p-4 text-amber-900 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-100">
          <ShieldAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <div>
            <p className="text-sm font-medium">
              {t('periods.pendingBlockTitle')}
            </p>
            <p className="mt-1 text-xs leading-5">
              {t('periods.pendingBlockDescription', {
                count: status.pendingAdditionalCount,
              })}
            </p>
          </div>
        </div>
      ) : null}

      <section className="mt-6 grid gap-4 lg:grid-cols-[1.2fr_0.8fr]">
        <article className="border-border border p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <FileSearch
              className="text-primary mt-0.5 size-5"
              aria-hidden="true"
            />
            <div>
              <h2 className="font-semibold">{t('periods.previewTitle')}</h2>
              <p className="text-muted-foreground mt-1 text-sm leading-6">
                {t('periods.previewDescription')}
              </p>
            </div>
          </div>
          <div className="mt-6 flex justify-end">
            <Button asChild={canPreview} disabled={!canPreview}>
              {canPreview ? (
                <Link
                  to={`/app/periods/${period.year}/${period.month}/preview${status.isSettled ? '?mode=correction' : ''}`}
                  viewTransition
                >
                  {status.isSettled
                    ? t('periods.previewCorrection')
                    : t('periods.startPreview')}
                  <ArrowRight aria-hidden="true" />
                </Link>
              ) : (
                <span>{t('periods.startPreview')}</span>
              )}
            </Button>
          </div>
        </article>

        <article className="border-border border p-5 sm:p-6">
          <div className="flex items-start gap-3">
            {revision ? (
              <BadgeCheck
                className="mt-0.5 size-5 text-emerald-600"
                aria-hidden="true"
              />
            ) : (
              <Clock3
                className="text-muted-foreground mt-0.5 size-5"
                aria-hidden="true"
              />
            )}
            <div className="min-w-0">
              <h2 className="font-semibold">{t('periods.settlementTitle')}</h2>
              <p className="text-muted-foreground mt-1 text-sm leading-6">
                {revision
                  ? t('periods.settlementCurrent', {
                      supplier: revision.primarySupplierName || '—',
                      date: formatDateTime(revision.confirmedAtUtc, language),
                    })
                  : t('periods.settlementEmpty')}
              </p>
            </div>
          </div>
          <div className="mt-6 flex justify-end">
            <Button asChild variant="outline" disabled={!revision}>
              {revision ? (
                <Link
                  to={`/app/periods/${period.year}/${period.month}/settlement`}
                  viewTransition
                >
                  {t('periods.viewSettlement')}
                  <ArrowRight aria-hidden="true" />
                </Link>
              ) : (
                <span>{t('periods.viewSettlement')}</span>
              )}
            </Button>
          </div>
        </article>
      </section>
    </div>
  )
}
