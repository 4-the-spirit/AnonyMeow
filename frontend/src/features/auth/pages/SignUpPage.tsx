import { isRealAuthConfigured } from '@/features/auth/nativeAuthAdapter'
import { AuthPage } from './AuthPage'
import { EmailAuthPage } from './EmailAuthPage'

export function SignUpPage() {
  return isRealAuthConfigured() ? <EmailAuthPage defaultTab="signUp" /> : <AuthPage defaultTab="signUp" />
}
