import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { useUserCommunities } from '../hooks'

export function ProfileCommunitiesTab({ username }: { username: string }) {
  const { t } = useTranslation('profile')
  const [page, setPage] = useState(1)
  const { data, isPending, isError, error, refetch } = useUserCommunities(username, page)

  if (isPending) {
    return (
      <div className="flex flex-col gap-3">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-14 w-full" />
        ))}
      </div>
    )
  }

  if (isError) return <ErrorState error={error} onRetry={() => refetch()} />

  if (data.items.length === 0) {
    return <EmptyState title={t('noCommunitiesYet')} />
  }

  return (
    <div className="flex flex-col gap-3">
      {data.items.map((community) => (
        <Link
          key={community.name}
          to={`/c/${community.name}`}
          className="flex items-center gap-3 rounded-lg border p-3 hover:bg-muted"
        >
          {community.iconImageUrl ? (
            <img
              src={community.iconImageUrl}
              alt=""
              className="size-8 shrink-0 rounded-full object-cover"
            />
          ) : (
            <span className="bg-muted flex size-8 shrink-0 items-center justify-center rounded-full text-sm">
              {community.name.charAt(0)}
            </span>
          )}
          <p className="flex-1 text-sm font-medium">c/{community.name}</p>
          {community.role === 'Moderator' && (
            <Badge variant="secondary">
              <ShieldCheck className="size-3" /> {t('moderatorBadge')}
            </Badge>
          )}
        </Link>
      ))}
      <PaginationControl
        page={data.page}
        pageSize={data.pageSize}
        totalCount={data.totalCount}
        onPageChange={setPage}
      />
    </div>
  )
}
