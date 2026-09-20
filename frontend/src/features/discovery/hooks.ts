import { useQuery } from '@tanstack/react-query'
import * as api from './api'
import { discoveryKeys } from './queryKeys'
import type { DiscoveryScope, TrendingWindow } from './types'

export function useTrending(
  scope: DiscoveryScope,
  community: string | undefined,
  window: TrendingWindow,
  page: number
) {
  return useQuery({
    queryKey: discoveryKeys.trending(scope, community, window, page),
    queryFn: () => api.getTrending(scope, community, window, page),
    enabled: scope === 'platform' || !!community,
  })
}

export function useRecommendedCommunities(page: number) {
  return useQuery({
    queryKey: discoveryKeys.recommendedCommunities(page),
    queryFn: () => api.getRecommendedCommunities(page),
  })
}
