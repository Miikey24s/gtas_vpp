import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  ArrowRight,
  BadgeCheck,
  CircleAlert,
  FileClock,
  Fingerprint,
  Landmark,
  RefreshCw,
  ShieldCheck,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiPeriodSettlementCurrentByYByM,
  getApiPeriodSettlementRevisionsByYByM,
  type SettlementRevisionResDto,
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

function FinancialBreakdown({
  settlement,
  language,
}: {
  settlement: SettlementRevisionResDto
  language: string
}) {
  const { t } = useTranslation()
  const rows = [
    [t('periods.subtotal'), settlement.subtotal ?? 0],
    [t('periods.discount'), -(settlement.discountAmount ?? 0)],
    [t('periods.rebate'), -(settlement.rebateAmount ?? 0)],
    [t('periods.fees'), settlement.feeAmount ?? 0],
    [t('periods.shipping'), settlement.shippingAmount ?? 0],
    [t('periods.vat'), settlement.vatAmount ?? 0],
    [t('periods.rounding'), settlement.roundingAdjustment ?? 0],
  ]
  return (
    <dl className="divide-border divide-y">
      {rows.map(([label, value]) => (
        <div
          key={String(label)}
          className="flex items-center justify-between gap-4 py-3 text-sm"
        >
          <dt className="text-muted-foreground">{String(label)}</dt>
          <dd className="font-medium tabular-nums">
            {formatMoney(
              Number(value),
              language,
              settlement.currencyCode || 'VND',
            )}
          </dd>
        </div>
      ))}
      <div className="flex items-center justify-between gap-4 pt-4">
        <dt className="font-semibold">{t('periods.grandTotal')}</dt>
        <dd className="text-xl font-semibold tabular-nums">
          {formatMoney(
            settlement.grandTotal,
            language,
            settlement.currencyCode || 'VND',
          )}
        </dd>
      </div>
    </dl>
  )
}

export function SettlementDetailPage() {
  const { t, i18n } = useTranslation()
  const params = useParams()
  const period = parsePeriodParams(params.year, params.month)
  const language = i18n.resolvedLanguage ?? 'vi'

  const currentQuery = useQuery({
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
  const revisionsQuery = useQuery({
    queryKey: [
      'vpp',
      'period-settlement',
      'revisions',
      period?.year,
      period?.month,
    ],
    enabled: Boolean(period),
    queryFn: async () => {
      const result = await getApiPeriodSettlementRevisionsByYByM({
        path: { y: period!.year, m: period!.month },
      })
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

  if (currentQuery.isLoading || revisionsQuery.isLoading) {
    return (
      <div className="mx-auto w-full max-w-[92rem] px-4 py-8 sm:px-6 lg:px-8">
        <Skeleton className="h-8 w-48" />
        <div className="mt-6 grid gap-5 lg:grid-cols-2">
          <Skeleton className="h-96" />
          <Skeleton className="h-96" />
        </div>
      </div>
    )
  }

  if (currentQuery.isError || revisionsQuery.isError) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('periods.settlementErrorTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('periods.settlementErrorDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const current = currentQuery.data
  if (!current) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <FileClock aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('periods.noSettlementTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('periods.noSettlementDescription')}
            </EmptyDescription>
          </EmptyHeader>
          <Button asChild>
            <Link
              to={`/app/periods/${period.year}/${period.month}/preview`}
              viewTransition
            >
              {t('periods.startPreview')}
              <ArrowRight aria-hidden="true" />
            </Link>
          </Button>
        </Empty>
      </div>
    )
  }

  const revisions = revisionsQuery.data ?? []
  return (
    <div className="mx-auto w-full max-w-[92rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <Button asChild variant="ghost" size="sm" className="-ml-3">
        <Link to={`/app/periods/${period.year}/${period.month}`} viewTransition>
          <ArrowLeft aria-hidden="true" />
          {t('periods.backToPeriod')}
        </Link>
      </Button>

      <header className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {t('periods.settlementEyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.04em] sm:text-3xl">
            {t('periods.settlementPageTitle', {
              period: formatPeriod(period.month, period.year),
            })}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t('periods.settlementPageDescription')}
          </p>
        </div>
        <Button asChild>
          <Link
            to={`/app/periods/${period.year}/${period.month}/preview?mode=correction`}
            viewTransition
          >
            <RefreshCw aria-hidden="true" />
            {t('periods.createCorrection')}
          </Link>
        </Button>
      </header>

      <section className="mt-6 grid gap-5 lg:grid-cols-[1fr_0.9fr]">
        <article className="border-border border p-5 sm:p-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-start gap-3">
              <Landmark
                className="text-primary mt-0.5 size-5"
                aria-hidden="true"
              />
              <div>
                <h2 className="font-semibold">
                  {current.primarySupplierName || '—'}
                </h2>
                <p className="text-muted-foreground mt-1 text-xs">
                  {current.priceListName || '—'} · v
                  {current.priceListVersion ?? 0}
                </p>
              </div>
            </div>
            <Badge
              variant="outline"
              className="rounded-[4px] border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300"
            >
              <BadgeCheck aria-hidden="true" />
              {t('periods.currentRevision', {
                revision: current.revisionNumber ?? 1,
              })}
            </Badge>
          </div>
          <div className="mt-6 grid grid-cols-2 gap-5 border-y py-5 text-sm sm:grid-cols-4">
            <div>
              <p className="text-muted-foreground text-xs">
                {t('periods.items')}
              </p>
              <p className="mt-1 font-semibold tabular-nums">
                {current.itemCount ?? 0}
              </p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs">
                {t('periods.allocations')}
              </p>
              <p className="mt-1 font-semibold tabular-nums">
                {current.allocationCount ?? 0}
              </p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs">
                {t('periods.confirmedAt')}
              </p>
              <p className="mt-1 font-semibold">
                {formatDateTime(current.confirmedAtUtc, language)}
              </p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs">
                {t('periods.confirmedBy')}
              </p>
              <p className="mt-1 font-semibold tabular-nums">
                #{current.confirmedByUserId ?? '—'}
              </p>
            </div>
          </div>
          <div className="mt-6">
            <FinancialBreakdown settlement={current} language={language} />
          </div>
          <div className="bg-muted/40 mt-6 flex items-start gap-3 p-4">
            <Fingerprint
              className="text-muted-foreground mt-0.5 size-4 shrink-0"
              aria-hidden="true"
            />
            <div className="min-w-0 text-xs">
              <p className="font-medium">
                {t('periods.reconciliationEvidence')}
              </p>
              <p
                className="text-muted-foreground mt-1 truncate font-mono"
                title={current.inputHash || undefined}
              >
                {current.calculationVersion || '—'} · {current.inputHash || '—'}
              </p>
            </div>
          </div>
        </article>

        <article className="border-border border p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <FileClock
              className="text-primary mt-0.5 size-5"
              aria-hidden="true"
            />
            <div>
              <h2 className="font-semibold">
                {t('periods.revisionHistoryTitle')}
              </h2>
              <p className="text-muted-foreground mt-1 text-sm leading-6">
                {t('periods.revisionHistoryDescription')}
              </p>
            </div>
          </div>
          <ol className="mt-6 space-y-0">
            {revisions.map((revision, index) => (
              <li key={revision.id} className="relative pl-8">
                {index < revisions.length - 1 ? (
                  <span
                    className="bg-border absolute top-5 bottom-0 left-[9px] w-px"
                    aria-hidden="true"
                  />
                ) : null}
                <span
                  className={
                    revision.isCurrentRevision
                      ? 'bg-primary ring-primary/10 absolute top-1 left-1 size-3 rounded-full ring-4'
                      : 'bg-muted-foreground/35 absolute top-1 left-1 size-3 rounded-full'
                  }
                  aria-hidden="true"
                />
                <div className="border-border pb-6">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="font-medium">
                        {t('periods.revisionLabel', {
                          revision: revision.revisionNumber ?? 0,
                        })}
                      </p>
                      <p className="text-muted-foreground mt-1 text-xs">
                        {revision.primarySupplierName || '—'} ·{' '}
                        {formatDateTime(revision.confirmedAtUtc, language)}
                      </p>
                    </div>
                    <Badge variant="outline" className="rounded-[4px]">
                      {revision.isCurrentRevision
                        ? t('periods.current')
                        : revision.isCorrection
                          ? t('periods.correction')
                          : t('periods.original')}
                    </Badge>
                  </div>
                  <div className="mt-3 flex items-end justify-between gap-4">
                    <p className="text-muted-foreground line-clamp-2 text-xs">
                      {revision.correctionReason ||
                        t('periods.initialConfirmation')}
                    </p>
                    <p className="shrink-0 font-semibold tabular-nums">
                      {formatMoney(
                        revision.grandTotal,
                        language,
                        revision.currencyCode || 'VND',
                      )}
                    </p>
                  </div>
                </div>
              </li>
            ))}
          </ol>
          <div className="border-border flex items-start gap-3 border-t pt-5 text-xs">
            <ShieldCheck
              className="text-muted-foreground size-4 shrink-0"
              aria-hidden="true"
            />
            <p className="text-muted-foreground leading-5">
              {t('periods.immutableHistoryHint')}
            </p>
          </div>
        </article>
      </section>
    </div>
  )
}
