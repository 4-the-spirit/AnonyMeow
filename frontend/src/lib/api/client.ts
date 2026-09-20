import { tokenStorage } from '@/lib/auth/tokenStorage'
import { ApiError, type ValidationProblemDetails } from './problemDetails'

export const BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export interface ApiFetchOptions {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE'
  body?: unknown
  searchParams?: Record<string, string | number | boolean | undefined | null>
  /** Skips attaching the Authorization header (e.g. /api/dev/token itself). */
  skipAuth?: boolean
}

function buildUrl(path: string, searchParams?: ApiFetchOptions['searchParams']) {
  const url = new URL(path.replace(/^\//, ''), `${BASE_URL}/`)
  if (searchParams) {
    for (const [key, value] of Object.entries(searchParams)) {
      if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(key, String(value))
      }
    }
  }
  return url.toString()
}

export async function apiFetch<T>(
  path: string,
  options: ApiFetchOptions = {}
): Promise<T> {
  const { method = 'GET', body, searchParams, skipAuth } = options

  const headers: Record<string, string> = {}
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  if (!skipAuth) {
    const token = tokenStorage.getToken()
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
  }

  const response = await fetch(buildUrl(path, searchParams), {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (response.status === 204) {
    return undefined as T
  }

  const isJson = response.headers.get('content-type')?.includes('application/json')
  const payload = isJson ? await response.json().catch(() => null) : null

  if (!response.ok) {
    throw new ApiError(response.status, payload as ValidationProblemDetails | null)
  }

  return payload as T
}
