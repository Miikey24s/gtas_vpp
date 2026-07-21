import { useQuery } from '@tanstack/react-query'
import {
  ChevronLeft,
  ChevronRight,
  PackageOpen,
  Plus,
  RefreshCw,
  Search,
  ShieldX,
} from 'lucide-react'
import { useDeferredValue, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestCategories,
  getApiVppRequestProducts,
  type VppItemResDto,
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

const pageSize = 20

function formatPrice(value: number | null | undefined, language: string) {
  if (value == null) return '—'
  return new Intl.NumberFormat(language === 'en' ? 'en-US' : 'vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function CatalogSkeleton() {
  return (
    <div className="space-y-4" aria-label="Loading">
      <Skeleton className="h-20 w-full" />
      <Skeleton className="h-80 w-full" />
    </div>
  )
}

function CatalogCard({
  item,
  language,
}: {
  item: VppItemResDto
  language: string
}) {
  const { t } = useTranslation()

  return (
    <article className="bg-card border-border border p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-primary truncate text-xs font-semibold">
            {item.vppCode}
          </p>
          <h2 className="mt-1 text-sm leading-5 font-semibold">
            {item.displayName || item.vppName}
          </h2>
        </div>
        <Badge variant="outline" className="shrink-0 rounded-[4px]">
          {item.uomName || item.uomCode || '—'}
        </Badge>
      </div>
      <dl className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 text-xs">
        <div>
          <dt className="text-muted-foreground">{t('catalog.category')}</dt>
          <dd className="mt-1 font-medium">{item.vppCategoryName || '—'}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">{t('catalog.price')}</dt>
          <dd className="mt-1 font-medium tabular-nums">
            {formatPrice(item.defaultPrice, language)}
          </dd>
        </div>
        <div className="col-span-2">
          <dt className="text-muted-foreground">{t('catalog.supplier')}</dt>
          <dd className="mt-1 font-medium">
            {item.defaultSupplierName || '—'}
          </dd>
        </div>
      </dl>
      {item.description ? (
        <p className="text-muted-foreground mt-4 text-xs leading-5">
          {item.description}
        </p>
      ) : null}
    </article>
  )
}

export function CatalogPage() {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const canView = hasPermission(permissions.requestCatalogView)
  const canCreate = hasPermission(permissions.requestCreate)
  const language = i18n.resolvedLanguage ?? 'vi'
  const [searchText, setSearchText] = useState('')
  const deferredSearch = useDeferredValue(searchText.trim())
  const [categoryId, setCategoryId] = useState('all')
  const [page, setPage] = useState(0)

  useEffect(() => setPage(0), [deferredSearch, categoryId])

  const categoriesQuery = useQuery({
    queryKey: ['vpp', 'catalog', 'categories', language],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiVppRequestCategories()
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const productsQuery = useQuery({
    queryKey: [
      'vpp',
      'catalog',
      'products',
      language,
      categoryId,
      deferredSearch,
      page,
    ],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiVppRequestProducts({
        query: {
          categoryId: categoryId === 'all' ? undefined : categoryId,
          search: deferredSearch || undefined,
          skip: page * pageSize,
          top: pageSize,
          orderby: 'VppName asc',
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      const total = Number(result.response?.headers.get('X-Total-Count'))
      return {
        items: result.data,
        total: Number.isFinite(total) ? total : result.data.length,
      }
    },
  })

  const items = productsQuery.data?.items ?? []
  const total = productsQuery.data?.total ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  const categories = categoriesQuery.data ?? []
  const pagePriceCoverage = items.filter(
    (item) => item.defaultPrice != null,
  ).length

  if (!canView) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('catalog.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('catalog.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const refresh = () => {
    void Promise.all([productsQuery.refetch(), categoriesQuery.refetch()])
  }

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {t('catalog.eyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.03em] sm:text-3xl">
            {t('catalog.title')}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-2xl text-sm leading-6">
            {t('catalog.description', { count: total })}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={refresh}
            disabled={productsQuery.isFetching}
          >
            <RefreshCw
              className={productsQuery.isFetching ? 'animate-spin' : ''}
              aria-hidden="true"
            />
            {t('actions.refresh')}
          </Button>
          {canCreate ? (
            <Button asChild size="sm">
              <Link to="/app/orders/new" viewTransition>
                <Plus aria-hidden="true" />
                {t('catalog.createOrder')}
              </Link>
            </Button>
          ) : null}
        </div>
      </header>

      <section
        className="bg-card border-border mt-6 border"
        aria-label={t('catalog.filters')}
      >
        <div className="grid gap-3 p-4 md:grid-cols-[minmax(16rem,1fr)_18rem_auto] md:items-end">
          <label className="block">
            <span className="mb-1.5 block text-xs font-medium">
              {t('catalog.searchLabel')}
            </span>
            <span className="relative block">
              <Search
                className="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2"
                aria-hidden="true"
              />
              <Input
                value={searchText}
                onChange={(event) => setSearchText(event.target.value)}
                placeholder={t('catalog.searchPlaceholder')}
                className="pl-8"
              />
            </span>
          </label>
          <div>
            <label
              className="mb-1.5 block text-xs font-medium"
              htmlFor="catalog-category"
            >
              {t('catalog.categoryLabel')}
            </label>
            <Select
              value={categoryId}
              onValueChange={(value: string) => setCategoryId(value)}
            >
              <SelectTrigger id="catalog-category" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">
                  {t('catalog.allCategories')}
                </SelectItem>
                {categories.map((category) =>
                  category.id ? (
                    <SelectItem key={category.id} value={category.id}>
                      {category.vppCategoryName ||
                        category.vppCategoryCode ||
                        '—'}
                    </SelectItem>
                  ) : null,
                )}
              </SelectContent>
            </Select>
          </div>
          <div className="text-muted-foreground flex gap-4 text-xs md:justify-end">
            <span>
              <strong className="text-foreground block text-base tabular-nums">
                {total}
              </strong>
              {t('catalog.totalItems')}
            </span>
            <span>
              <strong className="text-foreground block text-base tabular-nums">
                {pagePriceCoverage}/{items.length || 0}
              </strong>
              {t('catalog.priceCoverage')}
            </span>
          </div>
        </div>
      </section>

      <div className="mt-4">
        {productsQuery.isLoading ? <CatalogSkeleton /> : null}

        {productsQuery.isError ? (
          <Empty className="min-h-72 border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <PackageOpen aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{t('catalog.loadErrorTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('catalog.loadErrorDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}

        {!productsQuery.isLoading &&
        !productsQuery.isError &&
        items.length === 0 ? (
          <Empty className="min-h-72 border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <PackageOpen aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{t('catalog.emptyTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('catalog.emptyDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}

        {items.length > 0 ? (
          <>
            <div className="hidden overflow-hidden border md:block">
              <Table>
                <TableHeader className="bg-muted/35">
                  <TableRow>
                    <TableHead>{t('catalog.product')}</TableHead>
                    <TableHead>{t('catalog.category')}</TableHead>
                    <TableHead>{t('catalog.uom')}</TableHead>
                    <TableHead>{t('catalog.supplier')}</TableHead>
                    <TableHead className="text-right">
                      {t('catalog.price')}
                    </TableHead>
                    <TableHead>{t('catalog.descriptionLabel')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((item, index) => (
                    <TableRow key={item.id ?? index}>
                      <TableCell className="min-w-64 whitespace-normal">
                        <span className="text-primary block text-xs font-semibold">
                          {item.vppCode}
                        </span>
                        <span className="mt-0.5 block font-medium">
                          {item.displayName || item.vppName}
                        </span>
                      </TableCell>
                      <TableCell className="max-w-52 whitespace-normal">
                        {item.vppCategoryName || '—'}
                      </TableCell>
                      <TableCell>
                        {item.uomName || item.uomCode || '—'}
                      </TableCell>
                      <TableCell className="max-w-52 whitespace-normal">
                        {item.defaultSupplierName || '—'}
                      </TableCell>
                      <TableCell className="text-right font-medium tabular-nums">
                        {formatPrice(item.defaultPrice, language)}
                      </TableCell>
                      <TableCell className="text-muted-foreground max-w-80 whitespace-normal">
                        {item.description || '—'}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <div className="grid gap-3 md:hidden">
              {items.map((item, index) => (
                <CatalogCard
                  key={item.id ?? index}
                  item={item}
                  language={language}
                />
              ))}
            </div>

            <nav
              className="mt-4 flex items-center justify-between gap-4"
              aria-label={t('catalog.pagination')}
            >
              <p className="text-muted-foreground text-xs tabular-nums">
                {t('catalog.pageSummary', { page: page + 1, pageCount, total })}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((value) => Math.max(0, value - 1))}
                  disabled={page === 0 || productsQuery.isFetching}
                >
                  <ChevronLeft aria-hidden="true" />
                  {t('actions.previous')}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((value) => value + 1)}
                  disabled={page + 1 >= pageCount || productsQuery.isFetching}
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
