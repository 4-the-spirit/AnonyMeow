const TOKEN_KEY = 'anonymeow.auth.token'
const OID_KEY = 'anonymeow.auth.oid'
const REFRESH_TOKEN_KEY = 'anonymeow.auth.refreshToken'

/**
 * Plain module (not a hook) so it's readable from lib/api/client.ts outside React,
 * e.g. before the first render or inside a non-component fetch wrapper.
 */
export const tokenStorage = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  },
  getOid(): string | null {
    return localStorage.getItem(OID_KEY)
  },
  /** Only ever set for the native-auth adapter — used to silently refresh a session on a 401. */
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  },
  set(token: string, oid: string, refreshToken?: string) {
    localStorage.setItem(TOKEN_KEY, token)
    localStorage.setItem(OID_KEY, oid)
    if (refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
    }
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(OID_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  },
}
