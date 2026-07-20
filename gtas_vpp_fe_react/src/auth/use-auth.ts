import { createContext, useContext } from 'react'

import type {
  AuthenticationLoginRequest,
  CurrentUserResDto,
  PermissionSnapshotResDto,
} from '@/api/generated'

export type AuthStatus =
  'loading' | 'authenticated' | 'password-change-required' | 'anonymous'

export type AuthContextValue = {
  status: AuthStatus
  user: CurrentUserResDto | null
  permissionSnapshot: PermissionSnapshotResDto | null
  hasPermission: (permission: string) => boolean
  login: (credentials: AuthenticationLoginRequest) => Promise<AuthStatus>
  logout: () => Promise<void>
  refresh: () => Promise<AuthStatus>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider.')
  return context
}
