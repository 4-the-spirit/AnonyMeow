import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PostResultsList } from '@/features/posts/components/PostResultsList'
import { TrendingWindowToggle } from '../components/TrendingWindowToggle'
import { useTrending } from '../hooks'
import type { DiscoveryScope, TrendingWindow } from '../types'

/** Also reached with `?scope=community&community=<name>` from a community page's "Trending"
 * link — same page, no dedicated per-community route needed. */
export function TrendingPage() {
  const { t } = useTranslation('discovery')
  const [searchParams, setSearchParams] = useSearchParams()
  const scope: DiscoveryScope = searchParams.get('scope') === 'community' ? 'community' : 'platform'
  const community = searchParams.get('community') ?? undefined
  const window: TrendingWindow = searchParams.get('window') === 'week' ? 'week' : 'day'
  const page = Number(searchParams.get('page') ?? '1')

  const trending = useTrending(scope, community, window, page)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    next.set(key, value)
    if (key === 'window') next.set('page', '1')
    setSearchParams(next)
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">
        {scope === 'community' && community
          ? t('pageTitleCommunity', { community })
          : t('pageTitle')}
      </h1>

      <TrendingWindowToggle value={window} onChange={(next) => updateParam('window', next)} />

      <PostResultsList
        query={trending}
        onPageChange={(next) => updateParam('page', String(next))}
        emptyTitle={t('emptyTitle')}
        emptyDescription={t('emptyDescription')}
      />
    </div>
  )
}
