import { client } from '@/api/generated/client.gen'
import {
  antiforgeryHeaderName,
  isCookieSessionEnabled,
  readAntiforgeryToken,
} from '@/auth/auth-mode'
import { getAccessToken, removeAuthSession } from '@/auth/auth-session'
import { apiBaseUrl } from '@/lib/api-url'
import { i18n } from '@/lib/i18n'

let configured = false

export function configureApiClient() {
  if (configured) return
  configured = true

  client.setConfig({
    baseUrl: apiBaseUrl,
    credentials: 'include',
    responseStyle: 'fields',
  })

  client.interceptors.request.use((request) => {
    const accessToken = getAccessToken()
    if (accessToken) {
      request.headers.set('Authorization', `Bearer ${accessToken}`)
    }
    if (
      isCookieSessionEnabled() &&
      !['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(
        request.method.toUpperCase(),
      )
    ) {
      const antiforgeryToken = readAntiforgeryToken()
      if (antiforgeryToken) {
        request.headers.set(antiforgeryHeaderName, antiforgeryToken)
      }
    }
    request.headers.set('Accept-Language', i18n.resolvedLanguage ?? 'vi')
    return request
  })

  client.interceptors.response.use((response, request) => {
    const path = new URL(request.url).pathname.toLowerCase()
    if (response.status === 401 && path !== '/api/auth/login') {
      removeAuthSession()
    }
    return response
  })
}
