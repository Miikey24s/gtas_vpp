import type { TFunction } from 'i18next'

import { isApiRequestError } from '@/api/api-error'

const codeKeys: Record<string, string> = {
  PASSWORD_CONFIRMATION_MISMATCH: 'account.errors.passwordMismatch',
  REGISTRATION_FIELDS_REQUIRED: 'account.errors.requiredFields',
  REGISTRATION_INVALID: 'account.errors.registrationInvalid',
  EMAIL_CONFIRMATION_INVALID: 'account.errors.confirmationInvalid',
  PASSWORD_RESET_INVALID: 'account.errors.resetInvalid',
  PASSWORD_CHANGE_INVALID: 'account.errors.changeInvalid',
  PASSWORD_CHANGE_REQUIRED: 'account.errors.changeRequired',
  ACCOUNT_UNAVAILABLE: 'account.errors.accountUnavailable',
  ACCOUNT_NOT_FOUND: 'account.errors.accountNotFound',
  ACCOUNT_NOT_ACTIVE: 'account.errors.accountNotActive',
}

export function getAccountErrorMessage(
  error: unknown,
  t: TFunction,
  fallbackKey = 'account.errors.requestFailed',
) {
  if (!isApiRequestError(error)) return t(fallbackKey)

  const key = error.code ? codeKeys[error.code.toUpperCase()] : undefined
  if (key) return t(key)

  if (error.status === 401) return t('account.errors.unauthorized')
  if (error.status === 403) return t('account.errors.forbidden')
  if (error.status === 404) return t('account.errors.notFound')
  if (error.status === 409) return t('account.errors.conflict')
  if (error.status === 429) return t('account.errors.rateLimited')
  return t(fallbackKey)
}
