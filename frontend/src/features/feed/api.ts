import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { PostResponse, SortOrder } from '@/features/posts/types'

export function getFeed(sort: SortOrder, page: number) {
  return apiFetch<PagedResponse<PostResponse>>('/api/feed', { searchParams: { sort, page } })
}
