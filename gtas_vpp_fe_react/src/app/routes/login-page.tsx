import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { z } from 'zod'

import { isApiRequestError } from '@/api/api-error'
import { useAuth } from '@/auth/use-auth'
import { Button } from '@/components/ui/button'
import { FieldGroup } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { AccountFeedback } from '@/features/account/account-feedback'
import {
  AccountPasswordField,
  AccountTextField,
} from '@/features/account/account-fields'
import { AccountShell } from '@/features/account/account-shell'

function getSafeReturnUrl(value: string | null) {
  if (!value || !value.startsWith('/') || value.startsWith('//')) {
    return '/app/orders'
  }
  return value
}

export function LoginPage() {
  const { t } = useTranslation()
  const { status, login } = useAuth()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const passwordChanged = searchParams.get('passwordChanged') === '1'

  const schema = useMemo(
    () =>
      z.object({
        username: z.string().trim().min(1, t('validation.usernameRequired')),
        password: z.string().min(1, t('validation.passwordRequired')),
      }),
    [t],
  )

  type LoginValues = z.infer<typeof schema>
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: { username: '', password: '' },
    mode: 'onBlur',
  })

  if (status === 'authenticated') {
    return <Navigate to="/app/orders" replace />
  }
  if (status === 'password-change-required') {
    return <Navigate to="/change-password?required=1" replace />
  }

  const submit = handleSubmit(async (values) => {
    setSubmissionError(null)
    try {
      const nextStatus = await login(values)
      if (nextStatus === 'password-change-required') {
        navigate('/change-password?required=1', {
          replace: true,
          viewTransition: true,
        })
        return
      }
      navigate(getSafeReturnUrl(searchParams.get('returnUrl')), {
        replace: true,
        viewTransition: true,
      })
    } catch (error) {
      if (isApiRequestError(error) && error.status === 401) {
        setSubmissionError(t('auth.invalidCredentials'))
      } else {
        setSubmissionError(t('auth.loginUnavailable'))
      }
    }
  })

  return (
    <AccountShell
      title={t('auth.login')}
      footer={
        <nav
          className="flex items-center justify-center gap-3 text-sm"
          aria-label={t('auth.accountLinks')}
        >
          <Link
            className="text-primary hover:underline"
            to="/forgot-password"
            viewTransition
          >
            {t('auth.forgotPassword')}
          </Link>
          <span className="text-muted-foreground" aria-hidden="true">
            ·
          </span>
          <Link
            className="text-primary hover:underline"
            to="/register"
            viewTransition
          >
            {t('auth.register')}
          </Link>
        </nav>
      }
    >
      <AccountFeedback
        message={passwordChanged ? t('account.changeSuccess') : null}
        tone="success"
      />
      <form
        className={passwordChanged ? 'mt-5' : undefined}
        onSubmit={(event) => void submit(event)}
        noValidate
      >
        <FieldGroup>
          <AccountTextField
            id="username"
            label={t('auth.username')}
            autoComplete="username"
            autoFocus
            disabled={isSubmitting}
            error={errors.username?.message}
            {...register('username')}
          />
          <AccountPasswordField
            id="password"
            label={t('auth.password')}
            autoComplete="current-password"
            disabled={isSubmitting}
            error={errors.password?.message}
            {...register('password')}
          />
          <AccountFeedback message={submissionError} tone="error" />
          <Button type="submit" className="h-11 w-full" disabled={isSubmitting}>
            {isSubmitting ? <Spinner aria-hidden="true" /> : null}
            {t('auth.login')}
          </Button>
        </FieldGroup>
      </form>
    </AccountShell>
  )
}
