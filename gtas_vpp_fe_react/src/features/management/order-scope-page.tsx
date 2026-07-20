import { useQuery } from '@tanstack/react-query'
import {
  Building2,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  ExternalLink,
  PackageOpen,
  RefreshCw,
  ShieldX,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestAllOrders,
  getApiVppRequestDepartmentOrders,
  type VppRequestResDto,
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
import { Input } from '@/components/ui/input'
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
import {
  formatDateTime,
  formatPeriod,
  getOrderStatusLabel,
  getStatusClass,
} from '@/features/orders/order-format'
import { cn } from '@/lib/utils'

export type ManagementScope = 'department' | 'company'

const pageSize = 20

function parseOptionalNumber(value: string | null) {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) ? parsed : undefined
}

function parseHeader(response: Response | undefined, name: string) {
  const value = Number(response?.headers.get(name))
  return Number.isFinite(value) ? value : 0
}

function formatCurrency(value: number, language: string) {
  return new Intl.NumberFormat(language === 'en' ? 'en-US' : 'vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function OrderMobileCard({
  order,
  scope,
}: {
  order: VppRequestResDto
  scope: ManagementScope
}) {
  const { t, i18n } = useTranslation()
  const language = i18n.resolvedLanguage ?? 'vi'

  return (
    <article className="border-border border p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="truncate font-semibold">{order.vppCode || '—'}</h2>
          <p className="text-muted-foreground mt-1 truncate text-xs">
            {order.requesterName || '—'} · {order.departmentCode || '—'}
          </p>
        </div>
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
      <dl className="mt-4 grid grid-cols-3 gap-3 text-xs">
        <div>
          <dt className="text-muted-foreground">{t('management.period')}</dt>
          <dd className="mt-1 font-medium">
            {formatPeriod(order.month, order.year)}
          </dd>
        </div>
        <div>
          <dt className="text-muted-foreground">{t('management.items')}</dt>
          <dd className="mt-1 font-medium tabular-nums">
            {order.totalLines ?? 0}
          </dd>
        </div>
        <div>
          <dt className="text-muted-foreground">{t('management.quantity')}</dt>
          <dd className="mt-1 font-medium tabular-nums">
            {order.totalQty ?? 0}
          </dd>
        </div>
      </dl>
      <div className="border-border mt-4 flex items-center justify-between gap-3 border-t pt-3">
        <span className="text-muted-foreground text-xs">
          {formatDateTime(order.submittedDate, language)}
        </span>
        {order.id ? (
          <Button asChild variant="ghost" size="sm">
            <Link to={`/app/orders/${order.id}?from=${scope}`} viewTransition>
              {t('management.open')}
              <ExternalLink aria-hidden="true" />
            </Link>
          </Button>
        ) : null}
      </div>
    </article>
  )
}

export function OrderScopePage({ scope }: { scope: ManagementScope }) {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const language = i18n.resolvedLanguage ?? 'vi'
  const permission =
    scope === 'company'
      ? permissions.requestViewAll
      : permissions.requestViewDepartment
  const canView = hasPermission(permission)
  const year = parseOptionalNumber(searchParams.get('year'))
  const month = parseOptionalNumber(searchParams.get('month'))
  const status = parseOptionalNumber(searchParams.get('status'))
  const departmentCode = searchParams.get('department')?.trim() || undefined
  const page = Math.max(0, parseOptionalNumber(searchParams.get('page')) ?? 0)

  const ordersQuery = useQuery({
    queryKey: [
      'vpp',
      'management',
      scope,
      year,
      month,
      status,
      departmentCode,
      page,
    ],
    enabled: canView,
    queryFn: async () => {
      const options = {
        query: {
          year,
          month,
          status,
          departmentCode: scope === 'company' ? departmentCode : undefined,
          skip: page * pageSize,
          top: pageSize,
        },
      }
      const result =
        scope === 'company'
          ? await getApiVppRequestAllOrders(options)
          : await getApiVppRequestDepartmentOrders(options)
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return {
        orders: result.data,
        totalCount: parseHeader(result.response, 'X-Total-Count'),
        totalLines: parseHeader(result.response, 'X-Total-Lines'),
        totalQty: parseHeader(result.response, 'X-Total-Qty'),
        totalAmount: parseHeader(result.response, 'X-Total-Amount'),
      }
    },
  })

  const updateFilter = (key: string, value?: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    next.delete('page')
    setSearchParams(next)
  }

  const updatePage = (nextPage: number) => {
    const next = new URLSearchParams(searchParams)
    if (nextPage > 0) next.set('page', String(nextPage))
    else next.delete('page')
    setSearchParams(next)
  }

  if (!canView) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('management.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('management.forbiddenDescription', { permission })}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const orders = ordersQuery.data?.orders ?? []
  const total = ordersQuery.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {t('management.eyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.035em] sm:text-3xl">
            {t(
              scope === 'company'
                ? 'management.companyTitle'
                : 'management.departmentTitle',
            )}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t(
              scope === 'company'
                ? 'management.companyDescription'
                : 'management.departmentDescription',
            )}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => void ordersQuery.refetch()}
          disabled={ordersQuery.isFetching}
        >
          <RefreshCw
            className={ordersQuery.isFetching ? 'animate-spin' : ''}
            aria-hidden="true"
          />
          {t('actions.refresh')}
        </Button>
      </header>

      <section className="border-border mt-6 grid grid-cols-2 border lg:grid-cols-4">
        {[
          [t('management.totalOrders'), total],
          [t('management.totalItems'), ordersQuery.data?.totalLines ?? 0],
          [t('management.totalQuantity'), ordersQuery.data?.totalQty ?? 0],
          [
            scope === 'company'
              ? t('management.totalAmount')
              : t('management.departmentsInPage'),
            scope === 'company'
              ? formatCurrency(ordersQuery.data?.totalAmount ?? 0, language)
              : new Set(orders.map((order) => order.departmentCode)).size,
          ],
        ].map(([label, value], index) => (
          <div
            key={String(label)}
            className={cn(
              'min-w-0 p-4 sm:p-5',
              index % 2 === 1 && 'border-l',
              index >= 2 && 'border-t lg:border-t-0',
              index > 0 && 'lg:border-l',
            )}
          >
            <p className="truncate text-xl font-semibold tabular-nums">
              {value}
            </p>
            <p className="text-muted-foreground mt-1 truncate text-xs">
              {label}
            </p>
          </div>
        ))}
      </section>

      <section
        className="bg-card border-border mt-5 border"
        aria-label={t('management.filters')}
      >
        <div className="grid gap-3 p-4 sm:grid-cols-2 lg:grid-cols-5">
          <label>
            <span className="mb-1.5 block text-xs font-medium">
              {t('management.year')}
            </span>
            <Input
              type="number"
              inputMode="numeric"
              min={2000}
              max={2100}
              value={year ?? ''}
              onChange={(event) => updateFilter('year', event.target.value)}
              placeholder={t('management.allYears')}
            />
          </label>
          <div>
            <label
              className="mb-1.5 block text-xs font-medium"
              htmlFor={`${scope}-month`}
            >
              {t('management.month')}
            </label>
            <Select
              value={month ? String(month) : 'all'}
              onValueChange={(value: string) =>
                updateFilter('month', value === 'all' ? undefined : value)
              }
            >
              <SelectTrigger id={`${scope}-month`} className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('management.allMonths')}</SelectItem>
                {Array.from({ length: 12 }, (_, index) => index + 1).map(
                  (value) => (
                    <SelectItem key={value} value={String(value)}>
                      {t('management.monthValue', { value })}
                    </SelectItem>
                  ),
                )}
              </SelectContent>
            </Select>
          </div>
          <div>
            <label
              className="mb-1.5 block text-xs font-medium"
              htmlFor={`${scope}-status`}
            >
              {t('management.status')}
            </label>
            <Select
              value={status ? String(status) : 'all'}
              onValueChange={(value: string) =>
                updateFilter('status', value === 'all' ? undefined : value)
              }
            >
              <SelectTrigger id={`${scope}-status`} className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">
                  {t('management.allStatuses')}
                </SelectItem>
                {[1, 4, 6, 7, 8].map((value) => (
                  <SelectItem key={value} value={String(value)}>
                    {getOrderStatusLabel(
                      value,
                      false,
                      value !== 1 && value !== 4,
                      t,
                    )}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          {scope === 'company' ? (
            <label>
              <span className="mb-1.5 block text-xs font-medium">
                {t('management.departmentCode')}
              </span>
              <Input
                value={departmentCode ?? ''}
                onChange={(event) =>
                  updateFilter('department', event.target.value.trimStart())
                }
                placeholder={t('management.departmentPlaceholder')}
              />
            </label>
          ) : (
            <div className="text-muted-foreground flex items-end gap-2 pb-2 text-xs lg:col-span-2">
              <Building2 className="size-4" aria-hidden="true" />
              {t('management.departmentScopeNote')}
            </div>
          )}
          <div className="flex items-end">
            <Button
              type="button"
              variant="ghost"
              className="w-full"
              onClick={() => setSearchParams({})}
            >
              {t('management.clearFilters')}
            </Button>
          </div>
        </div>
      </section>

      <div className="mt-4">
        {ordersQuery.isLoading ? (
          <div className="space-y-3">
            <Skeleton className="h-14" />
            <Skeleton className="h-72" />
          </div>
        ) : null}
        {ordersQuery.isError ? (
          <Empty className="min-h-72 border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <CircleAlert aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{t('management.errorTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('management.errorDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}
        {!ordersQuery.isLoading &&
        !ordersQuery.isError &&
        orders.length === 0 ? (
          <Empty className="min-h-72 border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <PackageOpen aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{t('management.emptyTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('management.emptyDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}
        {orders.length > 0 ? (
          <>
            <div className="hidden overflow-hidden border md:block">
              <Table>
                <TableHeader className="bg-muted/35">
                  <TableRow>
                    <TableHead>{t('management.order')}</TableHead>
                    <TableHead>{t('management.requester')}</TableHead>
                    <TableHead>{t('management.department')}</TableHead>
                    <TableHead>{t('management.period')}</TableHead>
                    <TableHead>{t('management.status')}</TableHead>
                    <TableHead className="text-right">
                      {t('management.items')}
                    </TableHead>
                    <TableHead className="text-right">
                      {t('management.quantity')}
                    </TableHead>
                    <TableHead>{t('management.submitted')}</TableHead>
                    <TableHead className="w-24" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {orders.map((order, index) => (
                    <TableRow key={order.id ?? index}>
                      <TableCell className="font-medium">
                        {order.vppCode || '—'}
                      </TableCell>
                      <TableCell>{order.requesterName || '—'}</TableCell>
                      <TableCell>{order.departmentCode || '—'}</TableCell>
                      <TableCell>
                        {formatPeriod(order.month, order.year)}
                      </TableCell>
                      <TableCell>
                        <Badge
                          variant="outline"
                          className={cn(
                            'rounded-[4px]',
                            getStatusClass(order.status),
                          )}
                        >
                          {getOrderStatusLabel(
                            order.status,
                            order.isDeadlinePassed,
                            order.isAdditionalOrder,
                            t,
                          )}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {order.totalLines ?? 0}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {order.totalQty ?? 0}
                      </TableCell>
                      <TableCell>
                        {formatDateTime(order.submittedDate, language)}
                      </TableCell>
                      <TableCell>
                        {order.id ? (
                          <Button asChild variant="ghost" size="icon-sm">
                            <Link
                              to={`/app/orders/${order.id}?from=${scope}`}
                              viewTransition
                              aria-label={t('management.openOrder', {
                                code: order.vppCode || '',
                              })}
                            >
                              <ExternalLink aria-hidden="true" />
                            </Link>
                          </Button>
                        ) : null}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <div className="grid gap-3 md:hidden">
              {orders.map((order, index) => (
                <OrderMobileCard
                  key={order.id ?? index}
                  order={order}
                  scope={scope}
                />
              ))}
            </div>
            <nav
              className="mt-4 flex items-center justify-between gap-4"
              aria-label={t('management.pagination')}
            >
              <p className="text-muted-foreground text-xs tabular-nums">
                {t('management.pageSummary', {
                  page: page + 1,
                  pageCount,
                  total,
                })}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => updatePage(Math.max(0, page - 1))}
                  disabled={page === 0 || ordersQuery.isFetching}
                >
                  <ChevronLeft aria-hidden="true" />
                  {t('actions.previous')}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => updatePage(page + 1)}
                  disabled={page + 1 >= pageCount || ordersQuery.isFetching}
                >
                  {t('actions.next')}
                  <ChevronRight aria-hidden="true" />
                </Button>
              </div>
            </nav>
          </>
        ) : null}
      </div>
    </div>
  )
}
