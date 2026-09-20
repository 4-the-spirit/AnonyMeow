import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { SortTabs, SORT_VALUES_WITHOUT_PINNED } from '@/features/posts/components/SortTabs'
import type { SortOrder } from '@/features/posts/types'
import { usePostComments } from '../hooks'
import { CommentComposer } from './CommentComposer'
import { CommentItem } from './CommentItem'

export function CommentSection({
  postId,
  communityName,
  isPostLocked,
}: {
  postId: string
  communityName: string
  isPostLocked: boolean
}) {
  const [searchParams, setSearchParams] = useSearchParams()
  const sort = (searchParams.get('commentSort') as SortOrder) ?? 'new'
  const page = Number(searchParams.get('commentPage') ?? '1')
  const highlightCommentId = searchParams.get('highlightComment') ?? undefined

  const { t } = useTranslation('comments')
  const { data, isPending, isError, error, refetch } = usePostComments(postId, sort, page)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    next.set(key, value)
    if (key === 'commentSort') next.set('commentPage', '1')
    setSearchParams(next)
  }

  return (
    <Card className="flex-col gap-4 p-4">
      <h2 className="font-semibold">{t('section.title')}</h2>

      {isPostLocked ? (
        <p className="text-muted-foreground text-sm">{t('section.postLocked')}</p>
      ) : (
        <CommentComposer postId={postId} />
      )}

      <SortTabs value={sort} onChange={(next) => updateParam('commentSort', next)} sorts={SORT_VALUES_WITHOUT_PINNED} />

      {isPending && (
        <div className="flex flex-col gap-3">
          {[1, 2].map((i) => (
            <Skeleton key={i} className="h-16 w-full" />
          ))}
        </div>
      )}

      {isError && <ErrorState error={error} onRetry={() => refetch()} />}

      {data && data.items.length === 0 && (
        <EmptyState title={t('section.noneYetTitle')} description={t('section.noneYetDescription')} />
      )}

      {data && data.items.length > 0 && (
        <div className="flex flex-col gap-4">
          {data.items.map((comment) => (
            <CommentItem
              key={comment.id}
              postId={postId}
              communityName={communityName}
              comment={comment}
              isPostLocked={isPostLocked}
              sort={sort}
              depth={0}
              highlightCommentId={highlightCommentId}
            />
          ))}
        </div>
      )}

      {data && (
        <PaginationControl
          page={data.page}
          pageSize={data.pageSize}
          totalCount={data.totalCount}
          onPageChange={(next) => updateParam('commentPage', String(next))}
        />
      )}
    </Card>
  )
}
