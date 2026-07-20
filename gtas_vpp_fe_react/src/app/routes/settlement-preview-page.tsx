import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  BadgeCheck,
  Check,
  CircleAlert,
  FileWarning,
  Landmark,
  LoaderCircle,
  PackageSearch,
  RefreshCw,
  ShieldCheck,
} from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'

import { isApiRequestError, toApiRequestError } from '@/api/api-error'
import {
  getApiCatalogItemsById,
  getApiPeriodSettlementByYByM,
  postApiPeriodSettlementBySettlementIdCorrect,
  postApiPeriodSettlementConfirm,
  postApiPeriodSettlementPreview,
  type PriceBookQuoteResDto,
  type SettlementExceptionReqDto,
} from '@/api/generated'
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
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Textarea } from '@/components/ui/textarea'
import { formatPeriod } from '@/features/orders/order-format'
import {
  formatMoney,
  parsePeriodParams,
  settlementBlockerLabel,
} from '@/features/periods/period-format'
import { cn } from '@/lib/utils'

type PreviewInput = {
  priceListId?: string
  primarySupplierId?: string
  exceptions: SettlementExceptionReqDto[]
}

function uniqueSuppliers(quotes: PriceBookQuoteResDto[]) {
  const seen = new Set<string>()
  return quotes.filter((quote) => {
    if (!quote.supplierId || seen.has(quote.supplierId)) return false
    seen.add(quote.supplierId)
    return true
  })
}

function MissingItemEditor({
  vppId,
  suppliers,
  primarySupplierId,
  value,
  onChange,
}: {
  vppId: string
  suppliers: PriceBookQuoteResDto[]
  primarySupplierId?: string
  value?: SettlementExceptionReqDto
  onChange: (value: SettlementExceptionReqDto) => void
}) {
  const { t } = useTranslation()
  const itemQuery = useQuery({
    queryKey: ['catalog', 'item', vppId],
    retry: false,
    queryFn: async () => {
      const result = await getApiCatalogItemsById({ path: { id: vppId } })
      return result.data ?? null
    },
  })
  const alternatives = suppliers.filter(
    (quote) => quote.supplierId && quote.supplierId !== primarySupplierId,
  )

  return (
    <article className="border-border border p-4">
      <div className="flex items-start gap-3">
        <PackageSearch className="text-muted-foreground mt-0.5 size-4 shrink-0" aria-hidden="true" />
        <div className="min-w-0 flex-1">
          <h3 className="truncate text-sm font-medium">
            {itemQuery.data?.vppName || t('periods.exceptionItem')}
          </h3>
          <p className="text-muted-foreground mt-1 truncate font-mono text-[11px]">
            {itemQuery.data?.vppCode || vppId}
          </p>
        </div>
      </div>
      <div className="mt-4 grid gap-4 md:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor={`supplier-${vppId}`}>{t('periods.exceptionSupplier')}</Label>
          <Select
            value={value?.supplierId ?? ''}
            onValueChange={(supplierId: string) =>
              onChange({
                vppId,
                supplierId,
                reason: value?.reason ?? '',
              })
            }
          >
            <SelectTrigger id={`supplier-${vppId}`} className="w-full">
              <SelectValue placeholder={t('periods.exceptionSupplierPlaceholder')} />
            </SelectTrigger>
            <SelectContent>
              {alternatives.map((quote) => (
                <SelectItem key={quote.supplierId} value={quote.supplierId!}>
                  {quote.supplierName || quote.supplierId}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor={`reason-${vppId}`}>{t('periods.exceptionReason')}</Label>
          <Textarea
            id={`reason-${vppId}`}
            value={value?.reason ?? ''}
            maxLength={500}
            placeholder={t('periods.exceptionReasonPlaceholder')}
            onChange={(event) =>
              onChange({
                vppId,
                supplierId: value?.supplierId ?? '',
                reason: event.target.value,
              })
            }
          />
        </div>
      </div>
    </article>
  )
}

export function SettlementPreviewPage() {
  const { t, i18n } = useTranslation()
  const params = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const period = parsePeriodParams(params.year, params.month)
  const language = i18n.resolvedLanguage ?? 'vi'
  const correctionMode = searchParams.get('mode') === 'correction'
  const [appliedInput, setAppliedInput] = useState<PreviewInput>({ exceptions: [] })
  const [draftInput, setDraftInput] = useState<PreviewInput>({ exceptions: [] })
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [correctionReason, setCorrectionReason] = useState('')
  const idempotencyKeys = useRef(new Map<string, string>())

  const statusQuery = useQuery({
    queryKey: ['vpp', 'period-settlement', 'status', period?.year, period?.month],
    enabled: Boolean(period),
    queryFn: async () => {
      const result = await getApiPeriodSettlementByYByM({
        path: { y: period!.year, m: period!.month },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const previewQuery = useQuery({
    queryKey: [
      'vpp',
      'period-settlement',
      'preview',
      period?.year,
      period?.month,
      appliedInput,
    ],
    enabled: Boolean(period),
    queryFn: async () => {
      const result = await postApiPeriodSettlementPreview({
        body: {
          year: period!.year,
          month: period!.month,
          priceListId: appliedInput.priceListId,
          primarySupplierId: appliedInput.primarySupplierId,
          exceptions: appliedInput.exceptions,
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const preview = previewQuery.data
  const quotes = preview?.quotes ?? []
  const selectedQuote =
    preview?.primaryQuote ??
    quotes.find(
      (quote) =>
        quote.priceListId === appliedInput.priceListId &&
        quote.supplierId === appliedInput.primarySupplierId,
    )
  const missingIds = selectedQuote?.missingVppIds ?? []
  const suppliers = uniqueSuppliers(quotes)
  const blockers = preview?.blockers ?? []
  const canConfirm = Boolean(
    preview?.inputHash &&
      preview.primarySupplierId &&
      preview.primaryPriceListId &&
      preview.primaryQuote?.isEligible &&
      blockers.length === 0 &&
      (statusQuery.data?.pendingAdditionalCount ?? 0) === 0,
  )

  const getIdempotencyKey = () => {
    const mapKey = `${correctionMode ? 'correct' : 'confirm'}:${statusQuery.data?.settlementId ?? 'new'}:${preview?.inputHash ?? ''}`
    const existing = idempotencyKeys.current.get(mapKey)
    if (existing) return existing
    const created = crypto.randomUUID()
    idempotencyKeys.current.set(mapKey, created)
    return created
  }

  const confirmMutation = useMutation({
    mutationFn: async () => {
      if (
        !period ||
        !preview?.inputHash ||
        !preview.priceAsOfUtc ||
        !preview.primarySupplierId ||
        !preview.primaryPriceListId
      ) {
        throw new Error('PREVIEW_REQUIRED')
      }
      const body = {
        year: period.year,
        month: period.month,
        priceAsOfUtc: preview.priceAsOfUtc,
        inputHash: preview.inputHash,
        primarySupplierId: preview.primarySupplierId,
        priceListId: preview.primaryPriceListId,
        idempotencyKey: getIdempotencyKey(),
        exceptions: appliedInput.exceptions,
      }
      if (correctionMode) {
        const reason = correctionReason.trim()
        if (reason.length < 5 || !statusQuery.data?.settlementId) {
          throw new Error('CORRECTION_REASON_REQUIRED')
        }
        const result = await postApiPeriodSettlementBySettlementIdCorrect({
          path: { settlementId: statusQuery.data.settlementId },
          body: { ...body, reason },
        })
        if (!result.data) throw toApiRequestError(result.error, result.response)
        return result.data
      }
      const result = await postApiPeriodSettlementConfirm({ body })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      setConfirmOpen(false)
      await queryClient.invalidateQueries({
        queryKey: ['vpp', 'period-settlement'],
      })
      toast.success(
        t(
          correctionMode
            ? 'periods.correctionSuccess'
            : 'periods.confirmSuccess',
        ),
      )
      await navigate(
        `/app/periods/${period!.year}/${period!.month}/settlement`,
        { viewTransition: true },
      )
    },
    onError: (error) => {
      if (
        error instanceof Error &&
        error.message === 'CORRECTION_REASON_REQUIRED'
      ) {
        toast.error(t('periods.correctionReasonInvalid'))
        return
      }
      toast.error(
        isApiRequestError(error) && error.status === 409
          ? t('periods.confirmConflict')
          : t('periods.confirmError'),
      )
    },
  })

  if (!period) {
    return (
      <div className="mx-auto max-w-5xl px-5 py-12 sm:px-8">
        <Empty className="min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon"><CircleAlert aria-hidden="true" /></EmptyMedia>
            <EmptyTitle>{t('periods.invalidTitle')}</EmptyTitle>
            <EmptyDescription>{t('periods.invalidDescription')}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const chooseQuote = (quote: PriceBookQuoteResDto) => {
    const next = {
      priceListId: quote.priceListId,
      primarySupplierId: quote.supplierId,
      exceptions: [] as SettlementExceptionReqDto[],
    }
    setDraftInput(next)
    setAppliedInput(next)
  }
  const updateException = (nextException: SettlementExceptionReqDto) => {
    setDraftInput((current) => ({
      ...current,
      exceptions: [
        ...current.exceptions.filter(
          (item) => item.vppId !== nextException.vppId,
        ),
        nextException,
      ],
    }))
  }
  const exceptionDraftValid =
    missingIds.length > 0 &&
    missingIds.every((vppId) => {
      const exception = draftInput.exceptions.find((item) => item.vppId === vppId)
      return Boolean(exception?.supplierId && (exception.reason?.trim().length ?? 0) >= 5)
    })

  return (
    <div className="mx-auto w-full max-w-[100rem] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <Button asChild variant="ghost" size="sm" className="-ml-3">
        <Link to={`/app/periods/${period.year}/${period.month}`} viewTransition>
          <ArrowLeft aria-hidden="true" />
          {t('periods.backToPeriod')}
        </Link>
      </Button>

      <header className="mt-4 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-primary text-xs font-semibold tracking-[0.14em] uppercase">
            {correctionMode ? t('periods.correctionEyebrow') : t('periods.previewEyebrow')}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-[-0.04em] sm:text-3xl">
            {t(correctionMode ? 'periods.correctionPageTitle' : 'periods.previewPageTitle', {
              period: formatPeriod(period.month, period.year),
            })}
          </h1>
          <p className="text-muted-foreground mt-2 max-w-3xl text-sm leading-6">
            {t('periods.previewPageDescription')}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => void previewQuery.refetch()}
          disabled={previewQuery.isFetching}
        >
          <RefreshCw
            className={previewQuery.isFetching ? 'animate-spin' : ''}
            aria-hidden="true"
          />
          {t('periods.runPreview')}
        </Button>
      </header>

      {previewQuery.isLoading ? (
        <div className="mt-6 grid gap-4 lg:grid-cols-3">
          <Skeleton className="h-72 lg:col-span-2" />
          <Skeleton className="h-72" />
        </div>
      ) : null}
      {previewQuery.isError ? (
        <Empty className="mt-6 min-h-72 border">
          <EmptyHeader>
            <EmptyMedia variant="icon"><CircleAlert aria-hidden="true" /></EmptyMedia>
            <EmptyTitle>{t('periods.previewErrorTitle')}</EmptyTitle>
            <EmptyDescription>{t('periods.previewErrorDescription')}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}

      {preview ? (
        <>
          <section className="border-border mt-6 grid grid-cols-2 border lg:grid-cols-4">
            {[
              [t('periods.requestedItems'), preview.requestedItemCount ?? 0],
              [t('periods.requestedLines'), preview.requestedLineCount ?? 0],
              [t('periods.quoteCount'), quotes.length],
              [t('periods.pendingSupplements'), preview.pendingAdditionalCount ?? 0],
            ].map(([label, value], index) => (
              <div
                key={String(label)}
                className={`p-4 sm:p-5 ${index % 2 === 1 ? 'border-l' : ''} ${index >= 2 ? 'border-t lg:border-t-0' : ''} ${index > 0 ? 'lg:border-l' : ''}`}
              >
                <p className="text-lg font-semibold tabular-nums">{value}</p>
                <p className="text-muted-foreground mt-1 text-xs">{label}</p>
              </div>
            ))}
          </section>

          <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
            <div className="min-w-0 space-y-6">
              <section>
                <div className="flex items-end justify-between gap-4">
                  <div>
                    <h2 className="text-lg font-semibold">{t('periods.quotesTitle')}</h2>
                    <p className="text-muted-foreground mt-1 text-sm">{t('periods.quotesDescription')}</p>
                  </div>
                </div>
                <div className="mt-4 grid gap-4 lg:grid-cols-2">
                  {quotes.map((quote) => {
                    const selected =
                      quote.priceListId === appliedInput.priceListId &&
                      quote.supplierId === appliedInput.primarySupplierId
                    return (
                      <button
                        key={`${quote.priceListId}-${quote.supplierId}`}
                        type="button"
                        onClick={() => chooseQuote(quote)}
                        className={cn(
                          'border-border bg-card hover:border-primary/45 focus-visible:ring-ring relative min-w-0 border p-5 text-left transition-[border-color,background-color,transform] duration-200 hover:-translate-y-0.5 focus-visible:ring-2 focus-visible:outline-none motion-reduce:transform-none motion-reduce:transition-none',
                          selected && 'border-primary bg-primary/[0.035]',
                        )}
                        aria-pressed={selected}
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div className="min-w-0">
                            <p className="truncate font-semibold">
                              {quote.supplierName || t('periods.unnamedSupplier')}
                            </p>
                            <p className="text-muted-foreground mt-1 truncate text-xs">
                              {quote.priceListCode || quote.priceListId} · v{quote.version ?? 0}
                            </p>
                          </div>
                          {selected ? (
                            <span className="bg-primary text-primary-foreground grid size-6 shrink-0 place-items-center rounded-full">
                              <Check className="size-3.5" aria-hidden="true" />
                            </span>
                          ) : null}
                        </div>
                        <div className="mt-5 flex items-end justify-between gap-4">
                          <div>
                            <p className="text-muted-foreground text-xs">{t('periods.coverage')}</p>
                            <p className="mt-1 font-semibold tabular-nums">
                              {quote.coveredItemCount ?? 0}/{quote.requestedItemCount ?? 0} · {quote.coveragePercent ?? 0}%
                            </p>
                          </div>
                          <p className="text-right text-lg font-semibold tabular-nums">
                            {formatMoney(quote.grandTotal, language, quote.currencyCode || 'VND')}
                          </p>
                        </div>
                        <div className="mt-4 flex flex-wrap items-center gap-2">
                          <Badge variant="outline" className="rounded-[4px]">
                            {t('periods.leadTime', { count: quote.maximumLeadTimeDays ?? 0 })}
                          </Badge>
                          <Badge
                            variant="outline"
                            className={cn(
                              'rounded-[4px]',
                              quote.isEligible
                                ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300'
                                : 'border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-300',
                            )}
                          >
                            {quote.isEligible ? t('periods.completeCoverage') : t('periods.needsExceptions')}
                          </Badge>
                        </div>
                      </button>
                    )
                  })}
                </div>
              </section>

              {selectedQuote && missingIds.length > 0 ? (
                <section>
                  <div>
                    <h2 className="text-lg font-semibold">{t('periods.exceptionsTitle')}</h2>
                    <p className="text-muted-foreground mt-1 text-sm leading-6">
                      {t('periods.exceptionsDescription', { count: missingIds.length })}
                    </p>
                  </div>
                  <div className="mt-4 space-y-3">
                    {missingIds.map((vppId) => (
                      <MissingItemEditor
                        key={vppId}
                        vppId={vppId}
                        suppliers={suppliers}
                        primarySupplierId={selectedQuote.supplierId}
                        value={draftInput.exceptions.find((item) => item.vppId === vppId)}
                        onChange={updateException}
                      />
                    ))}
                  </div>
                  <div className="mt-4 flex justify-end">
                    <Button
                      onClick={() => setAppliedInput(draftInput)}
                      disabled={!exceptionDraftValid || previewQuery.isFetching}
                    >
                      {previewQuery.isFetching ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : <ShieldCheck aria-hidden="true" />}
                      {t('periods.validateExceptions')}
                    </Button>
                  </div>
                </section>
              ) : null}

              {preview.exceptions?.length ? (
                <section className="border-border border p-5">
                  <h2 className="font-semibold">{t('periods.exceptionEvidenceTitle')}</h2>
                  <div className="mt-4 space-y-3">
                    {preview.exceptions.map((exception) => (
                      <div
                        key={exception.vppId}
                        className="flex flex-wrap items-center justify-between gap-3 text-sm"
                      >
                        <span className="font-mono text-xs">{exception.vppId}</span>
                        <div className="flex items-center gap-3">
                          <span className="tabular-nums">
                            {formatMoney(exception.grossAmount, language)}
                          </span>
                          <Badge
                            variant="outline"
                            className={cn(
                              'rounded-[4px]',
                              exception.isValid
                                ? 'border-emerald-200 text-emerald-700 dark:border-emerald-900 dark:text-emerald-300'
                                : 'border-destructive/30 text-destructive',
                            )}
                          >
                            {exception.isValid ? t('periods.valid') : t('periods.invalid')}
                          </Badge>
                        </div>
                      </div>
                    ))}
                  </div>
                </section>
              ) : null}
            </div>

            <aside className="h-fit xl:sticky xl:top-24">
              <div className="border-border border p-5">
                <div className="flex items-start gap-3">
                  <Landmark className="text-primary mt-0.5 size-5" aria-hidden="true" />
                  <div>
                    <h2 className="font-semibold">{t('periods.decisionTitle')}</h2>
                    <p className="text-muted-foreground mt-1 text-xs leading-5">
                      {t('periods.decisionDescription')}
                    </p>
                  </div>
                </div>

                {selectedQuote ? (
                  <dl className="mt-5 space-y-3 text-sm">
                    <div className="flex justify-between gap-4">
                      <dt className="text-muted-foreground">{t('periods.supplier')}</dt>
                      <dd className="max-w-44 text-right font-medium">{selectedQuote.supplierName || '—'}</dd>
                    </div>
                    <div className="flex justify-between gap-4">
                      <dt className="text-muted-foreground">{t('periods.coverage')}</dt>
                      <dd className="font-medium tabular-nums">{selectedQuote.coveragePercent ?? 0}%</dd>
                    </div>
                    <div className="flex justify-between gap-4 border-t pt-3">
                      <dt className="font-medium">{t('periods.total')}</dt>
                      <dd className="font-semibold tabular-nums">
                        {formatMoney(selectedQuote.grandTotal, language, selectedQuote.currencyCode || 'VND')}
                      </dd>
                    </div>
                  </dl>
                ) : (
                  <p className="text-muted-foreground mt-5 text-sm">{t('periods.chooseQuoteHint')}</p>
                )}

                {blockers.length > 0 ? (
                  <Alert variant="destructive" className="mt-5 rounded-none">
                    <FileWarning aria-hidden="true" />
                    <AlertTitle>{t('periods.blockersTitle')}</AlertTitle>
                    <AlertDescription>
                      <ul className="list-disc space-y-1 pl-4">
                        {blockers.map((blocker) => (
                          <li key={blocker}>{settlementBlockerLabel(blocker, t)}</li>
                        ))}
                      </ul>
                    </AlertDescription>
                  </Alert>
                ) : canConfirm ? (
                  <Alert className="mt-5 rounded-none border-emerald-200 text-emerald-800 dark:border-emerald-900 dark:text-emerald-200">
                    <BadgeCheck aria-hidden="true" />
                    <AlertTitle>{t('periods.readyTitle')}</AlertTitle>
                    <AlertDescription>{t('periods.readyDescription')}</AlertDescription>
                  </Alert>
                ) : null}

                <Button
                  className="mt-5 w-full"
                  disabled={!canConfirm || confirmMutation.isPending}
                  onClick={() => setConfirmOpen(true)}
                >
                  <ShieldCheck aria-hidden="true" />
                  {correctionMode ? t('periods.createCorrection') : t('periods.confirmSettlement')}
                </Button>
                {preview.inputHash ? (
                  <p className="text-muted-foreground mt-3 truncate font-mono text-[10px]" title={preview.inputHash}>
                    {t('periods.inputHash')}: {preview.inputHash.slice(0, 16)}…
                  </p>
                ) : null}
              </div>
            </aside>
          </div>
        </>
      ) : null}

      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {correctionMode ? t('periods.correctionConfirmTitle') : t('periods.confirmTitle')}
            </DialogTitle>
            <DialogDescription>
              {correctionMode ? t('periods.correctionConfirmDescription') : t('periods.confirmDescription')}
            </DialogDescription>
          </DialogHeader>
          {correctionMode ? (
            <div className="space-y-2">
              <Label htmlFor="correction-reason">{t('periods.correctionReason')}</Label>
              <Textarea
                id="correction-reason"
                value={correctionReason}
                maxLength={500}
                placeholder={t('periods.correctionReasonPlaceholder')}
                onChange={(event) => setCorrectionReason(event.target.value)}
              />
              <p className="text-muted-foreground text-xs">{t('periods.fourEyesHint')}</p>
            </div>
          ) : null}
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmOpen(false)}>
              {t('actions.cancel')}
            </Button>
            <Button
              onClick={() => confirmMutation.mutate()}
              disabled={
                confirmMutation.isPending ||
                (correctionMode && correctionReason.trim().length < 5)
              }
            >
              {confirmMutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : <ShieldCheck aria-hidden="true" />}
              {correctionMode ? t('periods.confirmCorrection') : t('periods.confirmSettlement')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
