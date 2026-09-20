import { apiFetch } from '@/lib/api/client'
import type { SearchResponse, SearchTargetType } from './types'

export function search(query: string, type: SearchTargetType, page: number) {
  return apiFetch<SearchResponse>('/api/search', {
    searchParams: { q: query, type, page },
  })
}
