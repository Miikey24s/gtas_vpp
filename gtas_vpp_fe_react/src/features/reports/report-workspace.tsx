import { useMutation, useQuery } from '@tanstack/react-query'
import {
  AlertTriangle,
  BarChart3,
  Bot,
  CheckCircle2,
  Download,
  FileSpreadsheet,
  Lightbulb,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  TrendingUp,
} from 'lucide-react'
import { m } from 'motion/react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  XAxis,
  YAxis,
} from 'recharts'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiReportsExportXlsx,
  getApiReportsInsights,
  getApiReportsSummary,
  type ReportInsightResDto,
  type ReportSummaryResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { PrintControl } from '@/components/app/header-controls'
import { Badge } from '@/components/ui/badge'
import { Button, buttonVariants } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
} from '@/components/ui/chart'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { surfaceEnter } from '@/lib/motion'
import { cn } from '@/lib/utils'

type ReportScope = 'own' | 'department' | 'all'

type ScopeOption = {
  value: ReportScope
  labelKey: string
}

const trendConfig = {
  totalAmount: {
    label: 'Total amount',
    color: 'var(--chart-1)',
  },
} satisfies ChartConfig

const statusConfig = {
  orderCount: {
    label: 'Orders',
    color: 'var(--chart-2)',
  },
} satisfies ChartConfig

const departmentConfig = {
  totalAmount: {
    label: 'Total amount',
    color: 'var(--chart-3)',
  },
} satisfies ChartConfig

function finiteNumber(value: number | undefined | null) {
  return Number.isFinite(value) ? Number(value) : 0
}

function parseFilterNumber(value: string | null, min: number, max: number) {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed >= min && parsed <= max
    ? parsed
    : undefined
}

function statusTranslationKey(resourceKey?: string | null) {
  switch (resourceKey?.toLowerCase()) {
    case 'submitted':
      return 'reports.status.submitted'
    case 'cancelled':
      return 'reports.status.cancelled'
    case 'pending':
      return 'reports.status.pending'
    case 'approved':
      return 'reports.status.approved'
    case 'rejected':
      return 'reports.status.rejected'
    case 'submittedperiodclosed':
      return 'reports.status.submittedClosed'
    default:
      return 'reports.status.unknown'
  }
}

function downloadName(response: Response | undefined, fallback: string) {
  const disposition = response?.headers.get('content-disposition')
  if (!disposition) return fallback

  const utf8 = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
  if (utf8) return decodeURIComponent(utf8.replace(/["']/g, ''))

  return disposition.match(/filename="?([^";]+)"?/i)?.[1] ?? fallback
}

export function ReportWorkspace({ focusInsights = false }) {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()

  const scopeOptions = useMemo<ScopeOption[]>(() => {
    const options: ScopeOption[] = []
    if (hasPermission(permissions.reportViewOwn))
      options.push({ value: 'own', labelKey: 'reports.scope.own' })
    if (hasPermission(permissions.reportViewDepartment))
      options.push({
        value: 'department',
        labelKey: 'reports.scope.department',
      })
    if (hasPermission(permissions.reportViewAll))
      options.push({ value: 'all', labelKey: 'reports.scope.all' })
    return options
  }, [hasPermission])

  const requestedScope = searchParams.get('scope') as ReportScope | null
  const scope =
    scopeOptions.find((option) => option.value === requestedScope)?.value ??
    scopeOptions.at(-1)?.value ??
    'own'
  const year = parseFilterNumber(searchParams.get('year'), 2000, 2100)
  const month = year
    ? parseFilterNumber(searchParams.get('month'), 1, 12)
    : undefined
  const canView = scopeOptions.length > 0
  const canExport = hasPermission(permissions.reportExport)
  const language = i18n.resolvedLanguage === 'en' ? 'en' : 'vi'

  const query = { scope, year, month }
  const summaryQuery = useQuery({
    queryKey: ['reports', 'summary', scope, year, month],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiReportsSummary({ query })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const insightQuery = useQuery({
    queryKey: ['reports', 'insights', scope, year, month, language],
    enabled: false,
    retry: false,
    queryFn: async () => {
      const result = await getApiReportsInsights({
        query: { ...query, language },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const exportMutation = useMutation({
    mutationFn: async () => {
      const result = await getApiReportsExportXlsx({
        query,
        parseAs: 'blob',
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      const data = result.data as unknown
      return {
        blob: data instanceof Blob ? data : new Blob([data as BlobPart]),
        response: result.response,
      }
    },
    onSuccess: ({ blob, response }) => {
      const objectUrl = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = objectUrl
      anchor.download = downloadName(
        response,
        `GTAS-VPP-${scope}-${year ?? 'all'}${month ? `-${String(month).padStart(2, '0')}` : ''}.xlsx`,
      )
      document.body.append(anchor)
      anchor.click()
      anchor.remove()
      URL.revokeObjectURL(objectUrl)
      toast.success(t('reports.feedback.exported'))
    },
    onError: () => toast.error(t('reports.feedback.exportFailed')),
  })

  function updateFilters(
    changes: Partial<Record<'scope' | 'year' | 'month', string | undefined>>,
  ) {
    const next = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    if ('year' in changes && !changes.year) next.delete('month')
    setSearchParams(next, { replace: true })
  }

  const summary = summaryQuery.data
  const availableYears = Array.from(
    new Set([...(summary?.availableYears ?? []), ...(year ? [year] : [])]),
  ).sort((left, right) => right - left)
  const scopeLabel = t(
    scopeOptions.find((option) => option.value === scope)?.labelKey ??
      'reports.scope.own',
  )
  const periodLabel = month
    ? `${String(month).padStart(2, '0')}/${year}`
    : year
      ? String(year)
      : t('reports.filters.allPeriods')
  const monthFormatter = new Intl.DateTimeFormat(
    language === 'vi' ? 'vi-VN' : 'en-US',
    { month: 'long' },
  )

  if (!canView) {
    return (
      <Empty className="min-h-[60svh]">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <ShieldCheck />
          </EmptyMedia>
          <EmptyTitle>{t('reports.forbiddenTitle')}</EmptyTitle>
          <EmptyDescription>
            {t('reports.forbiddenDescription')}
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    )
  }

  return (
    <m.div
      {...surfaceEnter}
      className="report-print-root mx-auto flex w-full max-w-[1600px] flex-col gap-5 p-4 sm:p-6"
    >
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="min-w-0">
          <p className="text-primary text-xs font-semibold tracking-[0.16em] uppercase">
            {t('reports.eyebrow')}
          </p>
          <h1 className="font-heading mt-1 text-2xl font-semibold tracking-tight sm:text-3xl">
            {t('reports.title')}
          </h1>
          <p className="text-muted-foreground mt-1 max-w-3xl text-sm">
            {t('reports.description')}
          </p>
        </div>
        <div className="no-print flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            onClick={() => void summaryQuery.refetch()}
            disabled={summaryQuery.isFetching}
          >
            <RefreshCw
              className={cn(
                'size-4',
                summaryQuery.isFetching && 'animate-spin',
              )}
            />
            {t('reports.actions.refresh')}
          </Button>
          {canExport ? (
            <Button
              variant="outline"
              onClick={() => exportMutation.mutate()}
              disabled={exportMutation.isPending || summaryQuery.isLoading}
            >
              <Download className="size-4" />
              {t('reports.actions.export')}
            </Button>
          ) : null}
          <PrintControl showLabel />
        </div>
      </header>

      <nav
        className="no-print flex w-fit gap-1 rounded-lg border p-1"
        aria-label={t('reports.tabs.label')}
      >
        <Link
          to={{ pathname: '/app/reports', search: location.search }}
          className={cn(
            buttonVariants({ variant: 'ghost', size: 'sm' }),
            !focusInsights && 'bg-muted text-foreground',
          )}
        >
          <BarChart3 className="size-4" />
          {t('reports.tabs.overview')}
        </Link>
        <Link
          to={{ pathname: '/app/reports/insights', search: location.search }}
          className={cn(
            buttonVariants({ variant: 'ghost', size: 'sm' }),
            focusInsights && 'bg-muted text-foreground',
          )}
        >
          <Sparkles className="size-4" />
          {t('reports.tabs.insights')}
        </Link>
      </nav>

      <section className="no-print grid gap-3 rounded-xl border p-4 md:grid-cols-3">
        <div className="grid gap-1.5">
          <label
            className="text-muted-foreground text-xs font-medium"
            htmlFor="report-scope"
          >
            {t('reports.filters.scope')}
          </label>
          <Select
            value={scope}
            onValueChange={(value: ReportScope) =>
              updateFilters({ scope: value })
            }
          >
            <SelectTrigger id="report-scope" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {scopeOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {t(option.labelKey)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1.5">
          <label
            className="text-muted-foreground text-xs font-medium"
            htmlFor="report-year"
          >
            {t('reports.filters.year')}
          </label>
          <Select
            value={year ? String(year) : 'all'}
            onValueChange={(value: string) =>
              updateFilters({ year: value === 'all' ? undefined : value })
            }
          >
            <SelectTrigger id="report-year" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">
                {t('reports.filters.allYears')}
              </SelectItem>
              {availableYears.map((value) => (
                <SelectItem key={value} value={String(value)}>
                  {value}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1.5">
          <label
            className="text-muted-foreground text-xs font-medium"
            htmlFor="report-month"
          >
            {t('reports.filters.month')}
          </label>
          <Select
            value={month ? String(month) : 'all'}
            disabled={!year}
            onValueChange={(value: string) =>
              updateFilters({ month: value === 'all' ? undefined : value })
            }
          >
            <SelectTrigger id="report-month" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">
                {t('reports.filters.allMonths')}
              </SelectItem>
              {Array.from({ length: 12 }, (_, index) => index + 1).map(
                (value) => (
                  <SelectItem key={value} value={String(value)}>
                    {monthFormatter.format(new Date(2026, value - 1, 1))}
                  </SelectItem>
                ),
              )}
            </SelectContent>
          </Select>
        </div>
      </section>

      <div className="hidden print:block">
        <p className="text-sm font-semibold">
          {scopeLabel} · {periodLabel}
        </p>
        <p className="text-xs">
          {t('reports.generatedAt', {
            value: formatDateTime(summary?.generatedAt, language),
          })}
        </p>
      </div>

      {summaryQuery.isLoading ? (
        <ReportSkeleton />
      ) : summaryQuery.isError || !summary ? (
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <AlertTriangle />
            </EmptyMedia>
            <EmptyTitle>{t('reports.loadErrorTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('reports.loadErrorDescription')}
            </EmptyDescription>
          </EmptyHeader>
          <Button variant="outline" onClick={() => void summaryQuery.refetch()}>
            {t('actions.retry')}
          </Button>
        </Empty>
      ) : (
        <>
          <ReportHero
            summary={summary}
            scopeLabel={scopeLabel}
            periodLabel={periodLabel}
            language={language}
          />

          <KpiGrid summary={summary} language={language} />

          {focusInsights ? (
            <InsightPanel
              data={insightQuery.data}
              isLoading={insightQuery.isFetching}
              isError={insightQuery.isError}
              onGenerate={() => void insightQuery.refetch()}
              scopeLabel={scopeLabel}
              periodLabel={periodLabel}
              language={language}
            />
          ) : null}

          <ReportCharts summary={summary} language={language} />

          {!focusInsights ? (
            <InsightPanel
              data={insightQuery.data}
              isLoading={insightQuery.isFetching}
              isError={insightQuery.isError}
              onGenerate={() => void insightQuery.refetch()}
              scopeLabel={scopeLabel}
              periodLabel={periodLabel}
              language={language}
            />
          ) : null}

          <TopProductsTable summary={summary} language={language} />
        </>
      )}
    </m.div>
  )
}

function ReportHero({
  summary,
  scopeLabel,
  periodLabel,
  language,
}: {
  summary: ReportSummaryResDto
  scopeLabel: string
  periodLabel: string
  language: 'vi' | 'en'
}) {
  const { t } = useTranslation()
  const hasExactPeriod = Boolean(summary.year && summary.month)
  const hasSettlement = Boolean(summary.settlementId)
  const reconciled = hasSettlement && summary.isSettlementReconciled

  return (
    <section className="report-section via-background grid gap-4 rounded-xl border bg-gradient-to-br from-sky-50/70 to-teal-50/60 p-5 lg:grid-cols-[1fr_auto] dark:from-sky-950/20 dark:to-teal-950/10">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="outline">{scopeLabel}</Badge>
          <Badge variant="secondary">{periodLabel}</Badge>
        </div>
        <h2 className="font-heading mt-4 text-xl font-semibold sm:text-2xl">
          {t('reports.story.title', {
            orders: finiteNumber(summary.totalOrders),
            amount: formatCurrency(summary.totalAmount, language),
          })}
        </h2>
        <p className="text-muted-foreground mt-1 text-sm">
          {t('reports.generatedAt', {
            value: formatDateTime(summary.generatedAt, language),
          })}
        </p>
      </div>
      <div className="bg-background/85 min-w-64 rounded-lg border p-4 backdrop-blur-sm">
        <div className="flex items-center gap-2">
          {reconciled ? (
            <CheckCircle2 className="size-4 text-emerald-600" />
          ) : (
            <FileSpreadsheet className="text-muted-foreground size-4" />
          )}
          <p className="text-sm font-semibold">
            {t('reports.reconciliation.title')}
          </p>
        </div>
        <p className="text-muted-foreground mt-2 text-xs leading-relaxed">
          {!hasExactPeriod
            ? t('reports.reconciliation.selectPeriod')
            : !hasSettlement
              ? t('reports.reconciliation.unavailable')
              : reconciled
                ? t('reports.reconciliation.reconciled', {
                    revision: summary.settlementRevisionNumber ?? 1,
                  })
                : t('reports.reconciliation.variance', {
                    value: formatCurrency(summary.settlementVariance, language),
                  })}
        </p>
        {hasSettlement ? (
          <dl className="mt-3 grid grid-cols-2 gap-3 text-xs">
            <div>
              <dt className="text-muted-foreground">
                {t('reports.reconciliation.supplier')}
              </dt>
              <dd className="mt-0.5 font-medium">
                {summary.settlementPrimarySupplierName || '—'}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground">
                {t('reports.reconciliation.total')}
              </dt>
              <dd className="mt-0.5 font-medium tabular-nums">
                {formatCurrency(summary.settlementGrandTotal, language)}
              </dd>
            </div>
          </dl>
        ) : null}
      </div>
    </section>
  )
}

function KpiGrid({
  summary,
  language,
}: {
  summary: ReportSummaryResDto
  language: 'vi' | 'en'
}) {
  const { t } = useTranslation()
  const metrics = [
    ['reports.kpi.orders', formatNumber(summary.totalOrders, language)],
    ['reports.kpi.requesters', formatNumber(summary.totalRequesters, language)],
    ['reports.kpi.lines', formatNumber(summary.totalLines, language)],
    ['reports.kpi.quantity', formatNumber(summary.totalQuantity, language)],
    ['reports.kpi.amount', formatCurrency(summary.totalAmount, language)],
  ] as const

  return (
    <section
      className="report-section grid gap-3 sm:grid-cols-2 xl:grid-cols-5"
      aria-label={t('reports.kpi.label')}
    >
      {metrics.map(([labelKey, value]) => (
        <Card key={labelKey} size="sm">
          <CardContent>
            <p className="text-muted-foreground text-xs">{t(labelKey)}</p>
            <p className="font-heading mt-2 text-xl font-semibold tabular-nums">
              {value}
            </p>
          </CardContent>
        </Card>
      ))}
    </section>
  )
}

function ReportCharts({
  summary,
  language,
}: {
  summary: ReportSummaryResDto
  language: 'vi' | 'en'
}) {
  const { t } = useTranslation()
  const trend = summary.periodTrend ?? []
  const statuses = (summary.statusBreakdown ?? []).map((point) => ({
    ...point,
    label: t(statusTranslationKey(point.resourceKey)),
  }))
  const departments = [...(summary.departmentBreakdown ?? [])]
    .sort(
      (left, right) =>
        finiteNumber(right.totalAmount) - finiteNumber(left.totalAmount),
    )
    .slice(0, 8)

  return (
    <section className="grid gap-4 xl:grid-cols-2">
      <ChartCard
        title={t('reports.charts.trendTitle')}
        description={t('reports.charts.trendDescription')}
        empty={trend.length === 0}
      >
        <p className="sr-only">
          {t('reports.charts.trendSummary', { count: trend.length })}
        </p>
        <ChartContainer
          config={trendConfig}
          className="aspect-auto h-72 w-full"
        >
          <AreaChart data={trend} accessibilityLayer>
            <defs>
              <linearGradient
                id="report-trend-fill"
                x1="0"
                y1="0"
                x2="0"
                y2="1"
              >
                <stop
                  offset="0%"
                  stopColor="var(--color-totalAmount)"
                  stopOpacity={0.28}
                />
                <stop
                  offset="100%"
                  stopColor="var(--color-totalAmount)"
                  stopOpacity={0.02}
                />
              </linearGradient>
            </defs>
            <CartesianGrid vertical={false} strokeDasharray="3 3" />
            <XAxis dataKey="period" tickLine={false} axisLine={false} />
            <YAxis
              width={64}
              tickLine={false}
              axisLine={false}
              tickFormatter={(value) => compactNumber(Number(value), language)}
            />
            <ChartTooltip
              cursor={false}
              content={
                <ChartTooltipContent
                  formatter={(value) => (
                    <span className="font-mono font-medium tabular-nums">
                      {formatCurrency(Number(value), language)}
                    </span>
                  )}
                />
              }
            />
            <Area
              type="monotone"
              dataKey="totalAmount"
              stroke="var(--color-totalAmount)"
              fill="url(#report-trend-fill)"
              strokeWidth={2}
              isAnimationActive="auto"
            />
          </AreaChart>
        </ChartContainer>
      </ChartCard>

      <ChartCard
        title={t('reports.charts.statusTitle')}
        description={t('reports.charts.statusDescription')}
        empty={statuses.length === 0}
      >
        <p className="sr-only">
          {t('reports.charts.statusSummary', { count: statuses.length })}
        </p>
        <ChartContainer
          config={statusConfig}
          className="aspect-auto h-72 w-full"
        >
          <BarChart
            data={statuses}
            layout="vertical"
            accessibilityLayer
            margin={{ left: 8 }}
          >
            <CartesianGrid horizontal={false} strokeDasharray="3 3" />
            <XAxis
              type="number"
              allowDecimals={false}
              tickLine={false}
              axisLine={false}
            />
            <YAxis
              type="category"
              dataKey="label"
              width={116}
              tickLine={false}
              axisLine={false}
            />
            <ChartTooltip
              cursor={{ fill: 'var(--muted)' }}
              content={<ChartTooltipContent hideLabel />}
            />
            <Bar
              dataKey="orderCount"
              fill="var(--color-orderCount)"
              radius={[0, 4, 4, 0]}
              isAnimationActive="auto"
            />
          </BarChart>
        </ChartContainer>
      </ChartCard>

      <ChartCard
        title={t('reports.charts.departmentTitle')}
        description={t('reports.charts.departmentDescription')}
        empty={departments.length === 0}
        className="xl:col-span-2"
      >
        <p className="sr-only">
          {t('reports.charts.departmentSummary', {
            count: departments.length,
          })}
        </p>
        <ChartContainer
          config={departmentConfig}
          className="aspect-auto h-80 w-full"
        >
          <BarChart
            data={departments}
            layout="vertical"
            accessibilityLayer
            margin={{ left: 8, right: 24 }}
          >
            <CartesianGrid horizontal={false} strokeDasharray="3 3" />
            <XAxis
              type="number"
              tickLine={false}
              axisLine={false}
              tickFormatter={(value) => compactNumber(Number(value), language)}
            />
            <YAxis
              type="category"
              dataKey="code"
              width={108}
              tickLine={false}
              axisLine={false}
            />
            <ChartTooltip
              cursor={{ fill: 'var(--muted)' }}
              content={
                <ChartTooltipContent
                  hideLabel
                  formatter={(value) => (
                    <span className="font-mono font-medium tabular-nums">
                      {formatCurrency(Number(value), language)}
                    </span>
                  )}
                />
              }
            />
            <Bar
              dataKey="totalAmount"
              fill="var(--color-totalAmount)"
              radius={[0, 4, 4, 0]}
              isAnimationActive="auto"
            />
          </BarChart>
        </ChartContainer>
      </ChartCard>
    </section>
  )
}

function ChartCard({
  title,
  description,
  empty,
  className,
  children,
}: {
  title: string
  description: string
  empty: boolean
  className?: string
  children: React.ReactNode
}) {
  const { t } = useTranslation()
  return (
    <Card className={cn('report-section', className)}>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent>
        {empty ? (
          <Empty className="h-72">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <BarChart3 />
              </EmptyMedia>
              <EmptyTitle>{t('reports.charts.emptyTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('reports.charts.emptyDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          children
        )}
      </CardContent>
    </Card>
  )
}

function InsightPanel({
  data,
  isLoading,
  isError,
  onGenerate,
  scopeLabel,
  periodLabel,
  language,
}: {
  data?: ReportInsightResDto
  isLoading: boolean
  isError: boolean
  onGenerate: () => void
  scopeLabel: string
  periodLabel: string
  language: 'vi' | 'en'
}) {
  const { t } = useTranslation()
  const isAi = data?.isAiGenerated === true

  return (
    <Card className="report-section border-primary/20">
      <CardHeader>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <Bot className="text-primary size-5" />
              <CardTitle>{t('reports.insights.title')}</CardTitle>
            </div>
            <CardDescription className="mt-1">
              {t('reports.insights.description')}
            </CardDescription>
          </div>
          <Button
            className="no-print"
            onClick={onGenerate}
            disabled={isLoading}
          >
            <Sparkles className="size-4" />
            {data
              ? t('reports.insights.regenerate')
              : t('reports.insights.generate')}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="grid gap-3">
            <Skeleton className="h-5 w-2/3" />
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-20 w-full" />
          </div>
        ) : isError ? (
          <div className="border-destructive/30 bg-destructive/5 flex flex-col gap-3 rounded-lg border p-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="font-medium">{t('reports.insights.errorTitle')}</p>
              <p className="text-muted-foreground mt-1 text-sm">
                {t('reports.insights.errorDescription')}
              </p>
            </div>
            <Button variant="outline" onClick={onGenerate}>
              {t('actions.retry')}
            </Button>
          </div>
        ) : !data ? (
          <div className="flex flex-col items-start gap-3 rounded-lg border border-dashed p-5">
            <div className="bg-primary/10 text-primary rounded-lg p-2">
              <Lightbulb className="size-5" />
            </div>
            <div>
              <p className="font-medium">{t('reports.insights.idleTitle')}</p>
              <p className="text-muted-foreground mt-1 max-w-3xl text-sm">
                {t('reports.insights.idleDescription')}
              </p>
            </div>
          </div>
        ) : (
          <div className="grid gap-4">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={isAi ? 'default' : 'secondary'}>
                {isAi
                  ? t('reports.insights.aiGenerated')
                  : t('reports.insights.rulesFallback')}
              </Badge>
              {data.source ? (
                <Badge variant="outline">
                  {t('reports.insights.source', { value: data.source })}
                </Badge>
              ) : null}
              {data.model ? (
                <Badge variant="outline">{data.model}</Badge>
              ) : null}
            </div>

            <p className="text-base leading-relaxed font-medium">
              {data.summary || t('reports.insights.noSummary')}
            </p>

            <div className="grid gap-4 lg:grid-cols-3">
              <InsightList
                icon={<TrendingUp />}
                title={t('reports.insights.highlights')}
                items={data.highlights}
              />
              <InsightList
                icon={<AlertTriangle />}
                title={t('reports.insights.risks')}
                items={data.risks}
              />
              <InsightList
                icon={<Lightbulb />}
                title={t('reports.insights.recommendations')}
                items={data.recommendations}
              />
            </div>

            <p className="text-muted-foreground border-t pt-3 text-xs leading-relaxed">
              {t('reports.insights.evidence', {
                scope: scopeLabel,
                period: periodLabel,
                value: formatDateTime(data.generatedAt, language),
              })}
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function InsightList({
  icon,
  title,
  items,
}: {
  icon: React.ReactNode
  title: string
  items?: string[] | null
}) {
  return (
    <section className="rounded-lg border p-4">
      <h3 className="flex items-center gap-2 text-sm font-semibold [&_svg]:size-4">
        {icon}
        {title}
      </h3>
      {items?.length ? (
        <ul className="text-muted-foreground mt-3 grid gap-2 text-sm leading-relaxed">
          {items.map((item, index) => (
            <li key={`${index}-${item}`} className="flex gap-2">
              <span
                aria-hidden
                className="text-primary mt-2 size-1 shrink-0 rounded-full bg-current"
              />
              <span>{item}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-muted-foreground mt-3 text-sm">—</p>
      )}
    </section>
  )
}

function TopProductsTable({
  summary,
  language,
}: {
  summary: ReportSummaryResDto
  language: 'vi' | 'en'
}) {
  const { t } = useTranslation()
  const items = summary.topProducts ?? []

  return (
    <Card className="report-section">
      <CardHeader>
        <CardTitle>{t('reports.products.title')}</CardTitle>
        <CardDescription>{t('reports.products.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        {items.length === 0 ? (
          <Empty className="min-h-48">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <FileSpreadsheet />
              </EmptyMedia>
              <EmptyTitle>{t('reports.products.emptyTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('reports.products.emptyDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-14">#</TableHead>
                  <TableHead>{t('reports.products.item')}</TableHead>
                  <TableHead>{t('reports.products.code')}</TableHead>
                  <TableHead className="text-right">
                    {t('reports.products.quantity')}
                  </TableHead>
                  <TableHead className="text-right">
                    {t('reports.products.amount')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item, index) => (
                  <TableRow key={`${item.productCode}-${index}`}>
                    <TableCell className="text-muted-foreground tabular-nums">
                      {index + 1}
                    </TableCell>
                    <TableCell className="font-medium">
                      {item.productName || '—'}
                    </TableCell>
                    <TableCell className="text-muted-foreground font-mono text-xs">
                      {item.productCode || '—'}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatNumber(item.totalQuantity, language)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatCurrency(item.totalAmount, language)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function ReportSkeleton() {
  return (
    <div className="grid gap-4" aria-busy="true">
      <Skeleton className="h-52 w-full" />
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        {Array.from({ length: 5 }, (_, index) => (
          <Skeleton key={index} className="h-24 w-full" />
        ))}
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        <Skeleton className="h-96 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    </div>
  )
}

function formatNumber(value: number | undefined | null, language: 'vi' | 'en') {
  return new Intl.NumberFormat(language === 'vi' ? 'vi-VN' : 'en-US').format(
    finiteNumber(value),
  )
}

function compactNumber(value: number, language: 'vi' | 'en') {
  return new Intl.NumberFormat(language === 'vi' ? 'vi-VN' : 'en-US', {
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(finiteNumber(value))
}

function formatCurrency(
  value: number | undefined | null,
  language: 'vi' | 'en',
) {
  return new Intl.NumberFormat(language === 'vi' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(finiteNumber(value))
}

function formatDateTime(value: string | undefined, language: 'vi' | 'en') {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat(language === 'vi' ? 'vi-VN' : 'en-US', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}
