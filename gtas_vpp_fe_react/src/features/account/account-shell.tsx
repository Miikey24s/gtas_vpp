import { main as MotionMain } from 'motion/react-m'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { BrandMark } from '@/components/app/brand-mark'
import { LanguageControl, ThemeControl } from '@/components/app/header-controls'
import { cn } from '@/lib/utils'
import { surfaceEnter } from '@/lib/motion'

type AccountShellProps = {
  title: string
  eyebrow?: string
  description?: string
  children: ReactNode
  footer?: ReactNode
  className?: string
  contentClassName?: string
}

export function AccountShell({
  title,
  eyebrow,
  description,
  children,
  footer,
  className,
  contentClassName,
}: AccountShellProps) {
  const { t } = useTranslation()

  return (
    <div className="login-grid min-h-svh px-4 py-8 sm:grid sm:place-items-center sm:px-6">
      <MotionMain
        data-slot="account-surface"
        {...surfaceEnter}
        className={cn(
          'bg-card text-card-foreground mx-auto w-full max-w-[30rem] border p-6 shadow-[0_18px_60px_rgba(23,43,77,0.10)] sm:p-9',
          className,
        )}
      >
        <header className="flex items-start justify-between gap-4">
          <div className="flex items-center gap-3">
            <BrandMark />
            <p className="text-sm font-semibold tracking-tight">GTAS VPP</p>
          </div>
          <div className="flex items-center gap-2">
            <LanguageControl compact />
            <ThemeControl />
          </div>
        </header>

        <section className="mt-10" aria-labelledby="account-page-title">
          <div className="space-y-2">
            <p className="text-primary text-xs font-semibold tracking-[0.16em] uppercase">
              {eyebrow ?? t('auth.secureAccess')}
            </p>
            <h1
              id="account-page-title"
              className="text-4xl font-semibold tracking-[-0.04em] sm:text-[2.75rem]"
            >
              {title}
            </h1>
            {description ? (
              <p className="text-muted-foreground max-w-prose text-sm leading-6">
                {description}
              </p>
            ) : null}
          </div>

          <div className={cn('mt-8', contentClassName)}>{children}</div>
        </section>

        {footer ? <footer className="mt-6">{footer}</footer> : null}
      </MotionMain>
    </div>
  )
}
