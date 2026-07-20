import { cn } from '@/lib/utils'

export function BrandMark({ className }: { className?: string }) {
  return (
    <span
      className={cn(
        'relative grid size-8 shrink-0 place-items-center overflow-hidden rounded-md bg-[#0369a1] text-white shadow-sm',
        className,
      )}
      aria-hidden="true"
    >
      <svg viewBox="0 0 32 32" className="size-8" role="img">
        <path d="M6 7h5l5 13L21 7h5l-8 19h-4L6 7Z" fill="currentColor" />
        <path d="M21 7h5l-2 4h-5l2-4Z" fill="#2dd4bf" />
      </svg>
    </span>
  )
}
