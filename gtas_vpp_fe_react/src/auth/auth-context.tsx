import { useQueryClient } from '@tanstack/react-query'
import {
  type PropsWithChildren,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiAuthMe,
  getApiAuthMePermissions,
  postApiAuthLogin,
  postApiAuthLogout,
  type AuthenticationLoginRequest,
  type CurrentUserResDto,
  type PermissionSnapshotResDto,
} from '@/api/generated'
import {
  AUTH_SESSION_CHANGED_EVENT,
  readAuthSession,
  removeAuthSession,
  saveAuthSession,
} from '@/auth/auth-session'
import {
  AuthContext,
  type AuthContextValue,
  type AuthStatus,
} from '@/auth/use-auth'

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<CurrentUserResDto | null>(null)
  const [permissionSnapshot, setPermissionSnapshot] =
    useState<PermissionSnapshotResDto | null>(null)

  const setAnonymous = useCallback(() => {
    setUser(null)
    setPermissionSnapshot(null)
    setStatus('anonymous')
    queryClient.clear()
  }, [queryClient])

  const refresh = useCallback(async () => {
    if (!readAuthSession()) {
      setAnonymous()
      return 'anonymous' as const
    }

    setStatus('loading')
    const userResult = await getApiAuthMe()

    if (!userResult.data) {
      removeAuthSession()
      setAnonymous()
      throw toApiRequestError(userResult.error, userResult.response)
    }

    setUser(userResult.data)
    if (userResult.data.mustChangePassword) {
      setPermissionSnapshot(null)
      setStatus('password-change-required')
      return 'password-change-required' as const
    }

    const permissionsResult = await getApiAuthMePermissions()
    if (!permissionsResult.data) {
      removeAuthSession()
      setAnonymous()
      throw toApiRequestError(
        permissionsResult.error,
        permissionsResult.response,
      )
    }

    setPermissionSnapshot(permissionsResult.data)
    setStatus('authenticated')
    return 'authenticated' as const
  }, [setAnonymous])

  useEffect(() => {
    void refresh().catch(() => undefined)
  }, [refresh])

  useEffect(() => {
    const handleSessionChanged = () => {
      if (!readAuthSession()) setAnonymous()
    }
    const handleFocus = () => {
      if (
        (status === 'authenticated' || status === 'password-change-required') &&
        !readAuthSession()
      ) {
        setAnonymous()
      }
    }

    window.addEventListener(AUTH_SESSION_CHANGED_EVENT, handleSessionChanged)
    window.addEventListener('focus', handleFocus)
    return () => {
      window.removeEventListener(
        AUTH_SESSION_CHANGED_EVENT,
        handleSessionChanged,
      )
      window.removeEventListener('focus', handleFocus)
    }
  }, [setAnonymous, status])

  const login = useCallback(
    async (credentials: AuthenticationLoginRequest) => {
      const result = await postApiAuthLogin({ body: credentials })
      if (!result.data) {
        throw toApiRequestError(result.error, result.response)
      }
      saveAuthSession(result.data)
      try {
        return await refresh()
      } catch (error) {
        removeAuthSession()
        throw error
      }
    },
    [refresh],
  )

  const logout = useCallback(async () => {
    try {
      if (readAuthSession()) await postApiAuthLogout()
    } finally {
      removeAuthSession()
      setAnonymous()
    }
  }, [setAnonymous])

  const grantedPermissions = useMemo(
    () => new Set(permissionSnapshot?.permissions ?? []),
    [permissionSnapshot?.permissions],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      permissionSnapshot,
      hasPermission: (permission) => grantedPermissions.has(permission),
      login,
      logout,
      refresh,
    }),
    [
      grantedPermissions,
      login,
      logout,
      permissionSnapshot,
      refresh,
      status,
      user,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
