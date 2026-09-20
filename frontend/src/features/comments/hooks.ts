import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { SortOrder } from '@/features/posts/types'
import * as api from './api'
import { commentKeys } from './queryKeys'
import type { CreateCommentRequest, UpdateCommentRequest } from './types'

export function usePostComments(postId: string, sort: SortOrder, page: number) {
  return useQuery({
    queryKey: commentKeys.post(postId, sort, page),
    queryFn: () => api.listPostComments(postId, sort, page),
    enabled: !!postId,
  })
}

export function useCommentReplies(
  commentId: string,
  sort: SortOrder,
  page: number,
  pageSize: number,
  enabled: boolean
) {
  return useQuery({
    queryKey: commentKeys.replies(commentId, sort, page, pageSize),
    queryFn: () => api.listCommentReplies(commentId, sort, page, pageSize),
    enabled,
  })
}

/** Resolves a single comment by id — used to render a comment as the root of its own thread page
 * (the "Continue this thread" link past the inline depth limit). */
export function useComment(commentId: string) {
  return useQuery({
    queryKey: commentKeys.detail(commentId),
    queryFn: () => api.getComment(commentId),
    enabled: !!commentId,
  })
}

export function useCreateComment(postId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateCommentRequest) => api.createComment(postId, body),
    onSuccess: (_, variables) => {
      // Always invalidate the post's top-level list too, even for a reply: the parent
      // comment's replyCount lives there and would otherwise go stale until next refetch.
      queryClient.invalidateQueries({ queryKey: ['comments', 'post', postId] })
      if (variables.parentCommentId) {
        queryClient.invalidateQueries({
          queryKey: ['comments', 'replies', variables.parentCommentId],
        })
      }
    },
  })
}

export function useUpdateComment(commentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateCommentRequest) => api.updateComment(commentId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments'] })
    },
  })
}

export function useDeleteComment(commentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.deleteComment(commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments'] })
    },
  })
}
