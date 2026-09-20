import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ArrowLeft } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { usePost } from '@/features/posts/hooks'
import { useComment } from '../hooks'
import { CommentItem } from '../components/CommentItem'

/** Reached via a comment's "Continue this thread" link once its reply chain hits the inline
 * depth limit — renders that single comment as a fresh root (its own 3 inline levels), the same
 * way Reddit's permalinked thread view works. */
export function CommentThreadPage() {
  const { postId = '', commentId = '' } = useParams()
  const { t } = useTranslation('comments')

  const { data: post, isPending: isPostPending, isError: isPostError, error: postError, refetch: refetchPost } =
    usePost(postId)
  const { data: comment, isPending: isCommentPending, isError: isCommentError, error: commentError, refetch: refetchComment } =
    useComment(commentId)

  const isPending = isPostPending || isCommentPending
  if (isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (isPostError) {
    return <ErrorState error={postError} onRetry={() => refetchPost()} />
  }
  if (isCommentError) {
    return <ErrorState error={commentError} onRetry={() => refetchComment()} />
  }

  return (
    <div className="flex flex-col gap-4">
      <Link
        to={`/posts/${postId}`}
        className="text-muted-foreground flex w-fit items-center gap-1.5 text-sm hover:text-foreground hover:underline"
      >
        <ArrowLeft className="size-3.5 rtl:rotate-180" />
        {t('thread.backToPost', { title: post.title })}
      </Link>

      <Card className="flex-col gap-4 p-4">
        <h2 className="font-semibold">{t('thread.title')}</h2>
        <CommentItem
          postId={postId}
          communityName={post.communityName}
          comment={comment}
          isPostLocked={post.isLocked}
          sort="new"
          depth={0}
        />
      </Card>
    </div>
  )
}
