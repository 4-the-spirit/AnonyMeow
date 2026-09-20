import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { SortOrder } from '@/features/posts/types'
import type { CommentResponse, CreateCommentRequest, UpdateCommentRequest } from './types'

export function createComment(postId: string, body: CreateCommentRequest) {
  return apiFetch<CommentResponse>(`/api/posts/${postId}/comments`, {
    method: 'POST',
    body,
  })
}

export function listPostComments(postId: string, sort: SortOrder, page: number) {
  return apiFetch<PagedResponse<CommentResponse>>(`/api/posts/${postId}/comments`, {
    searchParams: { sort, page },
  })
}

export function listCommentReplies(commentId: string, sort: SortOrder, page: number, pageSize: number) {
  return apiFetch<PagedResponse<CommentResponse>>(`/api/comments/${commentId}/replies`, {
    searchParams: { sort, page, pageSize },
  })
}

/** Resolves any comment (not just the caller's own) to its postId/ancestorCommentIds — used to
 * deep-link a Reply/Mention notification to the exact post + highlighted comment. */
export function getComment(commentId: string) {
  return apiFetch<CommentResponse>(`/api/comments/${commentId}`)
}

export function updateComment(commentId: string, body: UpdateCommentRequest) {
  return apiFetch<CommentResponse>(`/api/comments/${commentId}`, {
    method: 'PATCH',
    body,
  })
}

export function deleteComment(commentId: string) {
  return apiFetch<void>(`/api/comments/${commentId}`, { method: 'DELETE' })
}
