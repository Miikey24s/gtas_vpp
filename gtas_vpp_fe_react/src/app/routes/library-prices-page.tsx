import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  ArchiveRestore,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldX,
  Star,
} from 'lucide-react'
import { m } from 'motion/react'
import { useDeferredValue, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiCatalogItems,
  getApiLibraryByTableCode,
  getApiVppPriceItemPrices,
  getApiVppPriceList,
  patchApiVppPriceByIdDeleted,
  postApiVppPrice,
  postApiVppPriceByIdSetDefault,
  putApiVppPriceById,
  type SupplierResDto,
  type VppItemPriceResDto,
  type VppItemResDto,
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
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Skeleton } from '@/components/ui/skeleton'
import { Switch } from '@/components/ui/switch'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'
import { LibraryTabs } from '@/features/library/master-data-workspace'
import { surfaceEnter } from '@/lib/motion'

type PriceForm = {
  vppItemId: string
  price: string
  netPrice: string
  vatRate: string
  minimumOrderQuantity: string
  leadTimeDays: string
  supplierSku: string
  description: string
  isDefault: boolean
}

const pageSize = 20

const emptyForm: PriceForm = {
  vppItemId: '',
  price: '0',
  netPrice: '0',
  vatRate: '8',
  minimumOrderQuantity: '1',
  leadTimeDays: '0',
  supplierSku: '',
  description: '',
  isDefault: false,
}

function asNumber(value: string) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : 0
}

function money(value?: number | null) {
  if (value == null) return '—'
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

export function LibraryPricesPage() {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const [urlParams, setUrlParams] = useSearchParams()
  const canView = hasPermission(permissions.libraryView)
  const canManage = hasPermission(permissions.libraryManage)
  const [supplierId, setSupplierId] = useState(
    urlParams.get('supplierId') ?? '',
  )
  const [priceListId, setPriceListId] = useState(
    urlParams.get('priceListId') ?? '',
  )
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search.trim())
  const [showDeleted, setShowDeleted] = useState(false)
  const [page, setPage] = useState(0)
  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<VppItemPriceResDto | null>(null)
  const [form, setForm] = useState<PriceForm>(emptyForm)
  const [archiveTarget, setArchiveTarget] = useState<VppItemPriceResDto | null>(
    null,
  )

  const referencesQuery = useQuery({
    queryKey: ['library', 'price-references', i18n.resolvedLanguage],
    enabled: canView,
    queryFn: async () => {
      const [suppliersResult, priceListsResult, itemsResult] =
        await Promise.all([
          getApiLibraryByTableCode({
            path: { tableCode: 'suppliers' },
            query: { top: 500, orderby: 'SupplierName asc' },
          }),
          getApiVppPriceList({
            query: { top: 500, orderby: 'SupplierName asc, PriceListName asc' },
          }),
          getApiCatalogItems({
            query: { top: 500, orderby: 'VppName asc', showDeleted: false },
          }),
        ])
      if (!suppliersResult.data)
        throw toApiRequestError(suppliersResult.error, suppliersResult.response)
      if (!priceListsResult.data)
        throw toApiRequestError(
          priceListsResult.error,
          priceListsResult.response,
        )
      if (!itemsResult.data)
        throw toApiRequestError(itemsResult.error, itemsResult.response)
      return {
        suppliers: suppliersResult.data as SupplierResDto[],
        priceLists: priceListsResult.data,
        items: itemsResult.data,
      }
    },
  })

  const supplierPriceLists = (referencesQuery.data?.priceLists ?? []).filter(
    (entry) => entry.supplierId === supplierId,
  )
  const selectedPriceList = supplierPriceLists.find(
    (entry) => entry.id === priceListId,
  )
  const draftSelected = selectedPriceList?.status?.toLowerCase() === 'draft'

  useEffect(() => {
    const suppliers = referencesQuery.data?.suppliers ?? []
    if (!supplierId && suppliers[0]?.id) setSupplierId(suppliers[0].id)
  }, [referencesQuery.data?.suppliers, supplierId])

  useEffect(() => {
    if (!referencesQuery.isSuccess || supplierPriceLists.length === 0) return
    if (supplierPriceLists.some((entry) => entry.id === priceListId)) return
    const preferred =
      supplierPriceLists.find((entry) => entry.isDefault) ??
      supplierPriceLists.find(
        (entry) => entry.status?.toLowerCase() === 'draft',
      ) ??
      supplierPriceLists[0]
    setPriceListId(preferred?.id ?? '')
  }, [priceListId, referencesQuery.isSuccess, supplierPriceLists])

  useEffect(() => {
    const next = new URLSearchParams()
    if (supplierId) next.set('supplierId', supplierId)
    if (priceListId) next.set('priceListId', priceListId)
    setUrlParams(next, { replace: true })
    setPage(0)
  }, [priceListId, setUrlParams, supplierId])

  useEffect(() => setPage(0), [deferredSearch, showDeleted])

  const pricesQuery = useQuery({
    queryKey: [
      'library',
      'prices',
      i18n.resolvedLanguage,
      supplierId,
      priceListId,
      deferredSearch,
      showDeleted,
      page,
    ],
    enabled: canView && supplierId.length > 0 && priceListId.length > 0,
    queryFn: async () => {
      const result = await getApiVppPriceItemPrices({
        query: {
          supplierId,
          priceListId,
          showDeleted,
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

  const openEditor = (entry?: VppItemPriceResDto) => {
    setEditing(entry ?? null)
    setForm(
      entry
        ? {
            vppItemId: entry.vppId ?? '',
            price: String(entry.price ?? entry.netPrice ?? 0),
            netPrice: String(entry.netPrice ?? entry.price ?? 0),
            vatRate: String(entry.vatRate ?? 0),
            minimumOrderQuantity: String(entry.minimumOrderQuantity ?? 1),
            leadTimeDays: String(entry.leadTimeDays ?? 0),
            supplierSku: entry.supplierSku ?? '',
            description: entry.description ?? '',
            isDefault: entry.isDefault ?? false,
          }
        : emptyForm,
    )
    setEditorOpen(true)
  }

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!supplierId || !priceListId)
        throw new Error('A price list is required')
      const payload = {
        vppItemId: form.vppItemId,
        supplierId,
        priceListId,
        price: asNumber(form.price),
        netPrice: asNumber(form.netPrice),
        vatRate: asNumber(form.vatRate),
        minimumOrderQuantity: asNumber(form.minimumOrderQuantity),
        leadTimeDays: asNumber(form.leadTimeDays),
        supplierSku: form.supplierSku.trim() || null,
        description: form.description.trim() || null,
        isDefault: form.isDefault,
      }
      const result = editing?.priceMappingId
        ? await putApiVppPriceById({
            path: { id: editing.priceMappingId },
            body: { ...payload, id: editing.priceMappingId },
          })
        : await postApiVppPrice({ body: payload })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setEditorOpen(false)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['library', 'prices'] }),
        queryClient.invalidateQueries({ queryKey: ['library', 'price-lists'] }),
      ])
      toast.success(
        t(
          editing ? 'library.priceUpdateSuccess' : 'library.priceCreateSuccess',
        ),
      )
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const archiveMutation = useMutation({
    mutationFn: async (entry: VppItemPriceResDto) => {
      if (!entry.priceMappingId) throw new Error('Missing price mapping id')
      const result = await patchApiVppPriceByIdDeleted({
        path: { id: entry.priceMappingId },
        body: { isDeleted: !entry.isDeleted },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setArchiveTarget(null)
      await queryClient.invalidateQueries({ queryKey: ['library', 'prices'] })
      toast.success(t('library.priceDeleteSuccess'))
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const defaultMutation = useMutation({
    mutationFn: async (entry: VppItemPriceResDto) => {
      if (!entry.priceMappingId) throw new Error('Missing price mapping id')
      const result = await postApiVppPriceByIdSetDefault({
        path: { id: entry.priceMappingId },
      })
      if (result.error) throw toApiRequestError(result.error, result.response)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['library', 'prices'] })
      toast.success(t('library.priceDefaultSuccess'))
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  if (!canView) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('library.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('library.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const entries = pricesQuery.data?.items ?? []
  const total = pricesQuery.data?.total ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  const formValid =
    form.vppItemId.length > 0 &&
    asNumber(form.price) >= 0 &&
    asNumber(form.netPrice) >= 0 &&
    asNumber(form.vatRate) >= 0 &&
    asNumber(form.vatRate) <= 100 &&
    asNumber(form.minimumOrderQuantity) >= 0 &&
    asNumber(form.leadTimeDays) >= 0

  return (
    <m.div
      {...surfaceEnter}
      className="mx-auto w-full max-w-[100rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8"
    >
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {t('library.eyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.04em] sm:text-3xl">
            {t('library.pricesTitle')}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t('library.pricesDescription')}
          </p>
        </div>
        {canManage ? (
          <Button disabled={!draftSelected} onClick={() => openEditor()}>
            <Plus aria-hidden="true" />
            {t('library.create')}
          </Button>
        ) : null}
      </header>
      <LibraryTabs active="prices" />

      <section className="mt-5 grid gap-3 lg:grid-cols-[minmax(14rem,20rem)_minmax(14rem,22rem)_minmax(14rem,1fr)_auto] lg:items-end">
        <div className="space-y-2">
          <Label htmlFor="price-supplier">{t('library.supplier')}</Label>
          <Select
            value={supplierId}
            onValueChange={(value: string) => {
              setSupplierId(value)
              setPriceListId('')
            }}
          >
            <SelectTrigger id="price-supplier" className="w-full">
              <SelectValue placeholder={t('library.supplierPlaceholder')} />
            </SelectTrigger>
            <SelectContent>
              {(referencesQuery.data?.suppliers ?? []).map((supplier) =>
                supplier.id ? (
                  <SelectItem key={supplier.id} value={supplier.id}>
                    {supplier.supplierName || supplier.supplierShortName}
                  </SelectItem>
                ) : null,
              )}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="price-list">{t('library.tabs.priceLists')}</Label>
          <Select value={priceListId} onValueChange={setPriceListId}>
            <SelectTrigger id="price-list" className="w-full">
              <SelectValue placeholder={t('library.selectPriceListHint')} />
            </SelectTrigger>
            <SelectContent>
              {supplierPriceLists.map((entry) =>
                entry.id ? (
                  <SelectItem key={entry.id} value={entry.id}>
                    {entry.priceListName} · v{entry.version} ·{' '}
                    {t(
                      entry.status?.toLowerCase() === 'published'
                        ? 'library.published'
                        : entry.status?.toLowerCase() === 'expired'
                          ? 'library.expired'
                          : 'library.draft',
                    )}
                  </SelectItem>
                ) : null,
              )}
            </SelectContent>
          </Select>
        </div>
        <div className="relative">
          <Search
            className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2"
            aria-hidden="true"
          />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="pl-9"
            placeholder={t('library.searchPlaceholder')}
            aria-label={t('library.search')}
          />
        </div>
        <div className="flex items-center justify-between gap-3 lg:justify-end">
          <div className="flex items-center gap-2">
            <Switch
              id="prices-show-archived"
              checked={showDeleted}
              onCheckedChange={setShowDeleted}
            />
            <Label
              htmlFor="prices-show-archived"
              className="text-sm font-normal"
            >
              {t('library.showArchived')}
            </Label>
          </div>
          <Button
            variant="outline"
            size="icon"
            onClick={() => void pricesQuery.refetch()}
            aria-label={t('actions.refresh')}
          >
            <RefreshCw
              className={pricesQuery.isFetching ? 'animate-spin' : ''}
              aria-hidden="true"
            />
          </Button>
        </div>
      </section>

      {!supplierId ? (
        <Empty className="mt-4 min-h-72 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <Search aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('library.supplier')}</EmptyTitle>
            <EmptyDescription>
              {t('library.selectSupplierHint')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}
      {supplierId &&
      referencesQuery.isSuccess &&
      supplierPriceLists.length === 0 ? (
        <Empty className="mt-4 min-h-72 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('library.noPriceListsTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('library.noPriceListsDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}
      {supplierId && priceListId && pricesQuery.isLoading ? (
        <Skeleton className="mt-4 h-96" />
      ) : null}
      {pricesQuery.isError ? (
        <Empty className="mt-4 min-h-72 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('library.errorTitle')}</EmptyTitle>
            <EmptyDescription>{t('library.errorDescription')}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}
      {supplierId &&
      priceListId &&
      !pricesQuery.isLoading &&
      !pricesQuery.isError &&
      entries.length === 0 ? (
        <Empty className="mt-4 min-h-72 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <Search aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('library.emptyTitle')}</EmptyTitle>
            <EmptyDescription>{t('library.emptyDescription')}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}

      {entries.length > 0 ? (
        <>
          <div className="border-border mt-4 hidden overflow-hidden border md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('library.itemName')}</TableHead>
                  <TableHead>{t('library.price')}</TableHead>
                  <TableHead>{t('library.vatRate')}</TableHead>
                  <TableHead>{t('library.minimumOrderQuantity')}</TableHead>
                  <TableHead>{t('library.leadTimeDays')}</TableHead>
                  <TableHead>{t('library.status')}</TableHead>
                  <TableHead className="text-right">
                    {t('library.actions')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {entries.map((entry) => (
                  <TableRow
                    key={entry.priceMappingId ?? entry.vppId}
                    className={entry.isDeleted ? 'opacity-60' : undefined}
                  >
                    <TableCell>
                      <p className="font-medium">{entry.vppName}</p>
                      <p className="text-muted-foreground mt-0.5 text-xs">
                        {entry.vppCode} · {entry.uomName || '—'}
                        {entry.supplierSku ? ` · ${entry.supplierSku}` : ''}
                      </p>
                    </TableCell>
                    <TableCell>
                      <p className="font-medium tabular-nums">
                        {money(entry.netPrice ?? entry.price)}
                      </p>
                      {entry.isDefault ? (
                        <p className="mt-0.5 flex items-center gap-1 text-xs text-amber-600">
                          <Star
                            className="size-3 fill-current"
                            aria-hidden="true"
                          />
                          {t('library.default')}
                        </p>
                      ) : null}
                    </TableCell>
                    <TableCell className="tabular-nums">
                      {entry.vatRate ?? 0}%
                    </TableCell>
                    <TableCell className="tabular-nums">
                      {entry.minimumOrderQuantity ?? 0}
                    </TableCell>
                    <TableCell className="tabular-nums">
                      {entry.leadTimeDays ?? 0}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="rounded-[4px]">
                        {entry.isDeleted
                          ? t('library.archived')
                          : t('library.active')}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-1">
                        {canManage ? (
                          <>
                            <Button
                              variant="ghost"
                              size="icon"
                              disabled={!draftSelected || entry.isDeleted}
                              onClick={() => openEditor(entry)}
                              aria-label={t('library.edit')}
                            >
                              <Pencil aria-hidden="true" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              disabled={
                                !draftSelected ||
                                entry.isDeleted ||
                                entry.isDefault
                              }
                              onClick={() => defaultMutation.mutate(entry)}
                              aria-label={t('library.setDefault')}
                            >
                              <Star aria-hidden="true" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              disabled={!draftSelected}
                              onClick={() => setArchiveTarget(entry)}
                              aria-label={
                                entry.isDeleted
                                  ? t('library.restore')
                                  : t('library.archive')
                              }
                            >
                              {entry.isDeleted ? (
                                <ArchiveRestore aria-hidden="true" />
                              ) : (
                                <Archive aria-hidden="true" />
                              )}
                            </Button>
                          </>
                        ) : null}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          <div className="mt-4 space-y-3 md:hidden">
            {entries.map((entry) => (
              <article
                key={entry.priceMappingId ?? entry.vppId}
                className="border-border border p-4"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <h2 className="font-semibold">{entry.vppName}</h2>
                    <p className="text-muted-foreground mt-1 text-xs">
                      {entry.vppCode} · {entry.uomName}
                    </p>
                  </div>
                  <Badge variant="outline" className="rounded-[4px]">
                    {entry.isDeleted
                      ? t('library.archived')
                      : t('library.active')}
                  </Badge>
                </div>
                <dl className="mt-4 grid grid-cols-3 gap-3 text-sm">
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.netPrice')}
                    </dt>
                    <dd className="mt-1 font-semibold tabular-nums">
                      {money(entry.netPrice ?? entry.price)}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.vatRate')}
                    </dt>
                    <dd className="mt-1 tabular-nums">{entry.vatRate ?? 0}%</dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.leadTimeDays')}
                    </dt>
                    <dd className="mt-1 tabular-nums">
                      {entry.leadTimeDays ?? 0}
                    </dd>
                  </div>
                </dl>
                {canManage ? (
                  <div className="border-border mt-4 flex justify-end gap-2 border-t pt-3">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={!draftSelected || entry.isDeleted}
                      onClick={() => openEditor(entry)}
                    >
                      <Pencil aria-hidden="true" />
                      {t('library.edit')}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={!draftSelected}
                      onClick={() => setArchiveTarget(entry)}
                    >
                      {entry.isDeleted ? (
                        <ArchiveRestore aria-hidden="true" />
                      ) : (
                        <Archive aria-hidden="true" />
                      )}
                      {entry.isDeleted
                        ? t('library.restore')
                        : t('library.archive')}
                    </Button>
                  </div>
                ) : null}
              </article>
            ))}
          </div>
          <div className="mt-4 flex items-center justify-between gap-4">
            <p className="text-muted-foreground text-xs">
              {t('library.pageSummary', { page: page + 1, pageCount, total })}
            </p>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="icon"
                disabled={page === 0}
                onClick={() => setPage((value) => Math.max(0, value - 1))}
                aria-label={t('actions.previous')}
              >
                <ChevronLeft aria-hidden="true" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                disabled={page + 1 >= pageCount}
                onClick={() => setPage((value) => value + 1)}
                aria-label={t('actions.next')}
              >
                <ChevronRight aria-hidden="true" />
              </Button>
            </div>
          </div>
        </>
      ) : null}

      <Sheet open={editorOpen} onOpenChange={setEditorOpen}>
        <SheetContent className="w-full overflow-y-auto sm:max-w-lg">
          <SheetHeader>
            <SheetTitle>
              {t(
                editing ? 'library.priceEditTitle' : 'library.priceCreateTitle',
              )}
            </SheetTitle>
            <SheetDescription>
              {t('library.priceFormDescription')}
            </SheetDescription>
          </SheetHeader>
          <div className="grid gap-5 px-4 py-6 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="mapping-item">{t('library.itemName')} *</Label>
              <Select
                value={form.vppItemId}
                onValueChange={(value: string) =>
                  setForm((current) => ({ ...current, vppItemId: value }))
                }
                disabled={Boolean(editing)}
              >
                <SelectTrigger id="mapping-item" className="w-full">
                  <SelectValue placeholder={t('library.itemPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {(referencesQuery.data?.items ?? []).map(
                    (item: VppItemResDto) =>
                      item.id ? (
                        <SelectItem key={item.id} value={item.id}>
                          {item.vppName} · {item.vppCode}
                        </SelectItem>
                      ) : null,
                  )}
                </SelectContent>
              </Select>
            </div>
            {(
              [
                'price',
                'netPrice',
                'vatRate',
                'minimumOrderQuantity',
                'leadTimeDays',
              ] as const
            ).map((key) => (
              <div key={key} className="space-y-2">
                <Label htmlFor={`mapping-${key}`}>{t(`library.${key}`)}</Label>
                <Input
                  id={`mapping-${key}`}
                  type="number"
                  min="0"
                  step={key === 'price' || key === 'netPrice' ? '100' : '1'}
                  value={form[key]}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      [key]: event.target.value,
                    }))
                  }
                />
              </div>
            ))}
            <div className="space-y-2">
              <Label htmlFor="mapping-sku">{t('library.supplierSku')}</Label>
              <Input
                id="mapping-sku"
                value={form.supplierSku}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    supplierSku: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="mapping-description">
                {t('library.descriptionLabel')}
              </Label>
              <Textarea
                id="mapping-description"
                value={form.description}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    description: event.target.value,
                  }))
                }
              />
            </div>
            <div className="flex items-center gap-2 sm:col-span-2">
              <Switch
                id="mapping-default"
                checked={form.isDefault}
                onCheckedChange={(checked: boolean) =>
                  setForm((current) => ({ ...current, isDefault: checked }))
                }
              />
              <Label htmlFor="mapping-default" className="font-normal">
                {t('library.setDefault')}
              </Label>
            </div>
          </div>
          <SheetFooter>
            <Button variant="outline" onClick={() => setEditorOpen(false)}>
              {t('actions.cancel')}
            </Button>
            <Button
              disabled={!formValid || !draftSelected || saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              {t('library.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <Dialog
        open={Boolean(archiveTarget)}
        onOpenChange={(open: boolean) => !open && setArchiveTarget(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {t(
                archiveTarget?.isDeleted
                  ? 'library.restoreTitle'
                  : 'library.archiveTitle',
              )}
            </DialogTitle>
            <DialogDescription>
              {t(
                archiveTarget?.isDeleted
                  ? 'library.restoreDescription'
                  : 'library.archiveDescription',
                { name: archiveTarget?.vppName },
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setArchiveTarget(null)}>
              {t('actions.cancel')}
            </Button>
            <Button
              variant={archiveTarget?.isDeleted ? 'default' : 'destructive'}
              disabled={archiveMutation.isPending}
              onClick={() =>
                archiveTarget && archiveMutation.mutate(archiveTarget)
              }
            >
              {archiveTarget?.isDeleted
                ? t('library.restore')
                : t('library.archive')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </m.div>
  )
}
