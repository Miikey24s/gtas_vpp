import type { AuthenticationResultDto } from '@/api/generated'

const SESSION_KEY = 'gtas-vpp-react-session-v1'
export const AUTH_SESSION_CHANGED_EVENT = 'gtas:auth-session-changed'

type StoredAuthSession = {
  accessToken: string
  expiresAtUtc: string
}

function isStoredAuthSession(value: unknown): value is StoredAuthSession {
  if (!value || typeof value !== 'object') return false

  const candidate = value as Partial<StoredAuthSession>
  return (
    typeof candidate.accessToken === 'string' &&
    candidate.accessToken.length > 0 &&
    typeof candidate.expiresAtUtc === 'string' &&
    Number.isFinite(Date.parse(candidate.expiresAtUtc))
  )
}

export function readAuthSession(): StoredAuthSession | null {
  try {
    const raw = window.sessionStorage.getItem(SESSION_KEY)
    if (!raw) return null

    const session: unknown = JSON.parse(raw)
    if (
      !isStoredAuthSession(session) ||
      Date.parse(session.expiresAtUtc) <= Date.now()
    ) {
      clearAuthSession()
      return null
    }

    return session
  } catch {
    clearAuthSession()
    return null
  }
}

export function saveAuthSession(result: AuthenticationResultDto) {
  if (!result.accessToken || !result.accessTokenExpiresAtUtc) {
    throw new Error(
      'The authentication response did not include a valid session.',
    )
  }

  const session: StoredAuthSession = {
    accessToken: result.accessToken,
    expiresAtUtc: result.accessTokenExpiresAtUtc,
  }

  window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(session))
  notifyAuthSessionChanged()
}

export function clearAuthSession() {
  window.sessionStorage.removeItem(SESSION_KEY)
}

export function getAccessToken() {
  return readAuthSession()?.accessToken
}

export function notifyAuthSessionChanged() {
  window.dispatchEvent(new Event(AUTH_SESSION_CHANGED_EVENT))
}

export function removeAuthSession() {
  clearAuthSession()
  notifyAuthSessionChanged()
}
