import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { postApiAccountRegister } from '@/api/generated'
import { Button } from '@/components/ui/button'
import { FieldGroup } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { requireApiSuccess } from '@/features/account/account-api'
import { getAccountErrorMessage } from '@/features/account/account-errors'
import { AccountFeedback } from '@/features/account/account-feedback'
import {
  AccountPasswordField,
  AccountTextField,
} from '@/features/account/account-fields'
import { AccountShell } from '@/features/account/account-shell'
import {
  passwordSchema,
  withPasswordConfirmation,
} from '@/features/account/account-validation'

export function RegisterPage() {
  const { t } = useTranslation()
  const [feedback, setFeedback] = useState<{
    tone: 'error' | 'success'
    message: string
  } | null>(null)

  const schema = useMemo(
    () =>
      withPasswordConfirmation(
        {
          username: z
            .string()
            .trim()
            .min(3, t('validation.usernameLength'))
            .max(100, t('validation.usernameLength')),
          fullName: z
            .string()
            .trim()
            .min(2, t('validation.fullNameLength'))
            .max(250, t('validation.fullNameLength')),
          email: z.string().trim().email(t('validation.emailInvalid')),
          password: passwordSchema(t),
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
    reset,
    formState: { errors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      username: '',
      fullName: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
    mode: 'onBlur',
  })

  const submit = handleSubmit(async (values) => {
    setFeedback(null)
    try {
      const result = await postApiAccountRegister({
        body: { ...values, employeeCode: null },
      })
      requireApiSuccess(result)
      reset()
      setFeedback({ tone: 'success', message: t('account.registerAccepted') })
    } catch (error) {
      setFeedback({
        tone: 'error',
        message: getAccountErrorMessage(
          error,
          t,
          'account.errors.registrationInvalid',
        ),
      })
    }
  })

  return (
    <AccountShell
      title={t('account.registerTitle')}
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
            id="register-username"
            label={t('auth.username')}
            autoComplete="username"
            autoFocus
            disabled={isSubmitting}
            error={errors.username?.message}
            {...register('username')}
          />
          <AccountTextField
            id="register-full-name"
            label={t('account.fullName')}
            autoComplete="name"
            disabled={isSubmitting}
            error={errors.fullName?.message}
            {...register('fullName')}
          />
          <AccountTextField
            id="register-email"
            type="email"
            label={t('account.companyEmail')}
            autoComplete="email"
            disabled={isSubmitting}
            error={errors.email?.message}
            {...register('email')}
          />
          <AccountPasswordField
            id="register-password"
            label={t('auth.password')}
            autoComplete="new-password"
            disabled={isSubmitting}
            hint={t('validation.passwordPolicy')}
            error={errors.password?.message}
            {...register('password')}
          />
          <AccountPasswordField
            id="register-confirm-password"
            label={t('account.confirmPassword')}
            autoComplete="new-password"
            disabled={isSubmitting}
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />
          <AccountFeedback message={feedback?.message} tone={feedback?.tone} />
          <Button type="submit" className="h-11 w-full" disabled={isSubmitting}>
            {isSubmitting ? <Spinner aria-hidden="true" /> : null}
            {t('account.registerAction')}
          </Button>
        </FieldGroup>
      </form>
    </AccountShell>
  )
}
