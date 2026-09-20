import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/features/auth/useAuth'
import { CompleteProfilePage } from '@/features/auth/pages/CompleteProfilePage'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'

/**
 * Gates the whole app on auth status: full-page spinner while loading, forces the
 * complete-profile screen while a signed-in user's username isn't set yet (regardless of the
 * requested route). Anonymous visitors (no session at all) and fully-ready users both fall
 * through to the real app — public content is viewable without an account; individual
 * interactive components/routes gate themselves via useRequireAuth.
 */
export function RootGate() {
  const { t } = useTranslation('common')
  const { status } = useAuth()

  if (status === 'loading') {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner />
      </div>
    )
  }

  if (status === 'unauthenticated') {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <ErrorState
          title={t('authError.title')}
          description={t('authError.description')}
        />
      </div>
    )
  }

  if (status === 'profile-incomplete') {
    return <CompleteProfilePage />
  }

  return <Outlet />
}
