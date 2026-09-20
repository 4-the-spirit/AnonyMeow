import { apiFetch } from '@/lib/api/client'
import { tokenStorage } from '@/lib/auth/tokenStorage'
import { isRealAuthConfigured, nativeAuthAdapter } from './nativeAuthAdapter'

export interface AuthSession {
  token: string
  oid: string
  /** Only ever set by the native-auth adapter — used to silently refresh on a 401. */
  refreshToken?: string
}

export interface AuthAdapter {
  /** Reads a stored session without minting one — null means "no session" (anonymous visitor). */
  getStoredSession(): AuthSession | null
  /** Creates a brand-new account and stores its session — the "Sign up" action. */
  signUp(): Promise<AuthSession>
  /** Signs in to an existing account by username. Rejects with a 404 ApiError if none exists. */
  signInWithUsername(username: string): Promise<AuthSession>
  /** Re-mints a token for the current session's oid (e.g. after a stale-token 401). */
  refreshCurrentSession(): Promise<AuthSession>
  /** Dev-only escape hatch: mint a token for an arbitrary oid, or a new random identity if omitted. */
  devMintToken(oid?: string): Promise<AuthSession>
  clear(): void
}

async function mintDevToken(oid?: string): Promise<AuthSession> {
  const response = await apiFetch<AuthSession>('/api/dev/token', {
    method: 'POST',
    skipAuth: true,
    searchParams: oid ? { oid } : undefined,
  })
  tokenStorage.set(response.token, response.oid)
  return response
}

/**
 * Talks to the backend's dev-only `/api/dev/token` bypass (see
 * Execution Log/2026-07-13-dev-only-auth-bypass-for-local-testing.md). Selected by
 * `createAuthAdapter` whenever real auth isn't configured — see `nativeAuthAdapter` for the real
 * Entra External ID implementation of this same interface.
 */
export const devTokenAdapter: AuthAdapter = {
  getStoredSession() {
    const token = tokenStorage.getToken()
    const oid = tokenStorage.getOid()
    return token && oid ? { token, oid } : null
  },

  signUp() {
    return mintDevToken()
  },

  async signInWithUsername(username: string) {
    const response = await apiFetch<AuthSession>('/api/dev/token', {
      method: 'POST',
      skipAuth: true,
      searchParams: { username },
    })
    tokenStorage.set(response.token, response.oid)
    return response
  },

  refreshCurrentSession() {
    return mintDevToken(tokenStorage.getOid() ?? undefined)
  },

  devMintToken(oid?: string) {
    return mintDevToken(oid)
  },

  clear() {
    tokenStorage.clear()
  },
}

/**
 * Single factory choosing the active adapter: real native auth once the backend is configured
 * (see VITE_REAL_AUTH_ENABLED in .env.example and docs/azure-ciam-setup.md), the dev bypass
 * otherwise. Neither AuthProvider nor any consuming component needs to know which one is in play.
 */
export function createAuthAdapter(): AuthAdapter {
  return isRealAuthConfigured() ? nativeAuthAdapter : devTokenAdapter
}
