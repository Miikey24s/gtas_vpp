import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { postApiAccountPasswordRecovery } from '@/api/generated'
import { Button } from '@/components/ui/button'
import { FieldGroup } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { requireApiSuccess } from '@/features/account/account-api'
import { getAccountErrorMessage } from '@/features/account/account-errors'
import { AccountFeedback } from '@/features/account/account-feedback'
import { AccountTextField } from '@/features/account/account-fields'
import { AccountShell } from '@/features/account/account-shell'

export function ForgotPasswordPage() {
  const { t } = useTranslation()
  const [feedback, setFeedback] = useState<{
    tone: 'error' | 'success'
    message: string
  } | null>(null)
  const schema = useMemo(
    () =>
      z.object({
        email: z.string().trim().email(t('validation.emailInvalid')),
      }),
    [t],
  )
  type Values = z.infer<typeof schema>
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { email: '' },
    mode: 'onBlur',
  })

  const submit = handleSubmit(async (values) => {
    setFeedback(null)
    try {
      const result = await postApiAccountPasswordRecovery({ body: values })
      requireApiSuccess(result)
      setFeedback({ tone: 'success', message: t('account.recoveryAccepted') })
    } catch (error) {
      setFeedback({
        tone: 'error',
        message: getAccountErrorMessage(error, t),
      })
    }
  })

  return (
    <AccountShell
      title={t('account.forgotTitle')}
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
      <form onSubmit={(event) => void submit(event)} noValidate>
        <FieldGroup>
          <AccountTextField
            id="recovery-email"
            type="email"
            label={t('account.companyEmail')}
            autoComplete="email"
            autoFocus
            disabled={isSubmitting}
            error={errors.email?.message}
            {...register('email')}
          />
          <AccountFeedback message={feedback?.message} tone={feedback?.tone} />
          <Button type="submit" className="h-11 w-full" disabled={isSubmitting}>
            {isSubmitting ? <Spinner aria-hidden="true" /> : null}
            {t('account.sendAction')}
          </Button>
        </FieldGroup>
      </form>
    </AccountShell>
  )
}
