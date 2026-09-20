import type { DiscoveryScope, TrendingWindow } from './types'

export const discoveryKeys = {
  trending: (
    scope: DiscoveryScope,
    community: string | undefined,
    window: TrendingWindow,
    page: number
  ) => ['discovery', 'trending', scope, community, window, page] as const,
  recommendedCommunities: (page: number) =>
    ['discovery', 'recommendedCommunities', page] as const,
}
