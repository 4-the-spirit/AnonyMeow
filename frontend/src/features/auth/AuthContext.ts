import { createContext } from 'react'
import type { UserResponse } from '@/features/users/types'
import type { AuthSession } from './authAdapter'

export type AuthStatus = 'loading' | 'anonymous' | 'unauthenticated' | 'profile-incomplete' | 'ready'

export interface AuthContextValue {
  status: AuthStatus
  user: UserResponse | null
  refetchUser: () => Promise<unknown>
  /** Creates a brand-new account and signs into it. */
  signUp: () => Promise<void>
  /** Signs in to an existing account by username; rejects if none exists with that name. */
  signInWithUsername: (username: string) => Promise<void>
  /** Adopts an already-finished session (e.g. from EmailAuthPage/ForgotPasswordPage's own
   * multi-step email+password+OTP flow, which doesn't fit signUp()/signInWithUsername()'s
   * one-shot shape) without minting a new one. */
  completeSession: (session: AuthSession) => Promise<void>
  /** Clears the current session, returning to the anonymous state. */
  signOut: () => void
  /** Dev-only escape hatch: mints a token for an arbitrary oid so testers can act as a
   * different user (e.g. a community moderator) in the same browser session. */
  devLoginAs: (oid?: string) => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
