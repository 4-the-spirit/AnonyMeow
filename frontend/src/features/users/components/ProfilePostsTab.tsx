import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { PostCard } from '@/features/posts/components/PostCard'
import { useUserPosts } from '../hooks'

export function ProfilePostsTab({ username }: { username: string }) {
  const { t } = useTranslation('profile')
  const [page, setPage] = useState(1)
  const { data, isPending, isError, error, refetch } = useUserPosts(username, page)

  if (isPending) {
    return (
      <div className="flex flex-col gap-3">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-24 w-full" />
        ))}
      </div>
    )
  }

  if (isError) return <ErrorState error={error} onRetry={() => refetch()} />

  if (data.items.length === 0) {
    return <EmptyState title={t('noPostsYet')} />
  }

  return (
    <div className="flex flex-col gap-3">
      {data.items.map((post) => (
        <PostCard key={post.id} post={post} />
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
