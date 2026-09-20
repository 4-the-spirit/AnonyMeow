import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { CommentResponse } from '@/features/comments/types'

export function saveComment(commentId: string) {
  return apiFetch<void>(`/api/comments/${commentId}/save`, { method: 'POST' })
}

export function unsaveComment(commentId: string) {
  return apiFetch<void>(`/api/comments/${commentId}/save`, { method: 'DELETE' })
}

export function listSavedComments(page: number) {
  return apiFetch<PagedResponse<CommentResponse>>('/api/users/me/saved-comments', {
    searchParams: { page },
  })
}
