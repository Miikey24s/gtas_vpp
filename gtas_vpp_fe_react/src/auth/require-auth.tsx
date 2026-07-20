import { Navigate, Outlet, useLocation } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { RouteFallback } from '@/app/routes/route-fallback'

export function RequireAuth() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <RouteFallback />
  if (status === 'anonymous') {
    const returnUrl = `${location.pathname}${location.search}`
    return (
      <Navigate
        to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`}
        replace
      />
    )
  }
  if (status === 'password-change-required') {
    return <Navigate to="/change-password?required=1" replace />
  }

  return <Outlet />
}

export function RequireSession() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <RouteFallback />
  if (status === 'anonymous') {
    const returnUrl = `${location.pathname}${location.search}`
    return (
      <Navigate
        to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`}
        replace
      />
    )
  }

  return <Outlet />
}
