import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  ArchiveRestore,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  PackagePlus,
  Pencil,
  RefreshCw,
  Search,
  ShieldX,
} from 'lucide-react'
import { m } from 'motion/react'
import { useDeferredValue, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiCatalogItems,
  getApiLibraryByTableCode,
  patchApiCatalogItemsByIdStatus,
  postApiCatalogItems,
  putApiCatalogItemsById,
  type LookupCategoryResDto,
  type LookupValueResDto,
  type VppCategoryResDto,
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
import { BusinessDataLocalizationEditor } from '@/features/library/business-data-localization-editor'
import { surfaceEnter } from '@/lib/motion'

type ItemForm = {
  vppCode: string
  vppName: string
  vppCategoryId: string
  uomId: string
  description: string
}

const emptyForm: ItemForm = {
  vppCode: '',
  vppName: '',
  vppCategoryId: '',
  uomId: '',
  description: '',
}

const pageSize = 20

function formatMoney(value?: number | null) {
  if (value == null) return null
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

export function LibraryItemsPage() {
  const { t, i18n } = useTranslation()
  const { hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const canView = hasPermission(permissions.libraryView)
  const canManage = hasPermission(permissions.libraryManage)
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search.trim())
  const [categoryId, setCategoryId] = useState('all')
  const [showDeleted, setShowDeleted] = useState(false)
  const [page, setPage] = useState(0)
  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<VppItemResDto | null>(null)
  const [form, setForm] = useState<ItemForm>(emptyForm)
  const [confirmItem, setConfirmItem] = useState<VppItemResDto | null>(null)

  useEffect(() => setPage(0), [deferredSearch, categoryId, showDeleted])

  const referencesQuery = useQuery({
    queryKey: ['library', 'item-references', i18n.resolvedLanguage],
    enabled: canView,
    queryFn: async () => {
      const [categoryResult, lookupCategoryResult] = await Promise.all([
        getApiLibraryByTableCode({
          path: { tableCode: 'vpp-categories' },
          query: { top: 500, orderby: 'VppCategoryName asc' },
        }),
        getApiLibraryByTableCode({
          path: { tableCode: 'lookup-categories' },
          query: { top: 500, orderby: 'Name asc' },
        }),
      ])
      if (!categoryResult.data) {
        throw toApiRequestError(categoryResult.error, categoryResult.response)
      }
      if (!lookupCategoryResult.data) {
        throw toApiRequestError(
          lookupCategoryResult.error,
          lookupCategoryResult.response,
        )
      }
      const lookupCategories =
        lookupCategoryResult.data as LookupCategoryResDto[]
      const uomCategory = lookupCategories.find(
        (entry) => entry.code?.toLowerCase() === 'uom',
      )
      let uoms: LookupValueResDto[] = []
      if (uomCategory?.id) {
        const uomResult = await getApiLibraryByTableCode({
          path: { tableCode: 'lookup-values' },
          query: {
            lookupCategoryId: uomCategory.id,
            top: 500,
            orderby: 'Sort asc, Value asc',
          },
        })
        if (!uomResult.data) {
          throw toApiRequestError(uomResult.error, uomResult.response)
        }
        uoms = uomResult.data as LookupValueResDto[]
      }
      return {
        categories: categoryResult.data as VppCategoryResDto[],
        uoms,
      }
    },
  })

  const itemsQuery = useQuery({
    queryKey: [
      'library',
      'items',
      i18n.resolvedLanguage,
      categoryId,
      deferredSearch,
      showDeleted,
      page,
    ],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiCatalogItems({
        query: {
          categoryId: categoryId === 'all' ? undefined : categoryId,
          search: deferredSearch || undefined,
          skip: page * pageSize,
          top: pageSize,
          orderby: 'VppName asc',
          showDeleted,
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

  const openEditor = (item?: VppItemResDto) => {
    setEditing(item ?? null)
    setForm(
      item
        ? {
            vppCode: item.vppCode ?? '',
            vppName: item.vppName ?? '',
            vppCategoryId: item.vppCategoryId ?? '',
            uomId: item.uomId ?? '',
            description: item.description ?? '',
          }
        : emptyForm,
    )
    setEditorOpen(true)
  }

  const saveMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        vppCode: form.vppCode.trim(),
        vppName: form.vppName.trim(),
        vppCategoryId: form.vppCategoryId,
        uomId: form.uomId,
        description: form.description.trim() || null,
      }
      const result = editing?.id
        ? await putApiCatalogItemsById({
            path: { id: editing.id },
            body: { ...payload, id: editing.id },
          })
        : await postApiCatalogItems({ body: payload })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setEditorOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['library', 'items'] })
      toast.success(
        t(editing ? 'library.itemUpdateSuccess' : 'library.itemCreateSuccess'),
      )
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const statusMutation = useMutation({
    mutationFn: async (item: VppItemResDto) => {
      if (!item.id) throw new Error('Missing item id')
      const result = await patchApiCatalogItemsByIdStatus({
        path: { id: item.id },
        body: { isDeleted: !item.isDeleted },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setConfirmItem(null)
      await queryClient.invalidateQueries({ queryKey: ['library', 'items'] })
      toast.success(t('library.itemStatusSuccess'))
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

  const items = itemsQuery.data?.items ?? []
  const total = itemsQuery.data?.total ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  const formValid =
    form.vppCode.trim().length > 0 &&
    form.vppName.trim().length > 0 &&
    form.vppCategoryId.length > 0 &&
    form.uomId.length > 0

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
            {t('library.itemsTitle')}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t('library.itemsDescription')}
          </p>
        </div>
        {canManage ? (
          <Button onClick={() => openEditor()}>
            <PackagePlus aria-hidden="true" />
            {t('library.create')}
          </Button>
        ) : null}
      </header>

      <LibraryTabs active="items" />

      <section className="mt-5 grid gap-3 lg:grid-cols-[minmax(16rem,1fr)_16rem_auto] lg:items-end">
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
        <Select value={categoryId} onValueChange={setCategoryId}>
          <SelectTrigger className="w-full" aria-label={t('library.category')}>
            <SelectValue placeholder={t('library.categoryPlaceholder')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('library.tabs.categories')}</SelectItem>
            {(referencesQuery.data?.categories ?? []).map((category) =>
              category.id ? (
                <SelectItem key={category.id} value={category.id}>
                  {category.displayName ||
                    category.vppCategoryName ||
                    category.vppCategoryCode}
                </SelectItem>
              ) : null,
            )}
          </SelectContent>
        </Select>
        <div className="flex items-center justify-between gap-3 lg:justify-end">
          <div className="flex items-center gap-2">
            <Switch
              id="items-show-archived"
              checked={showDeleted}
              onCheckedChange={setShowDeleted}
            />
            <Label
              htmlFor="items-show-archived"
              className="text-sm font-normal"
            >
              {t('library.showArchived')}
            </Label>
          </div>
          <Button
            variant="outline"
            size="icon"
            onClick={() => void itemsQuery.refetch()}
            aria-label={t('actions.refresh')}
          >
            <RefreshCw
              className={itemsQuery.isFetching ? 'animate-spin' : ''}
              aria-hidden="true"
            />
          </Button>
        </div>
      </section>

      {itemsQuery.isLoading ? <Skeleton className="mt-4 h-96" /> : null}
      {itemsQuery.isError ? (
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
      {!itemsQuery.isLoading && !itemsQuery.isError && items.length === 0 ? (
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

      {items.length > 0 ? (
        <>
          <div className="border-border mt-4 hidden overflow-hidden border md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('library.itemName')}</TableHead>
                  <TableHead>{t('library.category')}</TableHead>
                  <TableHead>{t('library.uom')}</TableHead>
                  <TableHead>{t('library.itemCoverage')}</TableHead>
                  <TableHead>{t('library.status')}</TableHead>
                  <TableHead className="text-right">
                    {t('library.actions')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => (
                  <TableRow
                    key={item.id}
                    className={item.isDeleted ? 'opacity-60' : undefined}
                  >
                    <TableCell>
                      <p className="font-medium">
                        {item.displayName || item.vppName}
                      </p>
                      <p className="text-muted-foreground mt-0.5 max-w-80 truncate text-xs">
                        {item.vppCode}
                      </p>
                    </TableCell>
                    <TableCell>
                      {item.vppCategory?.displayName ||
                        item.vppCategoryName ||
                        '—'}
                    </TableCell>
                    <TableCell>
                      {item.uom?.displayName ||
                        item.uomName ||
                        item.uom?.value ||
                        '—'}
                    </TableCell>
                    <TableCell>
                      {item.defaultPrice != null ? (
                        <div>
                          <p className="font-medium tabular-nums">
                            {formatMoney(item.defaultPrice)}
                          </p>
                          <p className="text-muted-foreground text-xs">
                            {item.defaultSupplierName || t('library.default')}
                          </p>
                        </div>
                      ) : (
                        <span className="text-muted-foreground text-sm">
                          {t('library.noDefaultPrice')}
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="rounded-[4px]">
                        {item.isDeleted
                          ? t('library.archived')
                          : t('library.active')}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {canManage ? (
                        <div className="flex justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => openEditor(item)}
                            aria-label={t('library.edit')}
                          >
                            <Pencil aria-hidden="true" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => setConfirmItem(item)}
                            aria-label={
                              item.isDeleted
                                ? t('library.restore')
                                : t('library.archive')
                            }
                          >
                            {item.isDeleted ? (
                              <ArchiveRestore aria-hidden="true" />
                            ) : (
                              <Archive aria-hidden="true" />
                            )}
                          </Button>
                        </div>
                      ) : null}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="mt-4 space-y-3 md:hidden">
            {items.map((item) => (
              <article key={item.id} className="border-border border p-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h2 className="font-semibold">
                      {item.displayName || item.vppName}
                    </h2>
                    <p className="text-muted-foreground mt-1 truncate text-xs">
                      {item.vppCode} ·{' '}
                      {item.uom?.displayName ||
                        item.uomName ||
                        item.uom?.value ||
                        '—'}
                    </p>
                  </div>
                  <Badge variant="outline" className="rounded-[4px]">
                    {item.isDeleted
                      ? t('library.archived')
                      : t('library.active')}
                  </Badge>
                </div>
                <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.category')}
                    </dt>
                    <dd className="mt-1">
                      {item.vppCategory?.displayName ||
                        item.vppCategoryName ||
                        '—'}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.price')}
                    </dt>
                    <dd className="mt-1 font-medium tabular-nums">
                      {formatMoney(item.defaultPrice) || '—'}
                    </dd>
                  </div>
                </dl>
                {canManage ? (
                  <div className="border-border mt-4 flex justify-end gap-2 border-t pt-3">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => openEditor(item)}
                    >
                      <Pencil aria-hidden="true" />
                      {t('library.edit')}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setConfirmItem(item)}
                    >
                      {item.isDeleted ? (
                        <ArchiveRestore aria-hidden="true" />
                      ) : (
                        <Archive aria-hidden="true" />
                      )}
                      {item.isDeleted
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
              {t(editing ? 'library.itemEditTitle' : 'library.itemCreateTitle')}
            </SheetTitle>
            <SheetDescription>
              {t('library.itemFormDescription')}
            </SheetDescription>
          </SheetHeader>
          <div className="space-y-5 px-4 py-6">
            <div className="space-y-2">
              <Label htmlFor="item-code">{t('library.itemCode')} *</Label>
              <Input
                id="item-code"
                value={form.vppCode}
                maxLength={64}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    vppCode: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="item-name">{t('library.itemName')} *</Label>
              <Input
                id="item-name"
                value={form.vppName}
                maxLength={250}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    vppName: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="item-category">{t('library.category')} *</Label>
              <Select
                value={form.vppCategoryId}
                onValueChange={(value: string) =>
                  setForm((current) => ({ ...current, vppCategoryId: value }))
                }
              >
                <SelectTrigger id="item-category" className="w-full">
                  <SelectValue placeholder={t('library.categoryPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {(referencesQuery.data?.categories ?? []).map((category) =>
                    category.id ? (
                      <SelectItem key={category.id} value={category.id}>
                        {category.displayName ||
                          category.vppCategoryName ||
                          category.vppCategoryCode}
                      </SelectItem>
                    ) : null,
                  )}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="item-uom">{t('library.uom')} *</Label>
              <Select
                value={form.uomId}
                onValueChange={(value: string) =>
                  setForm((current) => ({ ...current, uomId: value }))
                }
              >
                <SelectTrigger id="item-uom" className="w-full">
                  <SelectValue placeholder={t('library.uomPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {(referencesQuery.data?.uoms ?? []).map((uom) =>
                    uom.id ? (
                      <SelectItem key={uom.id} value={uom.id}>
                        {uom.displayName || uom.value || uom.code}
                      </SelectItem>
                    ) : null,
                  )}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="item-description">
                {t('library.descriptionLabel')}
              </Label>
              <Textarea
                id="item-description"
                value={form.description}
                maxLength={500}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    description: event.target.value,
                  }))
                }
              />
            </div>
            {editing?.id ? (
              <BusinessDataLocalizationEditor
                entityType="vpp-item"
                entityId={editing.id}
                onChanged={() =>
                  queryClient.invalidateQueries({
                    queryKey: ['library', 'items'],
                  })
                }
              />
            ) : null}
          </div>
          <SheetFooter>
            <Button variant="outline" onClick={() => setEditorOpen(false)}>
              {t('actions.cancel')}
            </Button>
            <Button
              disabled={!formValid || saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              {t('library.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <Dialog
        open={Boolean(confirmItem)}
        onOpenChange={(open: boolean) => !open && setConfirmItem(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {t(
                confirmItem?.isDeleted
                  ? 'library.restoreTitle'
                  : 'library.archiveTitle',
              )}
            </DialogTitle>
            <DialogDescription>
              {t(
                confirmItem?.isDeleted
                  ? 'library.restoreDescription'
                  : 'library.archiveDescription',
                { name: confirmItem?.displayName || confirmItem?.vppName },
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmItem(null)}>
              {t('actions.cancel')}
            </Button>
            <Button
              variant={confirmItem?.isDeleted ? 'default' : 'destructive'}
              disabled={statusMutation.isPending}
              onClick={() => confirmItem && statusMutation.mutate(confirmItem)}
            >
              {confirmItem?.isDeleted
                ? t('library.restore')
                : t('library.archive')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </m.div>
  )
}
