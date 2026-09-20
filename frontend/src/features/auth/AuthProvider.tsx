import { useEffect, useState, type ReactNode } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getMe } from '@/features/users/api'
import { tokenStorage } from '@/lib/auth/tokenStorage'
import { ApiError } from '@/lib/api/problemDetails'
import { createAuthAdapter, type AuthSession } from './authAdapter'
import { AuthContext, type AuthContextValue, type AuthStatus } from './AuthContext'

const authAdapter = createAuthAdapter()

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<AuthSession | null>(() => authAdapter.getStoredSession())
  const [retriedAfter401, setRetriedAfter401] = useState(false)

  const meQuery = useQuery({
    queryKey: ['users', 'me'],
    queryFn: getMe,
    enabled: session !== null,
    retry: false,
  })

  useEffect(() => {
    const error = meQuery.error
    if (session && error instanceof ApiError && error.status === 401 && !retriedAfter401) {
      setRetriedAfter401(true)
      authAdapter.refreshCurrentSession().then((next) => {
        setSession(next)
        queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
      })
    }
  }, [session, meQuery.error, retriedAfter401, queryClient])

  async function signUp() {
    const next = await authAdapter.signUp()
    setSession(next)
    setRetriedAfter401(false)
    await queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
  }

  async function signInWithUsername(username: string) {
    const next = await authAdapter.signInWithUsername(username)
    setSession(next)
    setRetriedAfter401(false)
    await queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
  }

  async function completeSession(next: AuthSession) {
    tokenStorage.set(next.token, next.oid, next.refreshToken)
    setSession(next)
    setRetriedAfter401(false)
    await queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
  }

  function signOut() {
    authAdapter.clear()
    setSession(null)
    setRetriedAfter401(false)
    queryClient.clear()
  }

  async function devLoginAs(oid?: string) {
    const next = await authAdapter.devMintToken(oid)
    setSession(next)
    setRetriedAfter401(false)
    await queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
  }

  let status: AuthStatus
  if (session === null) {
    status = 'anonymous'
  } else if (meQuery.isPending && meQuery.fetchStatus !== 'idle') {
    status = 'loading'
  } else if (meQuery.isSuccess) {
    status = 'ready'
  } else if (meQuery.error instanceof ApiError && meQuery.error.status === 403) {
    status = 'profile-incomplete'
  } else if (meQuery.error instanceof ApiError && meQuery.error.status === 401) {
    status = 'loading'
  } else if (meQuery.isError) {
    status = 'unauthenticated'
  } else {
    status = 'loading'
  }

  const value: AuthContextValue = {
    status,
    user: meQuery.data ?? null,
    refetchUser: meQuery.refetch,
    signUp,
    signInWithUsername,
    completeSession,
    signOut,
    devLoginAs,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
