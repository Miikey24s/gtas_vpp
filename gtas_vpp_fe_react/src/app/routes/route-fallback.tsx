import { useTranslation } from 'react-i18next'

export function RouteFallback() {
  const { t } = useTranslation()

  return (
    <div
      className="grid min-h-svh place-items-center"
      role="status"
      aria-label={t('system.loading')}
    >
      <span className="text-muted-foreground animate-pulse text-sm font-medium">
        GTAS VPP
      </span>
    </div>
  )
}
