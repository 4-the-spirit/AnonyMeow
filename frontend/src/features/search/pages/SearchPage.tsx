import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { PostCard } from '@/features/posts/components/PostCard'
import { CommunityCard } from '@/features/communities/components/CommunityCard'
import { CommentSearchResultCard } from '../components/CommentSearchResultCard'
import { useSearch } from '../hooks'
import type { SearchTargetType } from '../types'

// Matches the backend SearchEndpoints.DefaultPageSize — SearchResponse has no pageSize field of
// its own (unlike PagedResponse<T>), since it bundles three differently-counted result lists.
const PAGE_SIZE = 20

export function SearchPage() {
  const { t } = useTranslation('search')
  const [searchParams, setSearchParams] = useSearchParams()
  const query = searchParams.get('q') ?? ''
  const rawType = searchParams.get('type')
  const type: SearchTargetType =
    rawType === 'posts' || rawType === 'comments' || rawType === 'communities' ? rawType : 'all'
  const page = Number(searchParams.get('page') ?? '1')

  const results = useSearch(query, type, page)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    next.set(key, value)
    if (key === 'type') next.set('page', '1')
    setSearchParams(next)
  }

  const hasQuery = query.trim().length > 0
  const data = results.data

  const activeTotalCount = data
    ? type === 'posts'
      ? data.postsTotalCount
      : type === 'comments'
        ? data.commentsTotalCount
        : type === 'communities'
          ? data.communitiesTotalCount
          : Math.max(data.postsTotalCount, data.commentsTotalCount, data.communitiesTotalCount)
    : 0

  const isEmpty =
    !!data &&
    (type === 'posts'
      ? data.posts.length === 0
      : type === 'comments'
        ? data.comments.length === 0
        : type === 'communities'
          ? data.communities.length === 0
          : data.posts.length === 0 && data.comments.length === 0 && data.communities.length === 0)

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">
        {hasQuery ? t('pageTitle', { query }) : t('noQueryTitle')}
      </h1>

      <Tabs value={type} onValueChange={(next) => updateParam('type', next)}>
        <TabsList>
          <TabsTrigger value="all">{t('tabs.all')}</TabsTrigger>
          <TabsTrigger value="posts">{t('tabs.posts')}</TabsTrigger>
          <TabsTrigger value="comments">{t('tabs.comments')}</TabsTrigger>
          <TabsTrigger value="communities">{t('tabs.communities')}</TabsTrigger>
        </TabsList>
      </Tabs>

      {!hasQuery && <EmptyState title={t('noQueryTitle')} />}

      {hasQuery && results.isPending && (
        <div className="flex flex-col gap-3">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
      )}

      {hasQuery && results.isError && (
        <ErrorState error={results.error} onRetry={() => results.refetch()} />
      )}

      {hasQuery && data && isEmpty && (
        <EmptyState title={t('results.noneFound', { query })} />
      )}

      {hasQuery && data && !isEmpty && (
        <div className="flex flex-col gap-6">
          {(type === 'all' || type === 'posts') && data.posts.length > 0 && (
            <section className="flex flex-col gap-3">
              {type === 'all' && (
                <h2 className="text-lg font-semibold">
                  {t('sections.posts', { count: data.postsTotalCount })}
                </h2>
              )}
              <div className="flex flex-col gap-3">
                {data.posts.map((post) => (
                  <PostCard key={post.id} post={post} />
                ))}
              </div>
            </section>
          )}

          {(type === 'all' || type === 'comments') && data.comments.length > 0 && (
            <section className="flex flex-col gap-3">
              {type === 'all' && (
                <h2 className="text-lg font-semibold">
                  {t('sections.comments', { count: data.commentsTotalCount })}
                </h2>
              )}
              <div className="flex flex-col gap-3">
                {data.comments.map((comment) => (
                  <CommentSearchResultCard key={comment.id} comment={comment} />
                ))}
              </div>
            </section>
          )}

          {(type === 'all' || type === 'communities') && data.communities.length > 0 && (
            <section className="flex flex-col gap-3">
              {type === 'all' && (
                <h2 className="text-lg font-semibold">
                  {t('sections.communities', { count: data.communitiesTotalCount })}
                </h2>
              )}
              <div className="grid gap-4 sm:grid-cols-2">
                {data.communities.map((community) => (
                  <CommunityCard key={community.name} community={community} />
                ))}
              </div>
            </section>
          )}

          <PaginationControl
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={activeTotalCount}
            onPageChange={(next) => updateParam('page', String(next))}
          />
        </div>
      )}
    </div>
  )
}
