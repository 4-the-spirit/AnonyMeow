import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'

/**
 * Route guard for pages that only make sense for a signed-in user (messages, notifications,
 * create-post/community, settings, friend requests, admin/mod views). RootGate itself lets
 * anonymous visitors reach the app at all — this is what actually redirects them to sign in
 * for these specific routes, preserving where they were headed via `from`.
 */
export function RequireAuth() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'anonymous') {
    return <Navigate to="/signin" state={{ from: location }} replace />
  }

  return <Outlet />
}
