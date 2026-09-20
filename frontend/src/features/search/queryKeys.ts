import type { SearchTargetType } from './types'

export const searchKeys = {
  results: (query: string, type: SearchTargetType, page: number) =>
    ['search', query, type, page] as const,
}
