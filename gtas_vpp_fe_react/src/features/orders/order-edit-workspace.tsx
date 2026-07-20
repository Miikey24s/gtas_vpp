import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  CalendarDays,
  CircleAlert,
  PackageOpen,
  Save,
  Search,
  ShieldX,
} from 'lucide-react'
import { useDeferredValue, useEffect, useMemo, useRef, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useBlocker, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { z } from 'zod'

import { isApiRequestError, toApiRequestError } from '@/api/api-error'
import {
  getApiVppRequestOrdersById,
  getApiVppRequestProductsLookup,
  putApiVppRequestOrdersById,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { Alert, AlertDescription } from '@/components/ui/alert'
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
  ProductRow,
  SelectedLine,
} from '@/features/orders/order-create-workspace'
import {
  previousItemToDraftLine,
  productToDraftLine,
  readOrderDraft,
  removeOrderDraft,
  type OrderDraftLine,
  writeOrderDraft,
} from '@/features/orders/order-draft'
import { formatPeriod } from '@/features/orders/order-format'

type FormValues = { description: string; supplementReason: string }

export function OrderEditWorkspace({ orderId }: { orderId: string }) {
  const { t } = useTranslation()
  const { user, hasPermission } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchText, setSearchText] = useState('')
  const deferredSearch = useDeferredValue(searchText.trim())
  const [lines, setLines] = useState<OrderDraftLine[]>([])
  const [draftReady, setDraftReady] = useState(false)
  const [submitAttempted, setSubmitAttempted] = useState(false)
  const loadedVersion = useRef<string | null>(null)
  const originalSnapshot = useRef('')
  const allowNavigation = useRef(false)
  const idempotency = useRef({ signature: '', key: '' })
  const canUpdate = hasPermission(permissions.requestUpdateOwn)
  const canUseCatalog = hasPermission(permissions.requestCatalogView)

  const orderQuery = useQuery({
    queryKey: ['vpp', 'orders', orderId],
    enabled: canUpdate && canUseCatalog,
    queryFn: async () => {
      const result = await getApiVppRequestOrdersById({ path: { id: orderId } })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })
  const order = orderQuery.data

  const schema = useMemo(
    () =>
      z.object({
        description: z.string().trim().max(500, t('orderEdit.noteTooLong')),
        supplementReason: order?.isAdditionalOrder
          ? z
              .string()
              .trim()
              .min(5, t('orderEdit.reasonTooShort'))
              .max(500, t('orderEdit.reasonTooLong'))
          : z.string().trim().max(500),
      }),
    [order?.isAdditionalOrder, t],
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
  const draftKey = order?.rowVersion
    ? `gtas-vpp:order-edit-draft:v1:${user?.userId ?? 'unknown'}:${orderId}:${order.rowVersion}`
    : null

  useEffect(() => {
    if (!order || !draftKey || loadedVersion.current === order.rowVersion)
      return
    const draft = readOrderDraft(draftKey)
    const originalLines = (order.items ?? [])
      .map(previousItemToDraftLine)
      .filter((line) => line.vppId)
    originalSnapshot.current = JSON.stringify({
      description: order.description ?? '',
      supplementReason: order.supplementReason ?? '',
      lines: originalLines,
    })
    reset({
      description: draft?.description ?? order.description ?? '',
      supplementReason: draft?.supplementReason ?? order.supplementReason ?? '',
    })
    setLines(draft?.lines ?? originalLines)
    loadedVersion.current = order.rowVersion ?? null
    setDraftReady(true)
  }, [draftKey, order, reset])

  useEffect(() => {
    if (!draftKey || !draftReady) return
    writeOrderDraft(draftKey, {
      description: formValues.description ?? '',
      supplementReason: formValues.supplementReason ?? '',
      lines,
    })
  }, [draftKey, draftReady, formValues, lines])

  const productsQuery = useQuery({
    queryKey: ['vpp', 'products', 'lookup', deferredSearch],
    enabled: Boolean(order?.canEdit) && canUpdate && canUseCatalog,
    queryFn: async () => {
      const result = await getApiVppRequestProductsLookup({
        query: { search: deferredSearch || undefined, top: 40 },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const selectedIds = new Set(lines.map((line) => line.vppId))
  const invalidLines = lines.filter(
    (line) => !Number.isInteger(line.qty) || line.qty <= 0,
  )
  const totalQuantity = lines.reduce(
    (total, line) => total + (Number.isFinite(line.qty) ? line.qty : 0),
    0,
  )
  const hasUnsavedChanges =
    draftReady &&
    (isDirty ||
      JSON.stringify({
        description: formValues.description ?? '',
        supplementReason: formValues.supplementReason ?? '',
        lines,
      }) !== originalSnapshot.current)
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

  const addProduct = (productId: string) => {
    const product = productsQuery.data?.find((item) => item.id === productId)
    if (!product || selectedIds.has(productId)) return
    setLines((current) => [...current, productToDraftLine(product)])
  }

  const submit = handleSubmit(async (values) => {
    setSubmitAttempted(true)
    if (
      !order ||
      !order.rowVersion ||
      !order.canEdit ||
      lines.length === 0 ||
      invalidLines.length
    ) {
      return
    }

    const body = {
      id: orderId,
      description: order.isAdditionalOrder
        ? null
        : values.description.trim() || null,
      isAdditionalOrder: order.isAdditionalOrder,
      supplementReason: order.isAdditionalOrder
        ? values.supplementReason.trim()
        : null,
      rowVersion: order.rowVersion,
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
      const result = await putApiVppRequestOrdersById({
        path: { id: orderId },
        body: { ...body, idempotencyKey: idempotency.current.key },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      if (draftKey) removeOrderDraft(draftKey)
      await queryClient.invalidateQueries({ queryKey: ['vpp'] })
      allowNavigation.current = true
      toast.success(t('orderEdit.success'))
      navigate(`/app/orders/${result.data.id ?? orderId}`, {
        replace: true,
        viewTransition: true,
      })
    } catch (error) {
      toast.error(
        isApiRequestError(error) && error.status === 409
          ? t('orderEdit.conflict')
          : t('orderEdit.error'),
      )
    }
  })

  if (!canUpdate || !canUseCatalog) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orderEdit.forbiddenTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderEdit.forbiddenDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  if (orderQuery.isLoading) {
    return (
      <div className="mx-auto max-w-[110rem] space-y-5 px-4 py-6 sm:px-6 lg:px-8">
        <Skeleton className="h-28" />
        <div className="grid gap-5 lg:grid-cols-2">
          <Skeleton className="h-[32rem]" />
          <Skeleton className="h-[32rem]" />
        </div>
      </div>
    )
  }

  if (orderQuery.isError || !order) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleAlert aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orderEdit.loadErrorTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderEdit.loadErrorDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  if (!order.canEdit || !order.rowVersion) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <ShieldX aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>{t('orderEdit.immutableTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('orderEdit.immutableDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  return (
    <div className="mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <Link
        to={`/app/orders/${orderId}`}
        viewTransition
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-2 text-sm transition-colors"
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('orderEdit.back')}
      </Link>
      <header className="mt-4 border-b pb-5">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-semibold tracking-[-0.035em] sm:text-3xl">
            {t('orderEdit.title')}
          </h1>
          <Badge variant="outline" className="rounded-[4px]">
            <CalendarDays aria-hidden="true" />
            {formatPeriod(order.month, order.year)}
          </Badge>
        </div>
        <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
          {t('orderEdit.description', {
            code: order.vppCode || '—',
            revision: order.revisionNumber ?? 1,
          })}
        </p>
      </header>

      <form onSubmit={(event) => void submit(event)} noValidate>
        <div className="mt-5 grid min-w-0 gap-5 lg:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)] lg:items-start">
          <section
            className="bg-card border-border min-w-0 border"
            aria-labelledby="edit-products-title"
          >
            <div className="border-border border-b p-4">
              <h2 id="edit-products-title" className="font-semibold">
                {t('orderEdit.catalog')}
              </h2>
              <label className="relative mt-3 block">
                <Search
                  className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2"
                  aria-hidden="true"
                />
                <Input
                  value={searchText}
                  onChange={(event) => setSearchText(event.target.value)}
                  placeholder={t('orderEdit.searchPlaceholder')}
                  className="h-10 pl-9"
                  aria-label={t('orderEdit.searchLabel')}
                />
              </label>
            </div>
            <div className="max-h-[36rem] overflow-y-auto">
              {productsQuery.isLoading ? (
                <div className="space-y-3 p-4">
                  {Array.from({ length: 6 }).map((_, index) => (
                    <Skeleton key={index} className="h-16" />
                  ))}
                </div>
              ) : null}
              {productsQuery.isError ? (
                <Alert variant="destructive" className="m-4">
                  <CircleAlert aria-hidden="true" />
                  <AlertDescription>
                    {t('orderEdit.catalogError')}
                  </AlertDescription>
                </Alert>
              ) : null}
              {(productsQuery.data ?? []).map((product, index) => (
                <ProductRow
                  key={product.id ?? index}
                  product={product}
                  selected={Boolean(product.id && selectedIds.has(product.id))}
                  onAdd={() => product.id && addProduct(product.id)}
                />
              ))}
            </div>
          </section>

          <aside
            className="bg-card border-border min-w-0 border lg:sticky lg:top-20"
            aria-labelledby="edit-selection-title"
          >
            <div className="border-border flex items-start justify-between gap-4 border-b p-4">
              <div>
                <h2 id="edit-selection-title" className="font-semibold">
                  {t('orderEdit.selection')}
                </h2>
                <p className="text-muted-foreground mt-1 text-xs">
                  {t('orderEdit.selectionDescription')}
                </p>
              </div>
              <Badge variant="secondary" className="rounded-[4px]">
                {t('orderEdit.itemCount', { count: lines.length })}
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
                    onChange={(nextLine) =>
                      setLines((current) =>
                        current.map((item) =>
                          item.vppId === nextLine.vppId ? nextLine : item,
                        ),
                      )
                    }
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
                  {t('orderEdit.emptyTitle')}
                </p>
              </div>
            )}

            <div className="border-border space-y-4 border-t p-4">
              <label>
                <span className="mb-1.5 block text-xs font-medium">
                  {t(
                    order.isAdditionalOrder
                      ? 'orderEdit.supplementReason'
                      : 'orderEdit.orderNote',
                  )}
                </span>
                <Textarea
                  {...register(
                    order.isAdditionalOrder
                      ? 'supplementReason'
                      : 'description',
                  )}
                  maxLength={500}
                  rows={3}
                  aria-invalid={Boolean(
                    order.isAdditionalOrder
                      ? errors.supplementReason
                      : errors.description,
                  )}
                />
                <span className="text-destructive mt-1 block min-h-4 text-xs">
                  {(order.isAdditionalOrder
                    ? errors.supplementReason?.message
                    : errors.description?.message) ?? ''}
                </span>
              </label>

              {submitAttempted && lines.length === 0 ? (
                <Alert variant="destructive">
                  <CircleAlert aria-hidden="true" />
                  <AlertDescription>
                    {t('orderEdit.itemsRequired')}
                  </AlertDescription>
                </Alert>
              ) : null}

              <dl className="border-border grid grid-cols-2 border-y py-3 text-sm">
                <div>
                  <dt className="text-muted-foreground text-xs">
                    {t('orderEdit.totalItems')}
                  </dt>
                  <dd className="mt-1 font-semibold tabular-nums">
                    {lines.length}
                  </dd>
                </div>
                <div className="border-border border-l pl-4">
                  <dt className="text-muted-foreground text-xs">
                    {t('orderEdit.totalQuantity')}
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
                  isSubmitting || lines.length === 0 || invalidLines.length > 0
                }
              >
                {isSubmitting ? (
                  <Spinner aria-hidden="true" />
                ) : (
                  <Save aria-hidden="true" />
                )}
                {t('orderEdit.save')}
              </Button>
              <p className="text-muted-foreground text-center text-xs">
                {t('orderEdit.draftSaved')}
              </p>
            </div>
          </aside>
        </div>
      </form>

      <Dialog open={blocker.state === 'blocked'}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>{t('orderEdit.leaveTitle')}</DialogTitle>
            <DialogDescription>
              {t('orderEdit.leaveDescription')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => blocker.reset?.()}>
              {t('orderCreate.stay')}
            </Button>
            <Button variant="destructive" onClick={() => blocker.proceed?.()}>
              {t('orderCreate.leave')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
