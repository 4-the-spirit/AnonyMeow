import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { SortTabs, SORT_VALUES_WITHOUT_PINNED } from '@/features/posts/components/SortTabs'
import { PostResultsList } from '@/features/posts/components/PostResultsList'
import type { SortOrder } from '@/features/posts/types'
import { useFeed } from '../hooks'

export function FeedPage() {
  const { t } = useTranslation('feed')
  const [searchParams, setSearchParams] = useSearchParams()
  const sort = (searchParams.get('sort') as SortOrder) ?? 'hot'
  const page = Number(searchParams.get('page') ?? '1')

  const feed = useFeed(sort, page)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    next.set(key, value)
    if (key === 'sort') next.set('page', '1')
    setSearchParams(next)
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t('pageTitle')}</h1>

      <SortTabs value={sort} onChange={(next) => updateParam('sort', next)} sorts={SORT_VALUES_WITHOUT_PINNED} />

      <PostResultsList
        query={feed}
        onPageChange={(next) => updateParam('page', String(next))}
        emptyTitle={t('emptyTitle')}
        emptyDescription={t('emptyDescription')}
      />
    </div>
  )
}
