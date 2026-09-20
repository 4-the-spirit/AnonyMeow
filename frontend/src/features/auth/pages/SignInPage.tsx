import { isRealAuthConfigured } from '@/features/auth/nativeAuthAdapter'
import { AuthPage } from './AuthPage'
import { EmailAuthPage } from './EmailAuthPage'

export function SignInPage() {
  return isRealAuthConfigured() ? <EmailAuthPage defaultTab="signIn" /> : <AuthPage defaultTab="signIn" />
}
