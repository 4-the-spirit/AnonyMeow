import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { ApiError } from '@/lib/api/problemDetails'
import { useFriends } from '../hooks'

const PAGE_SIZE = 20

export function FriendsListTab({ username }: { username: string }) {
  const { t } = useTranslation('friends')
  const friends = useFriends(username)
  const [page, setPage] = useState(1)

  if (friends.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (friends.isError) {
    if (friends.error instanceof ApiError && friends.error.status === 403) {
      return <EmptyState title={t('list.privateTitle')} />
    }
    return <ErrorState error={friends.error} onRetry={() => friends.refetch()} />
  }

  if (friends.data.length === 0) {
    return <EmptyState title={t('list.noneYetTitle')} />
  }

  const pageItems = friends.data.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  return (
    <div className="flex flex-col gap-2">
      <ul className="flex flex-col gap-2">
        {pageItems.map((friend) => (
          <li key={friend.username}>
            <Link
              to={`/u/${friend.username}`}
              className="flex items-center gap-3 rounded-lg border p-2 hover:bg-muted"
            >
              <UserAvatar seed={friend.avatarSeed} username={friend.username} />
              <div>
                <p className="text-sm font-medium">{friend.displayName ?? friend.username}</p>
                <p className="text-muted-foreground text-xs">u/{friend.username}</p>
              </div>
            </Link>
          </li>
        ))}
      </ul>
      <PaginationControl
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={friends.data.length}
        onPageChange={setPage}
      />
    </div>
  )
}
