import { describe, expect, it, afterEach, beforeEach, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { tokenStorage } from '@/lib/auth/tokenStorage'
import { isRealAuthConfigured, nativeAuthAdapter } from './nativeAuthAdapter'

const BASE_URL = 'https://test.local'

describe('isRealAuthConfigured', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('is false when VITE_REAL_AUTH_ENABLED is unset', () => {
    vi.stubEnv('VITE_REAL_AUTH_ENABLED', undefined)
    expect(isRealAuthConfigured()).toBe(false)
  })

  it('is true only when VITE_REAL_AUTH_ENABLED is exactly "true"', () => {
    vi.stubEnv('VITE_REAL_AUTH_ENABLED', 'true')
    expect(isRealAuthConfigured()).toBe(true)

    vi.stubEnv('VITE_REAL_AUTH_ENABLED', 'yes')
    expect(isRealAuthConfigured()).toBe(false)
  })
})

describe('nativeAuthAdapter', () => {
  beforeEach(() => {
    tokenStorage.clear()
  })

  afterEach(() => {
    tokenStorage.clear()
  })

  it('getStoredSession returns null when nothing is stored', () => {
    expect(nativeAuthAdapter.getStoredSession()).toBeNull()
  })

  it('getStoredSession returns the stored token/oid/refreshToken', () => {
    tokenStorage.set('stored-token', 'stored-oid', 'stored-refresh')

    expect(nativeAuthAdapter.getStoredSession()).toEqual({
      token: 'stored-token',
      oid: 'stored-oid',
      refreshToken: 'stored-refresh',
    })
  })

  it('signUp/signInWithUsername/devMintToken reject — multi-step flows use the dedicated forms', async () => {
    await expect(nativeAuthAdapter.signUp()).rejects.toThrow()
    await expect(nativeAuthAdapter.signInWithUsername('someone')).rejects.toThrow()
    await expect(nativeAuthAdapter.devMintToken()).rejects.toThrow()
  })

  it('refreshCurrentSession throws when no refresh token is stored', async () => {
    await expect(nativeAuthAdapter.refreshCurrentSession()).rejects.toThrow(/no refresh token/i)
  })

  it('refreshCurrentSession exchanges the stored refresh token and persists the new session', async () => {
    tokenStorage.set('old-token', 'old-oid', 'old-refresh')
    server.use(
      http.post(`${BASE_URL}/api/auth/native/refresh`, async ({ request }) => {
        const body = (await request.json()) as { refreshToken: string }
        expect(body.refreshToken).toBe('old-refresh')
        return HttpResponse.json({
          accessToken: 'new-token',
          idToken: 'new-id-token',
          refreshToken: 'new-refresh',
          oid: 'old-oid',
          expiresIn: 3600,
        })
      })
    )

    const session = await nativeAuthAdapter.refreshCurrentSession()

    expect(session).toEqual({ token: 'new-id-token', oid: 'old-oid', refreshToken: 'new-refresh' })
    expect(tokenStorage.getToken()).toBe('new-id-token')
    expect(tokenStorage.getRefreshToken()).toBe('new-refresh')
  })

  it('clear removes the stored session', () => {
    tokenStorage.set('token', 'oid', 'refresh')

    nativeAuthAdapter.clear()

    expect(tokenStorage.getToken()).toBeNull()
    expect(tokenStorage.getRefreshToken()).toBeNull()
  })
})
