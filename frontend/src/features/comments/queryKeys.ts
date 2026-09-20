import type { SortOrder } from '@/features/posts/types'

export const commentKeys = {
  post: (postId: string, sort: SortOrder, page: number) =>
    ['comments', 'post', postId, sort, page] as const,
  replies: (commentId: string, sort: SortOrder, page: number, pageSize: number) =>
    ['comments', 'replies', commentId, sort, page, pageSize] as const,
  detail: (commentId: string) => ['comments', 'detail', commentId] as const,
}
