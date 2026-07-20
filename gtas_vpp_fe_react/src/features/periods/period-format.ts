import type { TFunction } from 'i18next'

export function formatMoney(
  value: number | null | undefined,
  language: string,
  currency = 'VND',
) {
  return new Intl.NumberFormat(language === 'en' ? 'en-US' : 'vi-VN', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value ?? 0)
}

export function previousCalendarPeriod(now = new Date()) {
  const date = new Date(now.getFullYear(), now.getMonth() - 1, 1)
  return { year: date.getFullYear(), month: date.getMonth() + 1 }
}

export function parsePeriodParams(year?: string, month?: string) {
  const parsedYear = Number(year)
  const parsedMonth = Number(month)
  if (
    !Number.isInteger(parsedYear) ||
    parsedYear < 2024 ||
    parsedYear > 2100 ||
    !Number.isInteger(parsedMonth) ||
    parsedMonth < 1 ||
    parsedMonth > 12
  ) {
    return null
  }
  return { year: parsedYear, month: parsedMonth }
}

export function settlementBlockerLabel(blocker: string, t: TFunction) {
  const [code, detail] = blocker.split(':')
  const labels: Record<string, string> = {
    PENDING_SUPPLEMENTS: t('periods.blockers.pendingSupplements', {
      count: Number(detail) || 0,
    }),
    NO_SUBMITTED_ITEMS: t('periods.blockers.noItems'),
    NO_COMPLETE_PRICE_COVERAGE: t('periods.blockers.noCoverage'),
    PRIMARY_SUPPLIER_NOT_COVERED: t('periods.blockers.supplierNotCovered'),
    PRICE_BOOK_NOT_COVERED: t('periods.blockers.priceBookNotCovered'),
    PRIMARY_QUOTE_HAS_UNRESOLVED_ITEMS: t('periods.blockers.unresolvedItems'),
    SUPPLEMENT_WITHOUT_BASE: t('periods.blockers.supplementWithoutBase'),
    INVALID_SUPPLIER_EXCEPTION: t('periods.blockers.invalidException'),
    EXCEPTION_MUST_USE_ANOTHER_SUPPLIER: t(
      'periods.blockers.sameSupplierException',
    ),
    EXCEPTION_NOT_REQUIRED: t('periods.blockers.exceptionNotRequired'),
    EXCEPTION_RESOLVER_UNAVAILABLE: t('periods.blockers.exceptionUnavailable'),
    SUPPLIER_EXCEPTION_UNRESOLVED: t('periods.blockers.exceptionUnresolved'),
  }
  return labels[code] ?? t('periods.blockers.unknown', { code })
}
