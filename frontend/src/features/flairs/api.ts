import { apiFetch } from '@/lib/api/client'
import type { CreateFlairRequest, FlairResponse } from './types'

export function listFlairs(communityName: string) {
  return apiFetch<FlairResponse[]>(
    `/api/communities/${encodeURIComponent(communityName)}/flairs`
  )
}

export function createFlair(communityName: string, body: CreateFlairRequest) {
  return apiFetch<FlairResponse>(
    `/api/communities/${encodeURIComponent(communityName)}/flairs`,
    { method: 'POST', body }
  )
}

export function updateFlair(communityName: string, id: string, body: CreateFlairRequest) {
  return apiFetch<FlairResponse>(
    `/api/communities/${encodeURIComponent(communityName)}/flairs/${id}`,
    { method: 'PATCH', body }
  )
}

export function deleteFlair(communityName: string, id: string) {
  return apiFetch<void>(
    `/api/communities/${encodeURIComponent(communityName)}/flairs/${id}`,
    { method: 'DELETE' }
  )
}
