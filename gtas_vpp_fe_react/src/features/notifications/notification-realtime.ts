import {
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr'
import { useEffect } from 'react'

import { getAccessToken } from '@/auth/auth-session'
import { isCookieSessionEnabled, readAntiforgeryToken } from '@/auth/auth-mode'
import { toApiUrl } from '@/lib/api-url'

const realtimeEnabled =
  import.meta.env.VITE_NOTIFICATIONS_REALTIME?.toLowerCase() === 'true'

export function useNotificationRealtime(onChanged: () => void) {
  useEffect(() => {
    if (!realtimeEnabled) return

    const connection = new HubConnectionBuilder()
      .withUrl(toApiUrl('/hubs/notifications'), {
        accessTokenFactory: () => getAccessToken() ?? '',
        withCredentials: true,
        headers: isCookieSessionEnabled()
          ? { 'X-XSRF-TOKEN': readAntiforgeryToken() ?? '' }
          : undefined,
        transport:
          HttpTransportType.WebSockets |
          HttpTransportType.ServerSentEvents |
          HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2_000, 10_000, 30_000])
      .configureLogging(LogLevel.None)
      .build()

    connection.on('NotificationsChanged', onChanged)
    void connection.start().catch(() => undefined)

    return () => {
      connection.off('NotificationsChanged', onChanged)
      void connection.stop()
    }
  }, [onChanged])
}
