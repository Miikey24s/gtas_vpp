import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const accessTabs = [
  ['users', 'access.tabs.users'],
  ['groups', 'access.tabs.groups'],
  ['permissions', 'access.tabs.permissions'],
] as const

export type AccessSection = (typeof accessTabs)[number][0]

export function AccessTabs({ active }: { active: AccessSection }) {
  const { t } = useTranslation()

  return (
    <nav
      aria-label={t('access.tabs.label')}
      className="border-border mt-6 overflow-x-auto border-b"
    >
      <div className="flex min-w-max gap-1">
        {accessTabs.map(([slug, labelKey]) => (
          <Button
            key={slug}
            asChild
            variant="ghost"
            className={cn(
              'rounded-none border-b-2 border-transparent px-4',
              active === slug && 'border-primary text-primary bg-muted/40',
            )}
          >
            <Link to={`/app/access/${slug}`} viewTransition>
              {t(labelKey)}
            </Link>
          </Button>
        ))}
      </div>
    </nav>
  )
}
