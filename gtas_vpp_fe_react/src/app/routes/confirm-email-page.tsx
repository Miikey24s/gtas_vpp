import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'

import { getApiAccountConfirmEmail } from '@/api/generated'
import { Spinner } from '@/components/ui/spinner'
import { requireApiSuccess } from '@/features/account/account-api'
import { getAccountErrorMessage } from '@/features/account/account-errors'
import { AccountFeedback } from '@/features/account/account-feedback'
import { AccountShell } from '@/features/account/account-shell'

type State = { status: 'loading' | 'success' | 'error'; message: string }

export function ConfirmEmailPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const userId = Number(searchParams.get('userId'))
  const token = searchParams.get('token')?.trim() ?? ''
  const started = useRef(false)
  const [state, setState] = useState<State>({
    status: 'loading',
    message: t('account.confirmChecking'),
  })

  useEffect(() => {
    if (started.current) return
    started.current = true

    if (!Number.isInteger(userId) || userId <= 0 || !token) {
      setState({
        status: 'error',
        message: t('account.errors.confirmationInvalid'),
      })
      return
    }

    void getApiAccountConfirmEmail({ query: { userId, token } })
      .then((result) => {
        requireApiSuccess(result)
        setState({ status: 'success', message: t('account.confirmSuccess') })
      })
      .catch((error) => {
        setState({
          status: 'error',
          message: getAccountErrorMessage(
            error,
            t,
            'account.errors.confirmationInvalid',
          ),
        })
      })
  }, [t, token, userId])

  return (
    <AccountShell
      title={t('account.confirmTitle')}
      footer={
        <Link
          className="text-primary text-sm hover:underline"
          to="/login"
          viewTransition
        >
          ← {t('account.goToLogin')}
        </Link>
      }
    >
      {state.status === 'loading' ? (
        <div
          className="text-muted-foreground flex items-center gap-3 text-sm"
          role="status"
        >
          <Spinner aria-hidden="true" />
          {state.message}
        </div>
      ) : (
        <AccountFeedback
          message={state.message}
          tone={state.status === 'success' ? 'success' : 'error'}
        />
      )}
    </AccountShell>
  )
}
