import type { ReactNode } from 'react'
import { describe, expect, it, afterEach } from 'vitest'
import { act, renderHook, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { createTestQueryClient } from '@/test/test-utils'
import { tokenStorage } from '@/lib/auth/tokenStorage'
import { AuthProvider } from './AuthProvider'
import { useAuth } from './useAuth'

const BASE_URL = 'https://test.local'

function renderAuth() {
  const queryClient = createTestQueryClient()
  function wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    )
  }
  return renderHook(() => useAuth(), { wrapper })
}

describe('AuthProvider', () => {
  afterEach(() => {
    tokenStorage.clear()
  })

  it('starts anonymous when no session is stored, without minting one', async () => {
    tokenStorage.clear()
    let tokenMinted = false
    server.use(
      http.post(`${BASE_URL}/api/dev/token`, () => {
        tokenMinted = true
        return HttpResponse.json({ oid: 'should-not-be-used', token: 'should-not-be-used' })
      })
    )

    const { result } = renderAuth()

    expect(result.current.status).toBe('anonymous')
    expect(result.current.user).toBeNull()
    expect(tokenMinted).toBe(false)
  })

  it('signUp mints a new session and reaches profile-incomplete for a brand-new account', async () => {
    tokenStorage.clear()
    server.use(
      http.post(`${BASE_URL}/api/dev/token`, () => HttpResponse.json({ oid: 'new-oid', token: 'new-token' })),
      http.get(`${BASE_URL}/api/users/me`, () =>
        HttpResponse.json({ title: 'Profile incomplete', status: 403 }, { status: 403 })
      )
    )

    const { result } = renderAuth()
    expect(result.current.status).toBe('anonymous')

    await act(() => result.current.signUp())

    await waitFor(() => expect(result.current.status).toBe('profile-incomplete'))
    expect(tokenStorage.getToken()).toBe('new-token')
  })

  it('signInWithUsername signs into an existing ready account', async () => {
    tokenStorage.clear()
    server.use(
      http.post(`${BASE_URL}/api/dev/token`, () => HttpResponse.json({ oid: 'existing-oid', token: 'existing-token' })),
      http.get(`${BASE_URL}/api/users/me`, () =>
        HttpResponse.json({
          username: 'existinguser',
          displayName: 'Existing User',
          avatarSeed: 'seed-1',
          karma: 0,
          friendListVisibility: 'Everyone',
          createdAtUtc: '2026-01-01T00:00:00Z',
        })
      )
    )

    const { result } = renderAuth()
    await act(() => result.current.signInWithUsername('existinguser'))

    await waitFor(() => expect(result.current.status).toBe('ready'))
    expect(result.current.user?.username).toBe('existinguser')
  })

  it('signInWithUsername rejects for an unknown username, leaving the visitor anonymous', async () => {
    tokenStorage.clear()
    server.use(
      http.post(`${BASE_URL}/api/dev/token`, () =>
        HttpResponse.json('No account found with that username.', { status: 404 })
      )
    )

    const { result } = renderAuth()

    await act(async () => {
      await expect(result.current.signInWithUsername('ghost')).rejects.toMatchObject({ status: 404 })
    })
    expect(result.current.status).toBe('anonymous')
  })

  it('completeSession adopts an already-finished session without minting a new one', async () => {
    tokenStorage.clear()
    let tokenMinted = false
    server.use(
      http.post(`${BASE_URL}/api/dev/token`, () => {
        tokenMinted = true
        return HttpResponse.json({ oid: 'should-not-be-used', token: 'should-not-be-used' })
      }),
      http.get(`${BASE_URL}/api/users/me`, () =>
        HttpResponse.json({
          username: 'nativeuser',
          displayName: 'Native User',
          avatarSeed: 'seed-2',
          karma: 0,
          friendListVisibility: 'Everyone',
          createdAtUtc: '2026-01-01T00:00:00Z',
        })
      )
    )

    const { result } = renderAuth()

    await act(() =>
      result.current.completeSession({ token: 'native-token', oid: 'native-oid', refreshToken: 'native-refresh' })
    )

    await waitFor(() => expect(result.current.status).toBe('ready'))
    expect(tokenMinted).toBe(false)
    expect(tokenStorage.getToken()).toBe('native-token')
    expect(tokenStorage.getRefreshToken()).toBe('native-refresh')
  })

  it('signOut clears the session and returns to anonymous', async () => {
    tokenStorage.clear()
    const { result } = renderAuth()
    await act(() => result.current.signUp())
    await waitFor(() => expect(result.current.status).not.toBe('anonymous'))

    act(() => result.current.signOut())

    await waitFor(() => expect(result.current.status).toBe('anonymous'))
    expect(tokenStorage.getToken()).toBeNull()
  })
})
