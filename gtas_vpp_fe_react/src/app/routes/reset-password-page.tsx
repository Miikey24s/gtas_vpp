import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { z } from 'zod'

import { postApiAccountPasswordReset } from '@/api/generated'
import { Button } from '@/components/ui/button'
import { FieldGroup } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { requireApiSuccess } from '@/features/account/account-api'
import { getAccountErrorMessage } from '@/features/account/account-errors'
import { AccountFeedback } from '@/features/account/account-feedback'
import { AccountPasswordField } from '@/features/account/account-fields'
import { AccountShell } from '@/features/account/account-shell'
import {
  passwordSchema,
  withPasswordConfirmation,
} from '@/features/account/account-validation'

export function ResetPasswordPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const userId = Number(searchParams.get('userId'))
  const token = searchParams.get('token')?.trim() ?? ''
  const validLink = Number.isInteger(userId) && userId > 0 && token.length > 0
  const [feedback, setFeedback] = useState<{
    tone: 'error' | 'success'
    message: string
  } | null>(
    validLink
      ? null
      : { tone: 'error', message: t('account.errors.resetInvalid') },
  )

  const schema = useMemo(
    () =>
      withPasswordConfirmation(
        {
          newPassword: passwordSchema(t),
          confirmPassword: z.string().min(1, t('validation.confirmRequired')),
        },
        t,
      ),
    [t],
  )
  type Values = z.infer<typeof schema>
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: '', confirmPassword: '' },
    mode: 'onBlur',
  })

  const submit = handleSubmit(async (values) => {
    if (!validLink) return
    setFeedback(null)
    try {
      const result = await postApiAccountPasswordReset({
        body: { userId, token, ...values },
      })
      requireApiSuccess(result)
      setFeedback({ tone: 'success', message: t('account.resetSuccess') })
    } catch (error) {
      setFeedback({
        tone: 'error',
        message: getAccountErrorMessage(
          error,
          t,
          'account.errors.resetInvalid',
        ),
      })
    }
  })

  return (
    <AccountShell
      title={t('account.resetTitle')}
      footer={
        <Link
          className="text-primary text-sm hover:underline"
          to="/login"
          viewTransition
        >
          ← {t('account.backToLogin')}
        </Link>
      }
    >
      <AccountFeedback message={feedback?.message} tone={feedback?.tone} />
      {validLink && feedback?.tone !== 'success' ? (
        <form
          className="mt-5"
          onSubmit={(event) => void submit(event)}
          noValidate
        >
          <FieldGroup>
            <AccountPasswordField
              id="reset-new-password"
              label={t('account.newPassword')}
              autoComplete="new-password"
              autoFocus
              disabled={isSubmitting}
              hint={t('validation.passwordPolicy')}
              error={errors.newPassword?.message}
              {...register('newPassword')}
            />
            <AccountPasswordField
              id="reset-confirm-password"
              label={t('account.confirmPassword')}
              autoComplete="new-password"
              disabled={isSubmitting}
              error={errors.confirmPassword?.message}
              {...register('confirmPassword')}
            />
            <Button
              type="submit"
              className="h-11 w-full"
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner aria-hidden="true" /> : null}
              {t('account.resetAction')}
            </Button>
          </FieldGroup>
        </form>
      ) : null}
    </AccountShell>
  )
}
