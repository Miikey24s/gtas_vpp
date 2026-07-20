import { CheckCircle2, CircleAlert, Info, TriangleAlert } from 'lucide-react'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { cn } from '@/lib/utils'

type AccountFeedbackProps = {
  message?: string | null
  tone?: 'error' | 'success' | 'warning' | 'info'
}

const icons = {
  error: CircleAlert,
  success: CheckCircle2,
  warning: TriangleAlert,
  info: Info,
}

export function AccountFeedback({
  message,
  tone = 'info',
}: AccountFeedbackProps) {
  if (!message) return null

  const Icon = icons[tone]
  return (
    <Alert
      variant={tone === 'error' ? 'destructive' : 'default'}
      role={tone === 'error' ? 'alert' : 'status'}
      className={cn(
        'px-3 py-3',
        tone === 'success' &&
          'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200',
        tone === 'warning' &&
          'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200',
      )}
    >
      <Icon aria-hidden="true" />
      <AlertDescription className="text-current">{message}</AlertDescription>
    </Alert>
  )
}
