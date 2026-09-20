import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { stripMarkdown } from '@/lib/markdown/stripMarkdown'
import { getDisplayName } from '@/lib/userDisplay'
import { useSavedComments, useSavedCommentsCache } from '../hooks'
import { CommentSaveButton } from './CommentSaveButton'

function commentPostLink(comment: { id: string; postId?: string; ancestorCommentIds?: string[] }) {
  const params = new URLSearchParams({ highlightComment: comment.id })
  if (comment.ancestorCommentIds?.length) {
    params.set('ancestors', comment.ancestorCommentIds.join(','))
  }
  return `/posts/${comment.postId}?${params.toString()}`
}

export function SavedCommentsTab() {
  const { t, i18n } = useTranslation('savedComments')
  const [page, setPage] = useState(1)
  const savedComments = useSavedComments(page)
  const { markSaved } = useSavedCommentsCache()
  const { data, isPending, isError, error, refetch } = savedComments

  // GET /api/users/me/saved-comments is authoritative for the comments it returns, so seed the
  // non-authoritative local cache from it — same reasoning as SavedPostsTab for posts.
  useEffect(() => {
    data?.items.forEach((comment) => markSaved(comment.id, true))
  }, [data])

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
    return <EmptyState title={t('emptyTitle')} description={t('emptyDescription')} />
  }

  return (
    <div className="flex flex-col gap-3">
      {data.items.map((comment) => (
        <div key={comment.id} className="rounded-lg border p-3">
          <div className="mb-1.5 flex items-center gap-1.5">
            <UserAvatar
              seed={comment.authorAvatarSeed}
              username={comment.authorUsername}
              className="size-6"
            />
            <p className="text-muted-foreground text-sm">
              {t('postedByPrefix')}{' '}
              <Link to={`/u/${comment.authorUsername}`} className="hover:text-foreground hover:underline">
                {getDisplayName({ username: comment.authorUsername, displayName: comment.authorDisplayName })}
              </Link>{' '}
              {comment.communityName && t('postedByCommunity', { community: comment.communityName })}
            </p>
          </div>
          <p className="text-muted-foreground line-clamp-2 text-sm">
            {stripMarkdown(comment.bodyMarkdown)}
          </p>
          <div className="mt-1 flex items-center justify-between gap-2">
            <Link
              to={commentPostLink(comment)}
              className="text-muted-foreground text-xs hover:underline"
            >
              {t('commentMeta', {
                score: comment.score,
                date: new Date(comment.createdAtUtc).toLocaleDateString(i18n.language),
              })}{' '}
              · {t('viewInPost')}
            </Link>
            <CommentSaveButton commentId={comment.id} />
          </div>
        </div>
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
