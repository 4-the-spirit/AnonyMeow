import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { PostResponse } from '@/features/posts/types'
import type { CommentResponse } from '@/features/comments/types'
import type { CommunityMembershipResponse } from '@/features/communities/types'
import type {
  CompleteProfileRequest,
  UpdateProfileRequest,
  UserResponse,
  UsernameAvailabilityResponse,
} from './types'

export function completeProfile(body: CompleteProfileRequest) {
  return apiFetch<UserResponse>('/api/auth/complete-profile', { method: 'POST', body })
}

export function getMe() {
  return apiFetch<UserResponse>('/api/users/me')
}

export function updateMe(body: UpdateProfileRequest) {
  return apiFetch<UserResponse>('/api/users/me', { method: 'PATCH', body })
}

export function checkUsernameAvailability(value: string) {
  return apiFetch<UsernameAvailabilityResponse>('/api/users/check-username', {
    searchParams: { value },
  })
}

export function getUserByUsername(username: string) {
  return apiFetch<UserResponse>(`/api/users/${encodeURIComponent(username)}`)
}

export function getUserPosts(username: string, page: number) {
  return apiFetch<PagedResponse<PostResponse>>(
    `/api/users/${encodeURIComponent(username)}/posts`,
    { searchParams: { page } }
  )
}

export function getUserComments(username: string, page: number) {
  return apiFetch<PagedResponse<CommentResponse>>(
    `/api/users/${encodeURIComponent(username)}/comments`,
    { searchParams: { page } }
  )
}

export function getUserCommunities(username: string, page: number) {
  return apiFetch<PagedResponse<CommunityMembershipResponse>>(
    `/api/users/${encodeURIComponent(username)}/communities`,
    { searchParams: { page } }
  )
}
