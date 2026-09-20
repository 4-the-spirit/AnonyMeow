import { useQuery } from '@tanstack/react-query'
import * as api from './api'
import { searchKeys } from './queryKeys'
import type { SearchTargetType } from './types'

export function useSearch(query: string, type: SearchTargetType, page: number) {
  return useQuery({
    queryKey: searchKeys.results(query, type, page),
    queryFn: () => api.search(query, type, page),
    enabled: query.trim().length > 0,
  })
}
