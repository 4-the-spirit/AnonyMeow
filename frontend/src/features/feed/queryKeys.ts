import type { SortOrder } from '@/features/posts/types'

export const feedKeys = {
  list: (sort: SortOrder, page: number) => ['feed', sort, page] as const,
}
