import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bell,
  CheckCheck,
  CircleCheckBig,
  CircleX,
  Clock3,
  Info,
  RefreshCw,
  UserRoundPlus,
} from 'lucide-react'
import { useCallback, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiNotifications,
  postApiNotificationsByIdRead,
  postApiNotificationsReadAll,
  type NotificationResDto,
} from '@/api/generated'
import { Button } from '@/components/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { useNotificationRealtime } from './notification-realtime'

const notificationQueryKey = ['notifications', 'inbox'] as const

function getNotificationIcon(type?: string | null) {
  switch (type) {
    case 'additional-order.approved':
      return CircleCheckBig
    case 'additional-order.rejected':
      return CircleX
    case 'additional-order.pending':
      return Clock3
    case 'account.registration.pending':
      return UserRoundPlus
    default:
      return Info
  }
}

function resolveReactRoute(route?: string | null) {
  if (!route?.startsWith('/')) return null
  const target = new URL(route, window.location.origin)
  const path = target.pathname.toLowerCase()

  if (path.startsWith('/app/')) return `${target.pathname}${target.search}`
  if (path === '/account/login') return '/login'
  if (path === '/dashboard' && target.searchParams.get('tab') === '1') {
    return '/app/orders'
  }
  return null
}

function formatNotificationTime(value: string | undefined, language: string) {
  if (!value || !Number.isFinite(Date.parse(value))) return '—'
  return new Intl.DateTimeFormat(language === 'en' ? 'en-GB' : 'vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function NotificationCenter() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [actionError, setActionError] = useState(false)

  const inboxQuery = useQuery({
    queryKey: notificationQueryKey,
    queryFn: async () => {
      const result = await getApiNotifications({
        query: { skip: 0, take: 20, unreadOnly: false },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    refetchInterval: 60_000,
  })

  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: notificationQueryKey })
  }, [queryClient])
  useNotificationRealtime(refresh)

  const markRead = useMutation({
    mutationFn: async (id: string) => {
      const result = await postApiNotificationsByIdRead({ path: { id } })
      if (!result.response?.ok) {
        throw toApiRequestError(result.error, result.response)
      }
    },
    onSuccess: refresh,
  })
  const markAllRead = useMutation({
    mutationFn: async () => {
      const result = await postApiNotificationsReadAll()
      if (!result.response?.ok) {
        throw toApiRequestError(result.error, result.response)
      }
      return result.data
    },
    onSuccess: refresh,
  })

  const items = useMemo(() => inboxQuery.data?.items ?? [], [inboxQuery.data])
  const unreadCount = inboxQuery.data?.unreadCount ?? 0
  const unreadLabel = t('notifications.unreadCount', { count: unreadCount })

  const openNotification = async (item: NotificationResDto) => {
    setActionError(false)
    try {
      if (!item.isRead && item.id) await markRead.mutateAsync(item.id)
    } catch {
      setActionError(true)
      return
    }

    setOpen(false)
    const route = resolveReactRoute(item.route)
    if (route) navigate(route, { viewTransition: true })
  }

  const markAll = async () => {
    setActionError(false)
    try {
      await markAllRead.mutateAsync()
    } catch {
      setActionError(true)
    }
  }

  return (
    <Popover
      open={open}
      onOpenChange={(nextOpen: boolean) => {
        setOpen(nextOpen)
        setActionError(false)
        if (nextOpen) refresh()
      }}
    >
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          size="icon-lg"
          className="no-print relative size-9"
          aria-label={
            unreadCount > 0
              ? t('notifications.openWithUnread', { count: unreadCount })
              : t('notifications.open')
          }
        >
          <Bell aria-hidden="true" />
          {unreadCount > 0 ? (
            <span
              className="bg-primary text-primary-foreground absolute -top-1 -right-1 grid min-w-4 place-items-center rounded-full px-1 text-[9px] leading-4 font-bold tabular-nums"
              aria-hidden="true"
            >
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          ) : null}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        sideOffset={8}
        className="w-[min(24rem,calc(100vw-2rem))] gap-0 overflow-hidden p-0"
      >
        <header className="border-border flex items-start justify-between gap-4 border-b px-4 py-3">
          <div>
            <h2 className="text-sm font-semibold">
              {t('notifications.title')}
            </h2>
            <p className="text-muted-foreground mt-0.5 text-xs">
              {unreadLabel}
            </p>
          </div>
          {unreadCount > 0 ? (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => void markAll()}
              disabled={markAllRead.isPending}
            >
              {markAllRead.isPending ? (
                <Spinner aria-hidden="true" />
              ) : (
                <CheckCheck aria-hidden="true" />
              )}
              {t('notifications.markAllRead')}
            </Button>
          ) : null}
        </header>

        {actionError ? (
          <p
            className="text-destructive border-border border-b px-4 py-2 text-xs"
            role="alert"
          >
            {t('notifications.actionError')}
          </p>
        ) : null}

        <ScrollArea className="h-[min(28rem,60vh)]">
          {inboxQuery.isLoading && items.length === 0 ? (
            <div
              className="text-muted-foreground grid h-40 place-items-center text-sm"
              role="status"
            >
              <Spinner aria-hidden="true" />
              <span className="sr-only">{t('system.loading')}</span>
            </div>
          ) : null}

          {inboxQuery.isError && items.length === 0 ? (
            <div className="px-6 py-10 text-center" role="alert">
              <CircleX
                className="text-destructive mx-auto size-6"
                aria-hidden="true"
              />
              <p className="mt-3 text-sm font-medium">
                {t('notifications.loadErrorTitle')}
              </p>
              <p className="text-muted-foreground mt-1 text-xs leading-5">
                {t('notifications.loadErrorDescription')}
              </p>
            </div>
          ) : null}

          {!inboxQuery.isLoading &&
          !inboxQuery.isError &&
          items.length === 0 ? (
            <div className="px-6 py-10 text-center">
              <Bell
                className="text-muted-foreground mx-auto size-6"
                aria-hidden="true"
              />
              <p className="mt-3 text-sm font-medium">
                {t('notifications.emptyTitle')}
              </p>
              <p className="text-muted-foreground mt-1 text-xs leading-5">
                {t('notifications.emptyDescription')}
              </p>
            </div>
          ) : null}

          {items.length > 0 ? (
            <div className="divide-border divide-y" aria-live="polite">
              {items.map((item, index) => {
                const Icon = getNotificationIcon(item.type)
                return (
                  <button
                    key={item.id ?? index}
                    type="button"
                    className={cn(
                      'hover:bg-muted/60 focus-visible:ring-ring relative flex w-full gap-3 px-4 py-3 text-left transition-colors focus-visible:ring-2 focus-visible:outline-none',
                      !item.isRead && 'bg-primary/[0.045]',
                    )}
                    onClick={() => void openNotification(item)}
                    disabled={markRead.isPending}
                  >
                    <span className="bg-muted mt-0.5 grid size-8 shrink-0 place-items-center rounded-md">
                      <Icon
                        className="text-muted-foreground size-4"
                        aria-hidden="true"
                      />
                    </span>
                    <span className="min-w-0 flex-1">
                      <strong className="block text-sm leading-5 font-medium">
                        {item.title || t('notifications.defaultTitle')}
                      </strong>
                      <span className="text-muted-foreground mt-0.5 block text-xs leading-5">
                        {item.message}
                      </span>
                      <time
                        className="text-muted-foreground mt-1 block text-[11px] tabular-nums"
                        dateTime={item.createdAt}
                      >
                        {formatNotificationTime(
                          item.createdAt,
                          i18n.resolvedLanguage ?? 'vi',
                        )}
                      </time>
                    </span>
                    {!item.isRead ? (
                      <span
                        className="bg-primary mt-2 size-2 shrink-0 rounded-full"
                        aria-label={t('notifications.unread')}
                      />
                    ) : null}
                  </button>
                )
              })}
            </div>
          ) : null}
        </ScrollArea>

        <footer className="border-border flex justify-end border-t px-3 py-2">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => void inboxQuery.refetch()}
            disabled={inboxQuery.isFetching}
          >
            {inboxQuery.isFetching ? (
              <Spinner aria-hidden="true" />
            ) : (
              <RefreshCw aria-hidden="true" />
            )}
            {t('actions.refresh')}
          </Button>
        </footer>
      </PopoverContent>
    </Popover>
  )
}
