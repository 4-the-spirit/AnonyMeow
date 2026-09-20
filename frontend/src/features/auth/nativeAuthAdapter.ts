import { tokenStorage } from '@/lib/auth/tokenStorage'
import { nativeAuthApi } from './nativeAuthApi'
import type { AuthAdapter, AuthSession } from './authAdapter'

const env = import.meta.env

/**
 * Set once the backend has real NativeAuth config wired up (see docs/azure-ciam-setup.md) — the
 * frontend itself needs no CIAM values at all anymore (client ID/tenant/scope all live
 * server-side only, see NativeAuthOptions in Program.cs), so this is just an on/off switch.
 */
export function isRealAuthConfigured(): boolean {
  return env.VITE_REAL_AUTH_ENABLED === 'true'
}

function toSession(tokens: {
  idToken: string
  refreshToken: string | null
  oid: string
}): AuthSession {
  tokenStorage.set(tokens.idToken, tokens.oid, tokens.refreshToken ?? undefined)
  return { token: tokens.idToken, oid: tokens.oid, refreshToken: tokens.refreshToken ?? undefined }
}

/**
 * Only getStoredSession/refreshCurrentSession/clear are actually used — sign-up/sign-in are
 * multi-step (email+password, then possibly an emailed code) and don't fit this interface's
 * one-shot shape, so EmailAuthPage/ForgotPasswordPage call nativeAuthApi directly and hand the
 * finished session to AuthContext's completeSession instead of going through this adapter.
 */
export const nativeAuthAdapter: AuthAdapter = {
  getStoredSession() {
    const token = tokenStorage.getToken()
    const oid = tokenStorage.getOid()
    if (!token || !oid) {
      return null
    }

    return { token, oid, refreshToken: tokenStorage.getRefreshToken() ?? undefined }
  },

  signUp() {
    return Promise.reject(new Error('signUp() is not supported for native auth — use the sign-up form.'))
  },

  signInWithUsername() {
    return Promise.reject(
      new Error('signInWithUsername() is not supported for native auth — use the sign-in form.')
    )
  },

  async refreshCurrentSession() {
    const refreshToken = tokenStorage.getRefreshToken()
    if (!refreshToken) {
      throw new Error('No refresh token available — sign in again.')
    }

    const tokens = await nativeAuthApi.refresh({ refreshToken })
    return toSession(tokens)
  },

  devMintToken() {
    return Promise.reject(new Error('devMintToken is not supported when using real native auth'))
  },

  clear() {
    tokenStorage.clear()
  },
}
