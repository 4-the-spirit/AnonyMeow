import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { CommunityCard } from '@/features/communities/components/CommunityCard'
import { useRecommendedCommunities } from '../hooks'

/** Popularity-heuristic recommendations (see backend `CommunityRecommendationService`) — hidden
 * entirely rather than shown empty, since "recommended" implies there's something to recommend. */
export function RecommendedCommunities() {
  const { t } = useTranslation('discovery')
  const { data, isPending, isError } = useRecommendedCommunities(1)

  if (isPending) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-40 w-full" />
        ))}
      </div>
    )
  }

  if (isError || data.items.length === 0) {
    return null
  }

  return (
    <div className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">{t('recommended.title')}</h2>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {data.items.map((community) => (
          <CommunityCard key={community.name} community={community} />
        ))}
      </div>
    </div>
  )
}
