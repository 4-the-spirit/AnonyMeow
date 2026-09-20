import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useAuth } from './useAuth'

/**
 * Gate for interactive actions (vote/react/comment/join/post/message) that live on otherwise
 * publicly-viewable pages — RootGate lets anonymous visitors reach these pages at all, so each
 * action call site is what actually enforces "sign in to interact".
 */
export function useRequireAuth() {
  const { status } = useAuth()
  const navigate = useNavigate()
  const { t } = useTranslation('auth')

  function requireAuth(action: () => void) {
    if (status === 'ready') {
      action()
      return
    }
    if (status === 'anonymous') {
      toast.error(t('requireAuth.toast'))
      navigate('/signin')
    }
  }

  return { requireAuth, isAnonymous: status === 'anonymous' }
}
