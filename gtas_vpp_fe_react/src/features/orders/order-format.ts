import type { TFunction } from 'i18next'

export function formatPeriod(month?: number, year?: number) {
  if (!month || !year) return '—'
  return `${String(month).padStart(2, '0')}/${year}`
}

export function formatDateTime(
  value: string | null | undefined,
  language: string,
) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'

  return new Intl.DateTimeFormat(language === 'en' ? 'en-GB' : 'vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

export function formatDate(value: string | null | undefined, language: string) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'

  return new Intl.DateTimeFormat(language === 'en' ? 'en-GB' : 'vi-VN', {
    dateStyle: 'medium',
  }).format(date)
}

export function getOrderStatusLabel(
  status: number | undefined,
  isDeadlinePassed: boolean | undefined,
  isAdditionalOrder: boolean | undefined,
  t: TFunction,
) {
  if (status === 1 && isDeadlinePassed && !isAdditionalOrder) {
    return t('orders.status.submittedClosed')
  }

  const key =
    status === 1
      ? 'submitted'
      : status === 4
        ? 'cancelled'
        : status === 6
          ? 'pending'
          : status === 7
            ? 'approved'
            : status === 8
              ? 'rejected'
              : 'unknown'

  return t(`orders.status.${key}`)
}

export function getStatusClass(status?: number) {
  if (status === 7)
    return 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300'
  if (status === 8 || status === 4)
    return 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-300'
  if (status === 6)
    return 'border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-300'
  return 'border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-300'
}
