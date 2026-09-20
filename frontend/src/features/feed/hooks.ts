import { useQuery } from '@tanstack/react-query'
import type { SortOrder } from '@/features/posts/types'
import * as api from './api'
import { feedKeys } from './queryKeys'

export function useFeed(sort: SortOrder, page: number) {
  return useQuery({
    queryKey: feedKeys.list(sort, page),
    queryFn: () => api.getFeed(sort, page),
  })
}
