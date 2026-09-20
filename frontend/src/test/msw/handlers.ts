import { http, HttpResponse } from 'msw'

const BASE_URL = 'https://test.local'

/**
 * Default handlers wired for a "ready" authenticated user. Individual tests override
 * specific routes via `server.use(...)` for error/edge-case scenarios.
 */
export const handlers = [
  http.post(`${BASE_URL}/api/dev/token`, () =>
    HttpResponse.json({ oid: 'test-oid', token: 'test-token' })
  ),
  http.get(`${BASE_URL}/api/users/me`, () =>
    HttpResponse.json({
      username: 'testuser',
      displayName: 'Test User',
      avatarSeed: 'seed-1',
      karma: 0,
      friendListVisibility: 'Everyone',
      createdAtUtc: '2026-01-01T00:00:00Z',
    })
  ),
]
