import { Link, useParams, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PostList } from '@/features/posts/components/PostList'
import { SortTabs } from '@/features/posts/components/SortTabs'
import type { SortOrder } from '@/features/posts/types'
import { useCommunity, useIsModerator } from '../hooks'
import { JoinLeaveButton } from '../components/JoinLeaveButton'

export function CommunityDetailPage() {
  const { t } = useTranslation('communities')
  const { communityName = '' } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const sort = (searchParams.get('sort') as SortOrder) ?? 'hot'
  const page = Number(searchParams.get('page') ?? '1')

  const community = useCommunity(communityName)
  const { isModerator } = useIsModerator(communityName)

  if (community.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (community.isError) {
    return <ErrorState error={community.error} onRetry={() => community.refetch()} />
  }

  const data = community.data

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    next.set(key, value)
    if (key === 'sort') next.set('page', '1')
    setSearchParams(next)
  }

  return (
    <div className="flex flex-col gap-6">
      <Card className="gap-0 overflow-hidden p-0">
        <div className="relative">
          {data.bannerImageUrl ? (
            <img src={data.bannerImageUrl} alt="" className="h-36 w-full object-cover" />
          ) : (
            <div className="from-primary/25 via-accent/40 to-secondary h-36 w-full bg-linear-to-br" />
          )}
          <div className="bg-card ring-card absolute -bottom-8 start-6 flex size-20 items-center justify-center overflow-hidden rounded-full ring-4">
            {data.iconImageUrl ? (
              <img src={data.iconImageUrl} alt="" className="size-full object-cover" />
            ) : (
              <div className="bg-muted text-muted-foreground flex size-full items-center justify-center rounded-full text-2xl font-semibold">
                {data.name.charAt(0)}
              </div>
            )}
          </div>
        </div>
        <CardContent className="flex flex-col gap-2 pt-10 pb-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h1 className="text-xl font-semibold">c/{data.name}</h1>
            <div className="flex gap-2">
              <JoinLeaveButton communityName={data.name} />
              <Button asChild variant="outline">
                <Link to={`/c/${data.name}/submit`}>{t('detail.submitPost')}</Link>
              </Button>
            </div>
          </div>
          {data.description && <p className="text-sm">{data.description}</p>}
          <p className="text-muted-foreground text-xs">
            {t('detail.memberCount', { count: data.memberCount })}
          </p>
          <div className="flex flex-wrap gap-3 text-sm">
            <Link
              to={`/trending?scope=community&community=${encodeURIComponent(data.name)}`}
              className="text-muted-foreground hover:underline"
            >
              {t('detail.trending')}
            </Link>
            <Link to={`/c/${data.name}/moderators`} className="text-muted-foreground hover:underline">
              {t('detail.moderators')}
            </Link>
            <Link to={`/c/${data.name}/members`} className="text-muted-foreground hover:underline">
              {t('detail.members')}
            </Link>
            {isModerator && (
              <>
                <Link to={`/c/${data.name}/settings`} className="text-muted-foreground hover:underline">
                  {t('detail.settings')}
                </Link>
                <Link to={`/c/${data.name}/mod/queue`} className="text-muted-foreground hover:underline">
                  {t('detail.modQueue')}
                </Link>
                <Link to={`/c/${data.name}/mod/bans`} className="text-muted-foreground hover:underline">
                  {t('detail.bans')}
                </Link>
                <Link to={`/c/${data.name}/flairs`} className="text-muted-foreground hover:underline">
                  {t('detail.flairs')}
                </Link>
              </>
            )}
          </div>
          {data.rules.length > 0 && (
            <details className="text-sm">
              <summary className="text-muted-foreground cursor-pointer">{t('detail.rules')}</summary>
              <ol className="mt-1 list-decimal ps-5">
                {data.rules.map((rule, index) => (
                  <li key={index}>
                    <span className="font-medium">{rule.title}</span>
                    <p className="text-muted-foreground whitespace-pre-wrap">{rule.description}</p>
                  </li>
                ))}
              </ol>
            </details>
          )}
        </CardContent>
      </Card>

      <SortTabs value={sort} onChange={(next) => updateParam('sort', next)} />

      <PostList
        communityName={communityName}
        sort={sort}
        page={page}
        onPageChange={(next) => updateParam('page', String(next))}
      />
    </div>
  )
}
