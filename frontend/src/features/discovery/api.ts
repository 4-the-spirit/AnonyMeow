import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { CommunityResponse } from '@/features/communities/types'
import type { PostResponse } from '@/features/posts/types'
import type { DiscoveryScope, TrendingWindow } from './types'

export function getTrending(
  scope: DiscoveryScope,
  community: string | undefined,
  window: TrendingWindow,
  page: number
) {
  return apiFetch<PagedResponse<PostResponse>>('/api/discover/trending', {
    searchParams: { scope, community, window, page },
  })
}

export function getRecommendedCommunities(page: number) {
  return apiFetch<PagedResponse<CommunityResponse>>('/api/discover/recommended-communities', {
    searchParams: { page },
  })
}
