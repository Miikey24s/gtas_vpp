import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <main className="login-grid grid min-h-svh place-items-center px-6 text-center">
      <div className="bg-card w-full max-w-md space-y-4 border p-8 shadow-sm">
        <p className="text-primary text-sm font-semibold">404</p>
        <h1 className="text-3xl font-semibold tracking-tight">
          {t('system.notFoundTitle')}
        </h1>
        <p className="text-muted-foreground text-sm">
          {t('system.notFoundDescription')}
        </p>
        <Button asChild>
          <Link to="/" viewTransition>
            {t('system.backHome')}
          </Link>
        </Button>
      </div>
    </main>
  )
}
