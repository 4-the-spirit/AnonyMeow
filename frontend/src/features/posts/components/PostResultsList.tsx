import type { UseQueryResult } from '@tanstack/react-query'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import type { PagedResponse } from '@/lib/api/types'
import { PostCard } from './PostCard'
import type { PostResponse } from '../types'

interface PostResultsListProps {
  query: UseQueryResult<PagedResponse<PostResponse>>
  onPageChange: (page: number) => void
  emptyTitle: string
  emptyDescription?: string
  /** The community whose listing this is, if any — passed through to PostCard so it can hide
   * the redundant "in c/…" mention on its own community's listing. */
  currentCommunityName?: string
}

/** Shared paged-post-list rendering (loading/error/empty/list+pagination) for any screen that
 * lists PostResponse pages — community listing, feed, saved posts. */
export function PostResultsList({
  query,
  onPageChange,
  emptyTitle,
  emptyDescription,
  currentCommunityName,
}: PostResultsListProps) {
  const { data, isPending, isError, error, refetch } = query

  if (isPending) {
    return (
      <div className="flex flex-col gap-3">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-24 w-full" />
        ))}
      </div>
    )
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => refetch()} />
  }

  if (data.items.length === 0) {
    return <EmptyState title={emptyTitle} description={emptyDescription} />
  }

  return (
    <div className="flex flex-col gap-3">
      {data.items.map((post) => (
        <PostCard key={post.id} post={post} currentCommunityName={currentCommunityName} />
      ))}
      <PaginationControl
        page={data.page}
        pageSize={data.pageSize}
        totalCount={data.totalCount}
        onPageChange={onPageChange}
      />
    </div>
  )
}
