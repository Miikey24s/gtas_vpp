import { Languages, Moon, Printer, Sun } from 'lucide-react'
import { useTheme } from 'next-themes'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export function LanguageControl({ compact = false }: { compact?: boolean }) {
  const { i18n, t } = useTranslation()
  const currentLanguage = i18n.resolvedLanguage === 'en' ? 'en' : 'vi'

  useEffect(() => {
    document.documentElement.lang = currentLanguage
    window.localStorage.setItem('gtas-vpp-language', currentLanguage)
  }, [currentLanguage])

  return (
    <div
      className="bg-muted/60 border-border flex h-9 items-center rounded-md border p-0.5"
      role="group"
      aria-label={t('actions.changeLanguage')}
    >
      {!compact ? (
        <Languages
          className="text-muted-foreground mx-1 size-3.5"
          aria-hidden="true"
        />
      ) : null}
      {(['en', 'vi'] as const).map((language) => (
        <button
          key={language}
          type="button"
          className={cn(
            'focus-visible:ring-ring h-7 min-w-8 rounded-[4px] px-2 text-[11px] font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none',
            language === currentLanguage
              ? 'bg-background text-foreground shadow-sm'
              : 'text-muted-foreground hover:text-foreground',
          )}
          aria-pressed={language === currentLanguage}
          onClick={() => void i18n.changeLanguage(language)}
        >
          {language.toUpperCase()}
        </button>
      ))}
    </div>
  )
}

export function ThemeControl() {
  const { resolvedTheme, setTheme } = useTheme()
  const { t } = useTranslation()
  const isDark = resolvedTheme === 'dark'

  return (
    <Button
      type="button"
      variant="outline"
      size="icon-sm"
      className="size-9"
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      aria-label={t('actions.changeTheme')}
    >
      {isDark ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}
    </Button>
  )
}

export function PrintControl() {
  const { t } = useTranslation()

  return (
    <Button
      type="button"
      variant="outline"
      size="icon-sm"
      className="no-print size-9"
      onClick={() => window.print()}
      aria-label={t('actions.print')}
    >
      <Printer aria-hidden="true" />
    </Button>
  )
}
