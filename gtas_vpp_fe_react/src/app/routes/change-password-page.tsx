import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { z } from 'zod'

import { postApiAccountPasswordChange } from '@/api/generated'
import { clearAuthSession } from '@/auth/auth-session'
import { useAuth } from '@/auth/use-auth'
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

export function ChangePasswordPage() {
  const { t } = useTranslation()
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const required = searchParams.get('required') === '1'
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const schema = useMemo(
    () =>
      withPasswordConfirmation(
        {
          currentPassword: z
            .string()
            .min(1, t('validation.currentPasswordRequired')),
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
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
    mode: 'onBlur',
  })

  const submit = handleSubmit(async (values) => {
    setErrorMessage(null)
    try {
      const result = await postApiAccountPasswordChange({ body: values })
      requireApiSuccess(result)
      clearAuthSession()
      window.location.replace('/login?passwordChanged=1')
    } catch (error) {
      setErrorMessage(
        getAccountErrorMessage(error, t, 'account.errors.changeInvalid'),
      )
    }
  })

  const cancel = async () => {
    if (required) {
      await logout()
      navigate('/login', { replace: true, viewTransition: true })
      return
    }
    navigate('/app/orders', { viewTransition: true })
  }

  return (
    <AccountShell title={t('account.changeTitle')}>
      {required ? (
        <AccountFeedback message={t('account.changeRequired')} tone="warning" />
      ) : null}
      <form
        className={required ? 'mt-5' : undefined}
        onSubmit={(event) => void submit(event)}
        noValidate
      >
        <FieldGroup>
          <AccountPasswordField
            id="change-current-password"
            label={t('account.currentPassword')}
            autoComplete="current-password"
            autoFocus
            disabled={isSubmitting}
            error={errors.currentPassword?.message}
            {...register('currentPassword')}
          />
          <AccountPasswordField
            id="change-new-password"
            label={t('account.newPassword')}
            autoComplete="new-password"
            disabled={isSubmitting}
            hint={t('validation.passwordPolicy')}
            error={errors.newPassword?.message}
            {...register('newPassword')}
          />
          <AccountPasswordField
            id="change-confirm-password"
            label={t('account.confirmPassword')}
            autoComplete="new-password"
            disabled={isSubmitting}
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />
          <AccountFeedback message={errorMessage} tone="error" />
          <Button type="submit" className="h-11 w-full" disabled={isSubmitting}>
            {isSubmitting ? <Spinner aria-hidden="true" /> : null}
            {t('account.changeAction')}
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="w-full"
            disabled={isSubmitting}
            onClick={() => void cancel()}
          >
            {required ? t('auth.logout') : t('account.backToApplication')}
          </Button>
        </FieldGroup>
      </form>
    </AccountShell>
  )
}
