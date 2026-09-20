import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { RecommendedCommunities } from '@/features/discovery/components/RecommendedCommunities'
import { useCommunities } from '../hooks'
import { CommunityCard } from '../components/CommunityCard'

export function CommunityBrowsePage() {
  const { t } = useTranslation('communities')
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('search') ?? ''
  const page = Number(searchParams.get('page') ?? '1')

  const { data, isPending, isError, error, refetch } = useCommunities(search, page)

  function handleSearchChange(value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set('search', value)
    else next.delete('search')
    next.set('page', '1')
    setSearchParams(next)
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold">{t('browse.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('browse.intro')}</p>
      </div>

      {/* Only relevant while browsing, not while filtering by search */}
      {!search && <RecommendedCommunities />}

      <Input
        placeholder={t('browse.searchPlaceholder')}
        value={search}
        onChange={(e) => handleSearchChange(e.target.value)}
        className="max-w-sm"
      />

      {isPending && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <Skeleton key={i} className="h-40 w-full" />
          ))}
        </div>
      )}

      {isError && <ErrorState error={error} onRetry={() => refetch()} />}

      {data && data.items.length === 0 && (
        <EmptyState
          title={t('browse.noneFoundTitle')}
          description={search ? t('browse.noneFoundTrySearch') : t('browse.noneFoundBeFirst')}
        />
      )}

      {data && data.items.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data.items.map((community) => (
            <CommunityCard key={community.name} community={community} />
          ))}
        </div>
      )}

      {data && (
        <PaginationControl
          page={data.page}
          pageSize={data.pageSize}
          totalCount={data.totalCount}
          onPageChange={(nextPage) => {
            const next = new URLSearchParams(searchParams)
            next.set('page', String(nextPage))
            setSearchParams(next)
          }}
        />
      )}
    </div>
  )
}
