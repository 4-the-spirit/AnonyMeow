import { describe, expect, it, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { apiFetch } from './client'
import { ApiError } from './problemDetails'
import { tokenStorage } from '@/lib/auth/tokenStorage'

const BASE_URL = 'https://test.local'

describe('apiFetch', () => {
  beforeEach(() => {
    tokenStorage.clear()
  })

  it('parses a successful camelCase JSON response', async () => {
    server.use(
      http.get(`${BASE_URL}/api/users/me`, () =>
        HttpResponse.json({ username: 'alice', karma: 5 })
      )
    )

    const result = await apiFetch<{ username: string; karma: number }>('/api/users/me')

    expect(result).toEqual({ username: 'alice', karma: 5 })
  })

  it('attaches the bearer token when present', async () => {
    tokenStorage.set('abc123', 'oid-1')
    let receivedAuth: string | null = null

    server.use(
      http.get(`${BASE_URL}/api/users/me`, ({ request }) => {
        receivedAuth = request.headers.get('authorization')
        return HttpResponse.json({ username: 'alice' })
      })
    )

    await apiFetch('/api/users/me')

    expect(receivedAuth).toBe('Bearer abc123')
  })

  it('returns undefined for a 204 response', async () => {
    server.use(
      http.delete(`${BASE_URL}/api/posts/1/vote`, () => new HttpResponse(null, { status: 204 }))
    )

    const result = await apiFetch('/api/posts/1/vote', { method: 'DELETE' })

    expect(result).toBeUndefined()
  })

  it('throws ApiError with parsed ProblemDetails on non-2xx', async () => {
    server.use(
      http.get(`${BASE_URL}/api/users/nobody`, () =>
        HttpResponse.json(
          { title: 'Not Found', status: 404, detail: 'User not found' },
          { status: 404 }
        )
      )
    )

    await expect(apiFetch('/api/users/nobody')).rejects.toMatchObject({
      status: 404,
      detail: 'User not found',
    })
  })

  it('surfaces validation field errors on 400', async () => {
    server.use(
      http.post(`${BASE_URL}/api/communities`, () =>
        HttpResponse.json(
          {
            title: 'Validation failed',
            status: 400,
            errors: { name: ['Name is required'] },
          },
          { status: 400 }
        )
      )
    )

    try {
      await apiFetch('/api/communities', { method: 'POST', body: { name: '' } })
      expect.fail('expected apiFetch to throw')
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError)
      expect((error as ApiError).errors).toEqual({ name: ['Name is required'] })
    }
  })
})
