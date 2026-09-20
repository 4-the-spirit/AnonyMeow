import type { SortOrder } from './types'

export const postKeys = {
  community: (name: string, sort: SortOrder, page: number) =>
    ['posts', 'community', name, sort, page] as const,
  detail: (id: string) => ['posts', 'detail', id] as const,
}
