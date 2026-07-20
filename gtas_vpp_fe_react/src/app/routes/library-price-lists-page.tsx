import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  ArchiveRestore,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  Copy,
  MoreHorizontal,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldX,
  Star,
  TimerOff,
} from 'lucide-react'
import { m } from 'motion/react'
import { useDeferredValue, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiLibraryByTableCode,
  getApiVppPriceList,
  patchApiVppPriceListByIdDeleted,
  postApiVppPriceList,
  postApiVppPriceListByIdExpire,
  postApiVppPriceListByIdPublish,
  postApiVppPriceListByIdSetDefault,
  postApiVppPriceListClone,
  putApiVppPriceListById,
  type PriceListResDto,
  type SupplierResDto,
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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

type PriceListForm = {
  code: string
  name: string
  description: string
  supplierId: string
  version: string
  effectiveFromUtc: string
  effectiveToUtc: string
  currencyCode: string
  vatPolicy: string
  contractCode: string
  discountRate: string
  rebateAmount: string
  feeAmount: string
  shippingAmount: string
  isDefault: boolean
}

type StatusAction = 'publish' | 'expire'

const pageSize = 20

function localDateTime(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function toIso(value: string) {
  return value ? new Date(value).toISOString() : undefined
}

function numberValue(value: string) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : 0
}

function createForm(item?: PriceListResDto): PriceListForm {
  return {
    code: item?.priceListCode ?? '',
    name: item?.priceListName ?? '',
    description: item?.description ?? '',
    supplierId: item?.supplierId ?? '',
    version: String(item?.version ?? 1),
    effectiveFromUtc: localDateTime(
      item?.effectiveFromUtc ?? new Date().toISOString(),
    ),
    effectiveToUtc: localDateTime(item?.effectiveToUtc),
    currencyCode: item?.currencyCode ?? 'VND',
    vatPolicy: item?.vatPolicy ?? 'item-rate',
    contractCode: item?.contractCode ?? '',
    discountRate: String(item?.discountRate ?? 0),
    rebateAmount: String(item?.rebateAmount ?? 0),
    feeAmount: String(item?.feeAmount ?? 0),
    shippingAmount: String(item?.shippingAmount ?? 0),
    isDefault: item?.isDefault ?? false,
  }
}

function statusKey(status?: string | null) {
  if (status?.toLowerCase() === 'published') return 'library.published'
  if (status?.toLowerCase() === 'expired') return 'library.expired'
  return 'library.draft'
}

export function LibraryPriceListsPage() {
  const { t } = useTranslation()
  const { hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const canView = hasPermission(permissions.libraryView)
  const canManage = hasPermission(permissions.libraryManage)
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search.trim())
  const [showDeleted, setShowDeleted] = useState(false)
  const [page, setPage] = useState(0)
  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<PriceListResDto | null>(null)
  const [form, setForm] = useState<PriceListForm>(() => createForm())
  const [archiveTarget, setArchiveTarget] = useState<PriceListResDto | null>(
    null,
  )
  const [cloneTarget, setCloneTarget] = useState<PriceListResDto | null>(null)
  const [cloneCode, setCloneCode] = useState('')
  const [cloneName, setCloneName] = useState('')
  const [statusTarget, setStatusTarget] = useState<PriceListResDto | null>(null)
  const [statusAction, setStatusAction] = useState<StatusAction>('publish')
  const [statusReason, setStatusReason] = useState('')

  useEffect(() => setPage(0), [deferredSearch, showDeleted])

  const suppliersQuery = useQuery({
    queryKey: ['library', 'price-list-suppliers'],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiLibraryByTableCode({
        path: { tableCode: 'suppliers' },
        query: { top: 500, orderby: 'SupplierName asc' },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data as SupplierResDto[]
    },
  })

  const listsQuery = useQuery({
    queryKey: ['library', 'price-lists', deferredSearch, showDeleted, page],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiVppPriceList({
        query: {
          showDeleted,
          search: deferredSearch || undefined,
          skip: page * pageSize,
          top: pageSize,
          orderby: 'IsDefault desc, PriceListName asc',
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

  const refresh = async () => {
    await Promise.all([listsQuery.refetch(), suppliersQuery.refetch()])
  }

  const openEditor = (item?: PriceListResDto) => {
    setEditing(item ?? null)
    setForm(createForm(item))
    setEditorOpen(true)
  }

  const saveMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        code: form.code.trim(),
        name: form.name.trim(),
        description: form.description.trim() || null,
        supplierId: form.supplierId,
        version: numberValue(form.version),
        effectiveFromUtc: toIso(form.effectiveFromUtc),
        effectiveToUtc: toIso(form.effectiveToUtc),
        currencyCode: form.currencyCode.trim().toUpperCase(),
        vatPolicy: form.vatPolicy.trim() || 'item-rate',
        contractCode: form.contractCode.trim() || null,
        discountRate: numberValue(form.discountRate),
        rebateAmount: numberValue(form.rebateAmount),
        feeAmount: numberValue(form.feeAmount),
        shippingAmount: numberValue(form.shippingAmount),
        isDefault: form.isDefault,
      }
      const result = editing?.id
        ? await putApiVppPriceListById({
            path: { id: editing.id },
            body: {
              ...payload,
              id: editing.id,
              rowVersion: editing.rowVersion,
            },
          })
        : await postApiVppPriceList({ body: payload })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setEditorOpen(false)
      await queryClient.invalidateQueries({
        queryKey: ['library', 'price-lists'],
      })
      toast.success(t('library.priceListSaveSuccess'))
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const archiveMutation = useMutation({
    mutationFn: async (item: PriceListResDto) => {
      if (!item.id) throw new Error('Missing price list id')
      const result = await patchApiVppPriceListByIdDeleted({
        path: { id: item.id },
        body: { isDeleted: !item.isDeleted },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setArchiveTarget(null)
      await queryClient.invalidateQueries({
        queryKey: ['library', 'price-lists'],
      })
      toast.success(t('library.statusSuccess'))
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const cloneMutation = useMutation({
    mutationFn: async () => {
      if (!cloneTarget?.id) throw new Error('Missing source price list id')
      const result = await postApiVppPriceListClone({
        body: {
          sourceId: cloneTarget.id,
          code: cloneCode.trim(),
          name: cloneName.trim(),
          description: cloneTarget.description,
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setCloneTarget(null)
      await queryClient.invalidateQueries({
        queryKey: ['library', 'price-lists'],
      })
      toast.success(t('library.priceListCloneSuccess'))
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const statusMutation = useMutation({
    mutationFn: async () => {
      if (!statusTarget?.id) throw new Error('Missing price list id')
      const options = {
        path: { id: statusTarget.id },
        body: {
          rowVersion: statusTarget.rowVersion,
          reason: statusReason.trim(),
          effectiveToUtc:
            statusAction === 'expire' ? new Date().toISOString() : undefined,
        },
      }
      const result =
        statusAction === 'publish'
          ? await postApiVppPriceListByIdPublish(options)
          : await postApiVppPriceListByIdExpire(options)
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      const completedAction = statusAction
      setStatusTarget(null)
      setStatusReason('')
      await queryClient.invalidateQueries({
        queryKey: ['library', 'price-lists'],
      })
      toast.success(
        t(
          completedAction === 'publish'
            ? 'library.priceListPublishSuccess'
            : 'library.priceListExpireSuccess',
        ),
      )
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const defaultMutation = useMutation({
    mutationFn: async (item: PriceListResDto) => {
      if (!item.id) throw new Error('Missing price list id')
      const result = await postApiVppPriceListByIdSetDefault({
        path: { id: item.id },
      })
      if (result.error) throw toApiRequestError(result.error, result.response)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['library', 'price-lists'],
      })
      toast.success(t('library.priceListDefaultSuccess'))
    },
    onError: () => toast.error(t('library.genericMutationError')),
  })

  const openClone = (item: PriceListResDto) => {
    setCloneTarget(item)
    setCloneCode(`${item.priceListCode ?? 'PRICE'}-COPY`)
    setCloneName(`${item.priceListName ?? ''} - Copy`)
  }

  const openStatus = (item: PriceListResDto, action: StatusAction) => {
    setStatusTarget(item)
    setStatusAction(action)
    setStatusReason('')
  }

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

  const items = listsQuery.data?.items ?? []
  const total = listsQuery.data?.total ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  const formValid =
    form.code.trim().length > 0 &&
    form.name.trim().length > 0 &&
    form.supplierId.length > 0 &&
    numberValue(form.version) > 0 &&
    form.currencyCode.trim().length === 3 &&
    form.effectiveFromUtc.length > 0

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
            {t('library.priceListsTitle')}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t('library.priceListsDescription')}
          </p>
        </div>
        {canManage ? (
          <Button onClick={() => openEditor()}>
            <Plus aria-hidden="true" />
            {t('library.create')}
          </Button>
        ) : null}
      </header>

      <LibraryTabs active="price-lists" />

      <section className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="relative w-full sm:max-w-md">
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
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <Switch
              id="price-lists-show-archived"
              checked={showDeleted}
              onCheckedChange={setShowDeleted}
            />
            <Label
              htmlFor="price-lists-show-archived"
              className="text-sm font-normal"
            >
              {t('library.showArchived')}
            </Label>
          </div>
          <Button
            variant="outline"
            size="icon"
            onClick={() => void refresh()}
            aria-label={t('actions.refresh')}
          >
            <RefreshCw
              className={listsQuery.isFetching ? 'animate-spin' : ''}
              aria-hidden="true"
            />
          </Button>
        </div>
      </section>

      {listsQuery.isLoading ? <Skeleton className="mt-4 h-96" /> : null}
      {listsQuery.isError ? (
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
      {!listsQuery.isLoading && !listsQuery.isError && items.length === 0 ? (
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
          <div className="border-border mt-4 hidden overflow-hidden border lg:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('library.priceListName')}</TableHead>
                  <TableHead>{t('library.supplier')}</TableHead>
                  <TableHead>{t('library.effectiveFrom')}</TableHead>
                  <TableHead>{t('library.status')}</TableHead>
                  <TableHead>{t('library.priceEntries')}</TableHead>
                  <TableHead className="text-right">
                    {t('library.actions')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => {
                  const draft = item.status?.toLowerCase() === 'draft'
                  const published = item.status?.toLowerCase() === 'published'
                  return (
                    <TableRow
                      key={item.id}
                      className={item.isDeleted ? 'opacity-60' : undefined}
                    >
                      <TableCell>
                        <div className="flex items-start gap-2">
                          <div className="min-w-0">
                            <p className="font-medium">{item.priceListName}</p>
                            <p className="text-muted-foreground mt-0.5 truncate text-xs">
                              {item.priceListCode} · v{item.version}
                            </p>
                          </div>
                          {item.isDefault ? (
                            <Star
                              className="size-4 fill-current text-amber-500"
                              aria-label={t('library.default')}
                            />
                          ) : null}
                        </div>
                      </TableCell>
                      <TableCell>{item.supplierName || '—'}</TableCell>
                      <TableCell>
                        <p>
                          {item.effectiveFromUtc
                            ? new Date(
                                item.effectiveFromUtc,
                              ).toLocaleDateString()
                            : '—'}
                        </p>
                        <p className="text-muted-foreground text-xs">
                          {item.effectiveToUtc
                            ? new Date(item.effectiveToUtc).toLocaleDateString()
                            : '∞'}
                        </p>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          <Badge variant="outline" className="rounded-[4px]">
                            {t(statusKey(item.status))}
                          </Badge>
                          {item.isDeleted ? (
                            <Badge variant="outline" className="rounded-[4px]">
                              {t('library.archived')}
                            </Badge>
                          ) : null}
                        </div>
                      </TableCell>
                      <TableCell className="tabular-nums">
                        {item.itemCount ?? 0}
                      </TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-1">
                          <Button asChild variant="ghost" size="sm">
                            <Link
                              to={`/app/library/prices?supplierId=${item.supplierId ?? ''}&priceListId=${item.id ?? ''}`}
                              viewTransition
                            >
                              {t('library.managePrices')}
                            </Link>
                          </Button>
                          {canManage ? (
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  aria-label={t('library.actions')}
                                >
                                  <MoreHorizontal aria-hidden="true" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                <DropdownMenuItem
                                  disabled={!draft || item.isDeleted}
                                  onSelect={() => openEditor(item)}
                                >
                                  <Pencil aria-hidden="true" />
                                  {t('library.edit')}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  onSelect={() => openClone(item)}
                                >
                                  <Copy aria-hidden="true" />
                                  {t('library.clone')}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  disabled={
                                    !draft ||
                                    (item.itemCount ?? 0) === 0 ||
                                    item.isDeleted
                                  }
                                  onSelect={() => openStatus(item, 'publish')}
                                >
                                  <CheckCircle2 aria-hidden="true" />
                                  {t('library.publish')}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  disabled={!published || item.isDeleted}
                                  onSelect={() => openStatus(item, 'expire')}
                                >
                                  <TimerOff aria-hidden="true" />
                                  {t('library.expire')}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  disabled={item.isDefault || item.isDeleted}
                                  onSelect={() => defaultMutation.mutate(item)}
                                >
                                  <Star aria-hidden="true" />
                                  {t('library.setDefault')}
                                </DropdownMenuItem>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem
                                  variant={
                                    item.isDeleted ? 'default' : 'destructive'
                                  }
                                  onSelect={() => setArchiveTarget(item)}
                                >
                                  {item.isDeleted ? (
                                    <ArchiveRestore aria-hidden="true" />
                                  ) : (
                                    <Archive aria-hidden="true" />
                                  )}
                                  {item.isDeleted
                                    ? t('library.restore')
                                    : t('library.archive')}
                                </DropdownMenuItem>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          ) : null}
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>

          <div className="mt-4 grid gap-3 lg:hidden">
            {items.map((item) => (
              <article key={item.id} className="border-border border p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <h2 className="font-semibold">{item.priceListName}</h2>
                    <p className="text-muted-foreground mt-1 text-xs">
                      {item.priceListCode} · v{item.version} ·{' '}
                      {item.supplierName}
                    </p>
                  </div>
                  <Badge variant="outline" className="rounded-[4px]">
                    {t(statusKey(item.status))}
                  </Badge>
                </div>
                <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.effectiveFrom')}
                    </dt>
                    <dd className="mt-1">
                      {item.effectiveFromUtc
                        ? new Date(item.effectiveFromUtc).toLocaleDateString()
                        : '—'}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground text-xs">
                      {t('library.itemsCount', { count: item.itemCount ?? 0 })}
                    </dt>
                    <dd className="mt-1 font-semibold tabular-nums">
                      {item.itemCount ?? 0}
                    </dd>
                  </div>
                </dl>
                <div className="border-border mt-4 flex justify-end gap-2 border-t pt-3">
                  <Button asChild variant="outline" size="sm">
                    <Link
                      to={`/app/library/prices?supplierId=${item.supplierId ?? ''}&priceListId=${item.id ?? ''}`}
                    >
                      {t('library.managePrices')}
                    </Link>
                  </Button>
                  {canManage ? (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() =>
                        item.status?.toLowerCase() === 'draft' &&
                        openEditor(item)
                      }
                      disabled={item.status?.toLowerCase() !== 'draft'}
                    >
                      <Pencil aria-hidden="true" />
                      {t('library.edit')}
                    </Button>
                  ) : null}
                </div>
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
        <SheetContent className="w-full overflow-y-auto sm:max-w-xl">
          <SheetHeader>
            <SheetTitle>
              {t(
                editing
                  ? 'library.priceListEditTitle'
                  : 'library.priceListCreateTitle',
              )}
            </SheetTitle>
            <SheetDescription>
              {t('library.priceListFormDescription')}
            </SheetDescription>
          </SheetHeader>
          <div className="grid gap-5 px-4 py-6 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="price-list-code">
                {t('library.priceListCode')} *
              </Label>
              <Input
                id="price-list-code"
                value={form.code}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    code: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="price-list-version">
                {t('library.version')} *
              </Label>
              <Input
                id="price-list-version"
                type="number"
                min="1"
                value={form.version}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    version: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="price-list-name">
                {t('library.priceListName')} *
              </Label>
              <Input
                id="price-list-name"
                value={form.name}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    name: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="price-list-supplier">
                {t('library.supplier')} *
              </Label>
              <Select
                value={form.supplierId}
                onValueChange={(value: string) =>
                  setForm((current) => ({ ...current, supplierId: value }))
                }
              >
                <SelectTrigger id="price-list-supplier" className="w-full">
                  <SelectValue placeholder={t('library.supplierPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {(suppliersQuery.data ?? []).map((supplier) =>
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
              <Label htmlFor="price-list-from">
                {t('library.effectiveFrom')} *
              </Label>
              <Input
                id="price-list-from"
                type="datetime-local"
                value={form.effectiveFromUtc}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    effectiveFromUtc: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="price-list-to">{t('library.effectiveTo')}</Label>
              <Input
                id="price-list-to"
                type="datetime-local"
                value={form.effectiveToUtc}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    effectiveToUtc: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="price-list-currency">
                {t('library.currency')} *
              </Label>
              <Input
                id="price-list-currency"
                maxLength={3}
                value={form.currencyCode}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    currencyCode: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="price-list-vat">{t('library.vatPolicy')}</Label>
              <Input
                id="price-list-vat"
                value={form.vatPolicy}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    vatPolicy: event.target.value,
                  }))
                }
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="price-list-contract">
                {t('library.contractCode')}
              </Label>
              <Input
                id="price-list-contract"
                value={form.contractCode}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    contractCode: event.target.value,
                  }))
                }
              />
            </div>
            {(
              [
                'discountRate',
                'rebateAmount',
                'feeAmount',
                'shippingAmount',
              ] as const
            ).map((key) => (
              <div key={key} className="space-y-2">
                <Label htmlFor={`price-list-${key}`}>
                  {t(`library.${key}`)}
                </Label>
                <Input
                  id={`price-list-${key}`}
                  type="number"
                  min="0"
                  step="0.01"
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
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="price-list-description">
                {t('library.descriptionLabel')}
              </Label>
              <Textarea
                id="price-list-description"
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
                id="price-list-default"
                checked={form.isDefault}
                onCheckedChange={(checked: boolean) =>
                  setForm((current) => ({ ...current, isDefault: checked }))
                }
              />
              <Label htmlFor="price-list-default" className="font-normal">
                {t('library.setDefault')}
              </Label>
            </div>
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
                { name: archiveTarget?.priceListName },
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

      <Dialog
        open={Boolean(cloneTarget)}
        onOpenChange={(open: boolean) => !open && setCloneTarget(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('library.cloneTitle')}</DialogTitle>
            <DialogDescription>
              {t('library.cloneDescription', {
                name: cloneTarget?.priceListName,
              })}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="clone-code">{t('library.cloneCode')}</Label>
              <Input
                id="clone-code"
                value={cloneCode}
                onChange={(event) => setCloneCode(event.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="clone-name">{t('library.cloneName')}</Label>
              <Input
                id="clone-name"
                value={cloneName}
                onChange={(event) => setCloneName(event.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCloneTarget(null)}>
              {t('actions.cancel')}
            </Button>
            <Button
              disabled={
                cloneCode.trim().length === 0 ||
                cloneName.trim().length === 0 ||
                cloneMutation.isPending
              }
              onClick={() => cloneMutation.mutate()}
            >
              {t('library.clone')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={Boolean(statusTarget)}
        onOpenChange={(open: boolean) => !open && setStatusTarget(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {t(
                statusAction === 'publish'
                  ? 'library.publishTitle'
                  : 'library.expireTitle',
              )}
            </DialogTitle>
            <DialogDescription>
              {t(
                statusAction === 'publish'
                  ? 'library.publishDescription'
                  : 'library.expireDescription',
              )}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="status-reason">{t('library.statusReason')}</Label>
            <Textarea
              id="status-reason"
              value={statusReason}
              maxLength={500}
              placeholder={t('library.statusReasonPlaceholder')}
              onChange={(event) => setStatusReason(event.target.value)}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setStatusTarget(null)}>
              {t('actions.cancel')}
            </Button>
            <Button
              disabled={
                statusReason.trim().length < 5 || statusMutation.isPending
              }
              onClick={() => statusMutation.mutate()}
            >
              {t(
                statusAction === 'publish'
                  ? 'library.publish'
                  : 'library.expire',
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </m.div>
  )
}
