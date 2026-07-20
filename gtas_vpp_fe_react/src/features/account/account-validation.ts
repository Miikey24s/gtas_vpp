import type { TFunction } from 'i18next'
import { z } from 'zod'

export function passwordSchema(t: TFunction) {
  return z
    .string()
    .min(10, t('validation.passwordPolicy'))
    .regex(/[a-z]/, t('validation.passwordPolicy'))
    .regex(/[A-Z]/, t('validation.passwordPolicy'))
    .regex(/[0-9]/, t('validation.passwordPolicy'))
    .regex(/[^A-Za-z0-9]/, t('validation.passwordPolicy'))
}

export function withPasswordConfirmation<T extends z.ZodRawShape>(
  shape: T,
  t: TFunction,
) {
  return z.object(shape).superRefine((value, context) => {
    const fields = value as Record<string, unknown>
    const password = fields.newPassword ?? fields.password
    if (password !== fields.confirmPassword) {
      context.addIssue({
        code: 'custom',
        path: ['confirmPassword'],
        message: t('validation.passwordMismatch'),
      })
    }
  })
}
