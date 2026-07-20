import { Eye, EyeOff } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { Field, FieldDescription, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'

type AccountFieldProps = Omit<React.ComponentProps<'input'>, 'id'> & {
  id: string
  label: string
  error?: string
  hint?: string
}

function describedBy(id: string, error?: string, hint?: string) {
  return [hint ? `${id}-hint` : null, error ? `${id}-error` : null]
    .filter(Boolean)
    .join(' ')
}

export function AccountTextField({
  id,
  label,
  error,
  hint,
  className,
  ...props
}: AccountFieldProps) {
  return (
    <Field data-invalid={Boolean(error)}>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Input
        id={id}
        className={cn('h-10', className)}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, error, hint) || undefined}
        {...props}
      />
      {hint ? (
        <FieldDescription id={`${id}-hint`} className="text-xs">
          {hint}
        </FieldDescription>
      ) : null}
      <p
        id={`${id}-error`}
        className="text-destructive min-h-5 text-xs"
        aria-live="polite"
      >
        {error}
      </p>
    </Field>
  )
}

export function AccountPasswordField({
  id,
  label,
  error,
  hint,
  className,
  ...props
}: AccountFieldProps) {
  const [visible, setVisible] = useState(false)
  const { t } = useTranslation()

  return (
    <Field data-invalid={Boolean(error)}>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <div className="relative">
        <Input
          id={id}
          type={visible ? 'text' : 'password'}
          className={cn('h-10 pr-11', className)}
          aria-invalid={Boolean(error)}
          aria-describedby={describedBy(id, error, hint) || undefined}
          {...props}
        />
        <button
          type="button"
          className="text-muted-foreground hover:text-foreground focus-visible:ring-ring absolute top-1/2 right-1 grid size-8 -translate-y-1/2 place-items-center rounded-[4px] focus-visible:ring-2 focus-visible:outline-none"
          onClick={() => setVisible((value) => !value)}
          aria-label={t(visible ? 'auth.hidePassword' : 'auth.showPassword')}
          aria-pressed={visible}
          disabled={props.disabled}
        >
          {visible ? (
            <EyeOff className="size-4" aria-hidden="true" />
          ) : (
            <Eye className="size-4" aria-hidden="true" />
          )}
        </button>
      </div>
      {hint ? (
        <FieldDescription id={`${id}-hint`} className="text-xs">
          {hint}
        </FieldDescription>
      ) : null}
      <p
        id={`${id}-error`}
        className="text-destructive min-h-5 text-xs"
        aria-live="polite"
      >
        {error}
      </p>
    </Field>
  )
}
