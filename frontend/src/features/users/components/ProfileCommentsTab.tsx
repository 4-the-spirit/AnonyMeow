import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { MarkdownBody } from '@/components/MarkdownBody/MarkdownBody'
import { useUserComments } from '../hooks'

function commentPostLink(comment: { id: string; postId?: string; ancestorCommentIds?: string[] }) {
  const params = new URLSearchParams({ highlightComment: comment.id })
  if (comment.ancestorCommentIds?.length) {
    params.set('ancestors', comment.ancestorCommentIds.join(','))
  }
  return `/posts/${comment.postId}?${params.toString()}`
}

export function ProfileCommentsTab({ username }: { username: string }) {
  const { t, i18n } = useTranslation('profile')
  const [page, setPage] = useState(1)
  const { data, isPending, isError, error, refetch } = useUserComments(username, page)

  if (isPending) {
    return (
      <div className="flex flex-col gap-3">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    )
  }

  if (isError) return <ErrorState error={error} onRetry={() => refetch()} />

  if (data.items.length === 0) {
    return <EmptyState title={t('noCommentsYet')} />
  }

  return (
    <div className="flex flex-col gap-3">
      {data.items.map((comment) => (
        // MarkdownBody can render its own <a> tags from the comment's markdown, so the
        // navigation link lives in the metadata line rather than wrapping the whole card
        // (nesting an <a> inside an <a> is invalid HTML and breaks click handling).
        <div key={comment.id} className="rounded-lg border p-3">
          <MarkdownBody className="text-sm" allowImages={false}>
            {comment.bodyMarkdown}
          </MarkdownBody>
          <Link
            to={commentPostLink(comment)}
            className="text-muted-foreground mt-1 block text-xs hover:underline"
          >
            {t('commentMeta', {
              score: comment.score,
              date: new Date(comment.createdAtUtc).toLocaleDateString(i18n.language),
            })}{' '}
            · {t('viewInPost')}
          </Link>
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
