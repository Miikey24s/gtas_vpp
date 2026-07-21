import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  CalendarDays,
  Check,
  CircleAlert,
  ClipboardCopy,
  Clock3,
  Minus,
  PackageOpen,
  Plus,
  Search,
  Send,
  ShieldX,
  Trash2,
} from 'lucide-react'
import { useDeferredValue, useEffect, useMemo, useRef, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import {
  Link,
  useBlocker,
  useNavigate,
  useSearchParams,
} from 'react-router-dom'
import { toast } from 'sonner'
import { z } from 'zod'

import { isApiRequestError, toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestOrdersPreviousItems,
  getApiVppRequestPeriodInfo,
  getApiVppRequestProductsLookup,
  postApiVppRequestOrders,
  type VppItemResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
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
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import {
  previousItemToDraftLine,
  productToDraftLine,
  readOrderDraft,
  removeOrderDraft,
  type OrderDraftLine,
  writeOrderDraft,
} from '@/features/orders/order-draft'
import { formatDate, formatPeriod } from '@/features/orders/order-format'

type OrderMode = 'regular' | 'additional'
type FormValues = { description: string; supplementReason: string }

export function ProductRow({
  product,
  selected,
  onAdd,
}: {
  product: VppItemResDto
  selected: boolean
  onAdd: () => void
}) {
  const { t } = useTranslation()

  return (
    <article className="border-border flex items-center gap-3 border-b px-4 py-3 last:border-b-0">
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <h3 className="truncate text-sm font-medium">
            {product.displayName || product.vppName || '—'}
          </h3>
          <span className="text-muted-foreground text-xs">
            {product.uomName || product.uomCode || '—'}
          </span>
        </div>
        <p className="text-muted-foreground mt-1 truncate text-xs">
          {product.vppCode} · {product.vppCategoryName || '—'}
        </p>
      </div>
      <Button
        type="button"
        variant={selected ? 'secondary' : 'outline'}
        size="sm"
        onClick={onAdd}
        disabled={selected || !product.id}
      >
        {selected ? <Check aria-hidden="true" /> : <Plus aria-hidden="true" />}
        {t(selected ? 'orderCreate.added' : 'orderCreate.add')}
      </Button>
    </article>
  )
}

export function SelectedLine({
  line,
  invalid,
  onChange,
  onRemove,
}: {
  line: OrderDraftLine
  invalid: boolean
  onChange: (line: OrderDraftLine) => void
  onRemove: () => void
}) {
  const { t } = useTranslation()

  return (
    <article className="border-border border-b px-4 py-4 last:border-b-0">
      <div className="flex items-start gap-3">
        <div className="min-w-0 flex-1">
          <h3 className="truncate text-sm font-medium">{line.vppName}</h3>
          <p className="text-muted-foreground mt-1 text-xs">
            {line.vppCode} · {line.uomName || '—'}
          </p>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={onRemove}
          aria-label={t('orderCreate.removeItem', { item: line.vppName })}
        >
          <Trash2 aria-hidden="true" />
        </Button>
      </div>

      <div className="mt-3 grid gap-3 sm:grid-cols-[9rem_minmax(0,1fr)]">
        <label>
          <span className="mb-1.5 block text-xs font-medium">
            {t('orderCreate.quantity')}
          </span>
          <div className="grid grid-cols-[2rem_minmax(0,1fr)_2rem]">
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="rounded-r-none"
              onClick={() =>
                onChange({ ...line, qty: Math.max(1, line.qty - 1) })
              }
              aria-label={t('orderCreate.decreaseQuantity')}
            >
              <Minus aria-hidden="true" />
            </Button>
            <Input
              type="number"
              inputMode="numeric"
              min={1}
              step={1}
              value={Number.isFinite(line.qty) ? line.qty : ''}
              onChange={(event) =>
                onChange({ ...line, qty: Number(event.target.value) })
              }
              aria-invalid={invalid}
              className="rounded-none border-x-0 text-center tabular-nums"
              aria-label={t('orderCreate.quantityFor', {
                item: line.vppName,
              })}
            />
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="rounded-l-none"
              onClick={() => onChange({ ...line, qty: line.qty + 1 })}
              aria-label={t('orderCreate.increaseQuantity')}
            >
              <Plus aria-hidden="true" />
            </Button>
          </div>
          <span className="text-destructive mt-1 block min-h-4 text-xs">
            {invalid ? t('orderCreate.quantityError') : ''}
          </span>
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-medium">
            {t('orderCreate.itemNote')}
          </span>
          <Input
            value={line.description}
            maxLength={500}
            onChange={(event) =>
              onChange({ ...line, description: event.target.value })
            }
            placeholder={t('orderCreate.itemNotePlaceholder')}
          />
        </label>
      </div>
    </article>
  )
}

export function OrderCreateWorkspace() {
  const { t, i18n } = useTranslation()
  const { user, hasPermission } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedMode: OrderMode =
    searchParams.get('type') === 'additional' ? 'additional' : 'regular'
  const [mode, setMode] = useState<OrderMode>(requestedMode)
  const [searchText, setSearchText] = useState('')
  const deferredSearch = useDeferredValue(searchText.trim())
  const [lines, setLines] = useState<OrderDraftLine[]>([])
  const [draftReady, setDraftReady] = useState(false)
  const [submitAttempted, setSubmitAttempted] = useState(false)
  const [copyDialogOpen, setCopyDialogOpen] = useState(false)
  const idempotency = useRef({ signature: '', key: '' })
  const allowNavigation = useRef(false)
  const language = i18n.resolvedLanguage ?? 'vi'
  const canCreate = hasPermission(permissions.requestCreate)
  const canUseCatalog = hasPermission(permissions.requestCatalogView)

  const schema = useMemo(
    () =>
      z.object({
        description: z.string().trim().max(500, t('orderCreate.noteTooLong')),
        supplementReason:
          mode === 'additional'
            ? z
                .string()
                .trim()
                .min(5, t('orderCreate.reasonTooShort'))
                .max(500, t('orderCreate.reasonTooLong'))
            : z.string().trim().max(500),
      }),
    [mode, t],
  )

  const {
    control,
    register,
    reset,
    handleSubmit,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { description: '', supplementReason: '' },
    mode: 'onBlur',
  })
  const formValues = useWatch({ control })

  const periodQuery = useQuery({
    queryKey: ['vpp', 'period-info', language],
    enabled: canCreate && canUseCatalog,
    queryFn: async () => {
      const result = await getApiVppRequestPeriodInfo()
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })
  const period = periodQuery.data
  const regularAllowed = period?.canCreateOrder === true
  const additionalAllowed = period?.canCreateAdditional === true

  useEffect(() => {
    if (!period) return
    const nextMode =
      requestedMode === 'additional' && additionalAllowed
        ? 'additional'
        : regularAllowed
          ? 'regular'
          : additionalAllowed
            ? 'additional'
            : requestedMode
    setMode(nextMode)
    if (nextMode !== requestedMode) {
      setSearchParams(nextMode === 'additional' ? { type: 'additional' } : {}, {
        replace: true,
      })
    }
  }, [
    additionalAllowed,
    period,
    regularAllowed,
    requestedMode,
    setSearchParams,
  ])

  const draftKey = period
    ? `gtas-vpp:order-draft:v1:${user?.userId ?? 'unknown'}:${period.currentPeriodYear}-${period.currentPeriodMonth}:${mode}`
    : null

  useEffect(() => {
    if (!draftKey) return
    setDraftReady(false)
    const draft = readOrderDraft(draftKey)
    reset({
      description: draft?.description ?? '',
      supplementReason: draft?.supplementReason ?? '',
    })
    setLines(draft?.lines ?? [])
    setSubmitAttempted(false)
    setDraftReady(true)
  }, [draftKey, reset])

  useEffect(() => {
    if (!draftKey || !draftReady) return
    writeOrderDraft(draftKey, {
      description: formValues.description ?? '',
      supplementReason: formValues.supplementReason ?? '',
      lines,
    })
  }, [draftKey, draftReady, formValues, lines])

  const productsQuery = useQuery({
    queryKey: ['vpp', 'products', 'lookup', language, deferredSearch],
    enabled:
      canCreate &&
      canUseCatalog &&
      Boolean(period) &&
      (regularAllowed || additionalAllowed),
    queryFn: async () => {
      const result = await getApiVppRequestProductsLookup({
        query: { search: deferredSearch || undefined, top: 40 },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const previousMutation = useMutation({
    mutationFn: async () => {
      const result = await getApiVppRequestOrdersPreviousItems()
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: (order) => {
      const copied = (order.items ?? [])
        .map(previousItemToDraftLine)
        .filter((line) => line.vppId)
      setLines(copied)
      setCopyDialogOpen(false)
      toast.success(t('orderCreate.copySuccess', { count: copied.length }))
    },
    onError: () => toast.error(t('orderCreate.copyError')),
  })

  const selectedIds = new Set(lines.map((line) => line.vppId))
  const invalidLines = lines.filter(
    (line) => !Number.isInteger(line.qty) || line.qty <= 0,
  )
  const totalQuantity = lines.reduce(
    (total, line) => total + (Number.isFinite(line.qty) ? line.qty : 0),
    0,
  )
  const modeAllowed = mode === 'regular' ? regularAllowed : additionalAllowed
  const hasUnsavedChanges = isDirty || lines.length > 0
  const blocker = useBlocker(
    () => !allowNavigation.current && hasUnsavedChanges && !isSubmitting,
  )

  useEffect(() => {
    const beforeUnload = (event: BeforeUnloadEvent) => {
      if (!hasUnsavedChanges || isSubmitting) return
      event.preventDefault()
    }
    window.addEventListener('beforeunload', beforeUnload)
    return () => window.removeEventListener('beforeunload', beforeUnload)
  }, [hasUnsavedChanges, isSubmitting])

  const selectMode = (nextMode: OrderMode) => {
    setMode(nextMode)
    setSearchParams(nextMode === 'additional' ? { type: 'additional' } : {})
  }

  const addProduct = (product: VppItemResDto) => {
    const line = productToDraftLine(product)
    if (!line.vppId || selectedIds.has(line.vppId)) return
    setLines((current) => [...current, line])
  }

  const updateLine = (nextLine: OrderDraftLine) => {
    setLines((current) =>
      current.map((line) => (line.vppId === nextLine.vppId ? nextLine : line)),
    )
  }

  const submit = handleSubmit(async (values) => {
    setSubmitAttempted(true)
    if (!period || !modeAllowed || lines.length === 0 || invalidLines.length) {
      return
    }

    const body = {
      year: period.currentPeriodYear,
      month: period.currentPeriodMonth,
      description:
        mode === 'regular' ? values.description.trim() || null : null,
      isAdditionalOrder: mode === 'additional',
      baseRequestId: mode === 'additional' ? period.baseRequestId : null,
      supplementReason:
        mode === 'additional' ? values.supplementReason.trim() : null,
      items: lines.map((line) => ({
        vppId: line.vppId,
        qty: line.qty,
        description: line.description.trim() || null,
      })),
    }
    const signature = JSON.stringify(body)
    if (idempotency.current.signature !== signature) {
      idempotency.current = { signature, key: crypto.randomUUID() }
    }

    try {
      const result = await postApiVppRequestOrders({
        body: { ...body, idempotencyKey: idempotency.current.key },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      if (draftKey) removeOrderDraft(draftKey)
      await queryClient.invalidateQueries({ queryKey: ['vpp'] })
      toast.success(
        t(
          mode === 'additional'
            ? 'orderCreate.additionalSuccess'
            : 'orderCreate.regularSuccess',
        ),
      )
      allowNavigation.current = true
      navigate('/app/orders', { replace: true, viewTransition: true })
    } catch (error) {
      if (isApiRequestError(error) && error.status === 409) {
        toast.error(t('orderCreate.conflictError'))
      } else if (isApiRequestError(error) && error.status === 400) {
        toast.error(t('orderCreate.businessError'))
      } else {
        toast.error(t('orderCreate.submitError'))
      }
    }
  })

  if (!canCreate || !canUseCatalog) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orderCreate.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderCreate.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  if (periodQuery.isLoading) {
    return (
      <div className="mx-auto max-w-[110rem] space-y-5 px-4 py-6 sm:px-6 lg:px-8">
        <Skeleton className="h-28 w-full" />
        <div className="grid gap-5 lg:grid-cols-2">
          <Skeleton className="h-[32rem]" />
          <Skeleton className="h-[32rem]" />
        </div>
      </div>
    )
  }

  if (periodQuery.isError || !period) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orderCreate.periodErrorTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderCreate.periodErrorDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <header className="border-border border-b pb-5">
        <Link
          to="/app/orders"
          viewTransition
          className="text-muted-foreground hover:text-foreground inline-flex items-center gap-2 text-sm transition-colors"
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          {t('orderCreate.back')}
        </Link>
        <div className="mt-4 flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-semibold tracking-[-0.035em] sm:text-3xl">
                {t('orderCreate.title')}
              </h1>
              <Badge variant="outline" className="rounded-[4px]">
                <CalendarDays aria-hidden="true" />
                {formatPeriod(
                  period.currentPeriodMonth,
                  period.currentPeriodYear,
                )}
              </Badge>
            </div>
            <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
              {t('orderCreate.description')}
            </p>
          </div>
          <div className="text-muted-foreground flex items-center gap-2 text-sm">
            <Clock3 className="size-4" aria-hidden="true" />
            {t('orderCreate.deadline', {
              value: formatDate(period.deadlineDate, language),
            })}
          </div>
        </div>
      </header>

      <section className="mt-5" aria-labelledby="order-mode-title">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 id="order-mode-title" className="text-sm font-semibold">
              {t('orderCreate.modeTitle')}
            </h2>
            <p className="text-muted-foreground mt-1 text-xs">
              {t('orderCreate.modeDescription')}
            </p>
          </div>
          <div className="bg-muted/60 grid grid-cols-2 gap-1 p-1" role="group">
            <Button
              type="button"
              size="sm"
              variant={mode === 'regular' ? 'secondary' : 'ghost'}
              onClick={() => selectMode('regular')}
              disabled={!regularAllowed}
              aria-pressed={mode === 'regular'}
            >
              {t('orderCreate.regular')}
            </Button>
            <Button
              type="button"
              size="sm"
              variant={mode === 'additional' ? 'secondary' : 'ghost'}
              onClick={() => selectMode('additional')}
              disabled={!additionalAllowed}
              aria-pressed={mode === 'additional'}
            >
              {t('orderCreate.additional')}
              {period.remainingSupplementAttempts != null ? (
                <span className="text-muted-foreground tabular-nums">
                  {period.remainingSupplementAttempts}
                </span>
              ) : null}
            </Button>
          </div>
        </div>

        {!regularAllowed && !additionalAllowed ? (
          <Alert className="mt-4">
            <CircleAlert aria-hidden="true" />
            <AlertTitle>{t('orderCreate.unavailableTitle')}</AlertTitle>
            <AlertDescription>
              {t(
                period.isSubmissionOpen
                  ? 'orderCreate.unavailableExisting'
                  : 'orderCreate.unavailableClosed',
              )}
            </AlertDescription>
          </Alert>
        ) : null}
      </section>

      <form onSubmit={(event) => void submit(event)} noValidate>
        <div className="mt-5 grid min-w-0 gap-5 lg:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)] lg:items-start">
          <section
            className="bg-card border-border min-w-0 border"
            aria-labelledby="product-picker-title"
          >
            <div className="border-border border-b p-4">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <h2 id="product-picker-title" className="font-semibold">
                    {t('orderCreate.chooseItems')}
                  </h2>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {t('orderCreate.chooseItemsDescription')}
                  </p>
                </div>
                {mode === 'regular' && period.canCopyPrevious ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      lines.length > 0
                        ? setCopyDialogOpen(true)
                        : previousMutation.mutate()
                    }
                    disabled={previousMutation.isPending}
                  >
                    {previousMutation.isPending ? (
                      <Spinner aria-hidden="true" />
                    ) : (
                      <ClipboardCopy aria-hidden="true" />
                    )}
                    {t('orderCreate.copyPrevious')}
                  </Button>
                ) : null}
              </div>
              <label className="relative mt-4 block">
                <Search
                  className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2"
                  aria-hidden="true"
                />
                <Input
                  value={searchText}
                  onChange={(event) => setSearchText(event.target.value)}
                  placeholder={t('orderCreate.searchPlaceholder')}
                  className="h-10 pl-9"
                  aria-label={t('orderCreate.searchLabel')}
                />
              </label>
            </div>

            <div className="max-h-[34rem] overflow-y-auto">
              {productsQuery.isLoading ? (
                <div className="space-y-3 p-4">
                  {Array.from({ length: 6 }).map((_, index) => (
                    <Skeleton key={index} className="h-16 w-full" />
                  ))}
                </div>
              ) : null}
              {productsQuery.isError ? (
                <Empty className="min-h-64">
                  <EmptyHeader>
                    <EmptyMedia variant="icon">
                      <PackageOpen aria-hidden="true" />
                    </EmptyMedia>
                    <EmptyTitle>
                      {t('orderCreate.productsErrorTitle')}
                    </EmptyTitle>
                    <EmptyDescription>
                      {t('orderCreate.productsErrorDescription')}
                    </EmptyDescription>
                  </EmptyHeader>
                </Empty>
              ) : null}
              {!productsQuery.isLoading &&
              !productsQuery.isError &&
              (productsQuery.data?.length ?? 0) === 0 ? (
                <Empty className="min-h-64">
                  <EmptyHeader>
                    <EmptyMedia variant="icon">
                      <PackageOpen aria-hidden="true" />
                    </EmptyMedia>
                    <EmptyTitle>{t('orderCreate.noProductsTitle')}</EmptyTitle>
                    <EmptyDescription>
                      {t('orderCreate.noProductsDescription')}
                    </EmptyDescription>
                  </EmptyHeader>
                </Empty>
              ) : null}
              {(productsQuery.data ?? []).map((product, index) => (
                <ProductRow
                  key={product.id ?? index}
                  product={product}
                  selected={Boolean(product.id && selectedIds.has(product.id))}
                  onAdd={() => addProduct(product)}
                />
              ))}
            </div>
          </section>

          <aside
            className="bg-card border-border min-w-0 border lg:sticky lg:top-20"
            aria-labelledby="order-summary-title"
          >
            <div className="border-border flex items-start justify-between gap-4 border-b p-4">
              <div>
                <h2 id="order-summary-title" className="font-semibold">
                  {t('orderCreate.summary')}
                </h2>
                <p className="text-muted-foreground mt-1 text-xs">
                  {t('orderCreate.summaryDescription')}
                </p>
              </div>
              <Badge variant="secondary" className="rounded-[4px] tabular-nums">
                {t('orderCreate.itemCount', { count: lines.length })}
              </Badge>
            </div>

            {lines.length > 0 ? (
              <div className="max-h-[24rem] overflow-y-auto">
                {lines.map((line) => (
                  <SelectedLine
                    key={line.vppId}
                    line={line}
                    invalid={
                      submitAttempted &&
                      (!Number.isInteger(line.qty) || line.qty <= 0)
                    }
                    onChange={updateLine}
                    onRemove={() =>
                      setLines((current) =>
                        current.filter((item) => item.vppId !== line.vppId),
                      )
                    }
                  />
                ))}
              </div>
            ) : (
              <div className="px-4 py-10 text-center">
                <PackageOpen
                  className="text-muted-foreground mx-auto size-6"
                  aria-hidden="true"
                />
                <p className="mt-3 text-sm font-medium">
                  {t('orderCreate.emptySelectionTitle')}
                </p>
                <p className="text-muted-foreground mt-1 text-xs leading-5">
                  {t('orderCreate.emptySelectionDescription')}
                </p>
              </div>
            )}

            <div className="border-border space-y-4 border-t p-4">
              {mode === 'additional' ? (
                <label>
                  <span className="mb-1.5 block text-xs font-medium">
                    {t('orderCreate.supplementReason')}
                  </span>
                  <Textarea
                    {...register('supplementReason')}
                    maxLength={500}
                    rows={3}
                    aria-invalid={Boolean(errors.supplementReason)}
                    placeholder={t('orderCreate.supplementReasonPlaceholder')}
                  />
                  <span className="text-destructive mt-1 block min-h-4 text-xs">
                    {errors.supplementReason?.message ?? ''}
                  </span>
                </label>
              ) : (
                <label>
                  <span className="mb-1.5 block text-xs font-medium">
                    {t('orderCreate.orderNote')}
                  </span>
                  <Textarea
                    {...register('description')}
                    maxLength={500}
                    rows={3}
                    aria-invalid={Boolean(errors.description)}
                    placeholder={t('orderCreate.orderNotePlaceholder')}
                  />
                  <span className="text-destructive mt-1 block min-h-4 text-xs">
                    {errors.description?.message ?? ''}
                  </span>
                </label>
              )}

              {submitAttempted && lines.length === 0 ? (
                <Alert variant="destructive" className="py-3">
                  <CircleAlert aria-hidden="true" />
                  <AlertDescription>
                    {t('orderCreate.itemsRequired')}
                  </AlertDescription>
                </Alert>
              ) : null}

              <dl className="border-border grid grid-cols-2 border-y py-3 text-sm">
                <div>
                  <dt className="text-muted-foreground text-xs">
                    {t('orderCreate.totalItems')}
                  </dt>
                  <dd className="mt-1 font-semibold tabular-nums">
                    {lines.length}
                  </dd>
                </div>
                <div className="border-border border-l pl-4">
                  <dt className="text-muted-foreground text-xs">
                    {t('orderCreate.totalQuantity')}
                  </dt>
                  <dd className="mt-1 font-semibold tabular-nums">
                    {totalQuantity}
                  </dd>
                </div>
              </dl>

              <Button
                type="submit"
                className="h-11 w-full"
                disabled={
                  isSubmitting ||
                  !modeAllowed ||
                  lines.length === 0 ||
                  invalidLines.length > 0
                }
              >
                {isSubmitting ? (
                  <Spinner aria-hidden="true" />
                ) : (
                  <Send aria-hidden="true" />
                )}
                {t(
                  mode === 'additional'
                    ? 'orderCreate.submitAdditional'
                    : 'orderCreate.submitRegular',
                )}
              </Button>
              <p className="text-muted-foreground text-center text-xs">
                {t('orderCreate.draftSaved')}
              </p>
            </div>
          </aside>
        </div>
      </form>

      <Dialog open={copyDialogOpen} onOpenChange={setCopyDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('orderCreate.copyConfirmTitle')}</DialogTitle>
            <DialogDescription>
              {t('orderCreate.copyConfirmDescription')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setCopyDialogOpen(false)}
            >
              {t('actions.cancel')}
            </Button>
            <Button
              type="button"
              onClick={() => previousMutation.mutate()}
              disabled={previousMutation.isPending}
            >
              {previousMutation.isPending ? (
                <Spinner aria-hidden="true" />
              ) : null}
              {t('orderCreate.copyPrevious')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={blocker.state === 'blocked'}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>{t('orderCreate.leaveTitle')}</DialogTitle>
            <DialogDescription>
              {t('orderCreate.leaveDescription')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => blocker.reset?.()}
            >
              {t('orderCreate.stay')}
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={() => blocker.proceed?.()}
            >
              {t('orderCreate.leave')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
