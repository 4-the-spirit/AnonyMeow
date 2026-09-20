import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type {
  CastPollVoteRequest,
  CreatePostRequest,
  PollOptionResponse,
  PostResponse,
  SortOrder,
  UpdatePostRequest,
} from './types'

export function createPost(communityName: string, body: CreatePostRequest) {
  return apiFetch<PostResponse>(
    `/api/communities/${encodeURIComponent(communityName)}/posts`,
    { method: 'POST', body }
  )
}

export function listCommunityPosts(communityName: string, sort: SortOrder, page: number) {
  return apiFetch<PagedResponse<PostResponse>>(
    `/api/communities/${encodeURIComponent(communityName)}/posts`,
    { searchParams: { sort, page } }
  )
}

export function getPost(id: string) {
  return apiFetch<PostResponse>(`/api/posts/${id}`)
}

export function updatePost(id: string, body: UpdatePostRequest) {
  return apiFetch<PostResponse>(`/api/posts/${id}`, { method: 'PATCH', body })
}

export function updatePostFlair(id: string, flairId: string) {
  return apiFetch<PostResponse>(`/api/posts/${id}/flair`, {
    method: 'PATCH',
    body: { flairId },
  })
}

export function deletePost(id: string) {
  return apiFetch<void>(`/api/posts/${id}`, { method: 'DELETE' })
}

export function castPollVote(postId: string, body: CastPollVoteRequest) {
  return apiFetch<PollOptionResponse[]>(`/api/posts/${postId}/poll-votes`, {
    method: 'POST',
    body,
  })
}
