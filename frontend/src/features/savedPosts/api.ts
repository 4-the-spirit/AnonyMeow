import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { PostResponse } from '@/features/posts/types'

export function savePost(postId: string) {
  return apiFetch<void>(`/api/posts/${postId}/save`, { method: 'POST' })
}

export function unsavePost(postId: string) {
  return apiFetch<void>(`/api/posts/${postId}/save`, { method: 'DELETE' })
}

export function listSavedPosts(page: number) {
  return apiFetch<PagedResponse<PostResponse>>('/api/users/me/saved-posts', {
    searchParams: { page },
  })
}
