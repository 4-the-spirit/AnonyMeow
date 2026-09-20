import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type {
  CommunityMemberResponse,
  CommunityModeratorResponse,
  CommunityResponse,
  CreateCommunityRequest,
  UpdateCommunityRequest,
} from './types'

export function createCommunity(body: CreateCommunityRequest) {
  return apiFetch<CommunityResponse>('/api/communities', { method: 'POST', body })
}

export function listCommunities(search: string, page: number) {
  return apiFetch<PagedResponse<CommunityResponse>>('/api/communities', {
    searchParams: { search: search || undefined, page },
  })
}

export function getCommunity(name: string) {
  return apiFetch<CommunityResponse>(`/api/communities/${encodeURIComponent(name)}`)
}

export function updateCommunity(name: string, body: UpdateCommunityRequest) {
  return apiFetch<CommunityResponse>(`/api/communities/${encodeURIComponent(name)}`, {
    method: 'PATCH',
    body,
  })
}

export function joinCommunity(name: string) {
  return apiFetch<CommunityResponse>(`/api/communities/${encodeURIComponent(name)}/join`, {
    method: 'POST',
  })
}

export function leaveCommunity(name: string) {
  return apiFetch<void>(`/api/communities/${encodeURIComponent(name)}/leave`, {
    method: 'DELETE',
  })
}

export function listModerators(name: string) {
  return apiFetch<CommunityModeratorResponse[]>(
    `/api/communities/${encodeURIComponent(name)}/moderators`
  )
}

export function listMembers(name: string, search: string, page: number) {
  return apiFetch<PagedResponse<CommunityMemberResponse>>(
    `/api/communities/${encodeURIComponent(name)}/members`,
    { searchParams: { search: search || undefined, page } }
  )
}

export function promoteModerator(name: string, username: string) {
  return apiFetch<void>(
    `/api/communities/${encodeURIComponent(name)}/mod/moderators/${encodeURIComponent(username)}`,
    { method: 'POST' }
  )
}

export function demoteModerator(name: string, username: string) {
  return apiFetch<void>(
    `/api/communities/${encodeURIComponent(name)}/mod/moderators/${encodeURIComponent(username)}`,
    { method: 'DELETE' }
  )
}
