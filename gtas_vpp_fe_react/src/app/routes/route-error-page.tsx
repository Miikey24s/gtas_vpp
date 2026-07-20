import { CircleAlert } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { isRouteErrorResponse, Link, useRouteError } from 'react-router-dom'

import { Button } from '@/components/ui/button'

export function RouteErrorPage() {
  const { t } = useTranslation()
  const error = useRouteError()
  const notFound = isRouteErrorResponse(error) && error.status === 404

  return (
    <main className="login-grid grid min-h-svh place-items-center px-6 text-center">
      <div className="bg-card w-full max-w-md space-y-4 border p-8 shadow-sm">
        <CircleAlert
          className="text-destructive mx-auto size-8"
          aria-hidden="true"
        />
        <h1 className="text-2xl font-semibold tracking-tight">
          {t(notFound ? 'system.notFoundTitle' : 'system.errorTitle')}
        </h1>
        <p className="text-muted-foreground text-sm">
          {t(
            notFound ? 'system.notFoundDescription' : 'system.errorDescription',
          )}
        </p>
        <div className="flex justify-center gap-2">
          <Button type="button" onClick={() => window.location.reload()}>
            {t('actions.retry')}
          </Button>
          <Button asChild variant="outline">
            <Link to="/" viewTransition>
              {t('system.backHome')}
            </Link>
          </Button>
        </div>
      </div>
    </main>
  )
}
