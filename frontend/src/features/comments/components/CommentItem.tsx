import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ChevronDown, ChevronRight, MessageCircle, Pencil, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ConfirmDialog/ConfirmDialog'
import { MarkdownBody } from '@/components/MarkdownBody/MarkdownBody'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { VoteWidget } from '@/components/VoteWidget/VoteWidget'
import { useAuth } from '@/features/auth/useAuth'
import { useIsModerator } from '@/features/communities/hooks'
import { ReportDialog } from '@/features/moderation/components/ReportDialog'
import { useCommentModActions } from '@/features/moderation/hooks'
import { CommentSaveButton } from '@/features/savedComments/components/CommentSaveButton'
import type { SortOrder } from '@/features/posts/types'
import { getDisplayName } from '@/lib/userDisplay'
import { useCommentReplies, useDeleteComment } from '../hooks'
import { CommentComposer } from './CommentComposer'
import { EditCommentForm } from './EditCommentForm'
import type { CommentResponse } from '../types'

// Reply chains render inline up to this many levels (this comment counts as level 1 of that
// chain); once the deepest inline level is reached, further replies are reached via a
// "Continue this thread" link to that comment's own thread page instead of nesting further.
const MAX_INLINE_DEPTH = 3
const REPLIES_INITIAL_PAGE_SIZE = 3
const REPLIES_PAGE_SIZE_INCREMENT = 3

interface CommentItemProps {
  postId: string
  communityName: string
  comment: CommentResponse
  isPostLocked: boolean
  /** Sort order applied to this comment's sibling replies — inherited from the post's comment
   * sort control (or the thread page's default) so ordering stays consistent at every level. */
  sort: SortOrder
  /** 0 for a top-level comment, incrementing by 1 per nesting level within the current inline
   * render — reset to 0 again on a thread page rooted at a deeper comment. */
  depth?: number
  /** Deep-link target from a profile page — scrolled to and briefly highlighted once mounted. */
  highlightCommentId?: string
}

export function CommentItem({
  postId,
  communityName,
  comment,
  isPostLocked,
  sort,
  depth = 0,
  highlightCommentId,
}: CommentItemProps) {
  const { t } = useTranslation('comments')
  const { t: tModeration } = useTranslation('moderation')
  const { user } = useAuth()
  const isAtInlineDepthLimit = depth >= MAX_INLINE_DEPTH - 1
  const [replyPageSize, setReplyPageSize] = useState(REPLIES_INITIAL_PAGE_SIZE)
  const [isReplying, setIsReplying] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [isHighlighted, setIsHighlighted] = useState(false)
  const [isMinimized, setIsMinimized] = useState(false)
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)
  const [confirmModRemoveOpen, setConfirmModRemoveOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  const replies = useCommentReplies(comment.id, sort, 1, replyPageSize, !isAtInlineDepthLimit)
  const deleteComment = useDeleteComment(comment.id)
  const { isModerator } = useIsModerator(communityName)
  const modActions = useCommentModActions(comment.id)
  const isAuthor = user?.username === comment.authorUsername
  const remainingReplyCount = replies.data ? replies.data.totalCount - replies.data.items.length : 0

  useEffect(() => {
    if (comment.id !== highlightCommentId) return
    containerRef.current?.scrollIntoView({ block: 'center', behavior: 'smooth' })
    setIsHighlighted(true)
    const timeout = setTimeout(() => setIsHighlighted(false), 2500)
    return () => clearTimeout(timeout)
  }, [comment.id, highlightCommentId])

  async function handleDelete() {
    try {
      await deleteComment.mutateAsync()
      toast.success(t('item.deleteSuccessToast'))
    } catch {
      toast.error(t('item.deleteErrorToast'))
    }
  }

  async function handleModRemove() {
    try {
      await modActions.remove.mutateAsync(undefined)
      toast.success(tModeration('commentRemove.successToast'))
    } catch {
      toast.error(tModeration('commentRemove.errorToast'))
    }
  }

  return (
    <div
      ref={containerRef}
      className={cn(
        'flex gap-2 rounded-lg transition-colors',
        isHighlighted && 'bg-primary/10 ring-2 ring-primary'
      )}
    >
      <VoteWidget
        targetType="Comment"
        targetId={comment.id}
        score={comment.score}
        viewerVote={comment.viewerVote}
        invalidateKey={['comments']}
        orientation="vertical"
      />
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        {/* The whole row (not just the avatar/name) toggles collapse — clicking the avatar or
         * username link still navigates to the profile instead, via stopPropagation. */}
        <div
          className="flex w-full cursor-pointer items-center gap-1 select-none"
          onClick={() => setIsMinimized((v) => !v)}
        >
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="size-5 shrink-0"
            aria-label={isMinimized ? t('item.expand') : t('item.minimize')}
            onClick={(event) => {
              event.stopPropagation()
              setIsMinimized((v) => !v)
            }}
          >
            {isMinimized ? (
              <ChevronRight className="size-3.5 rtl:rotate-180" />
            ) : (
              <ChevronDown className="size-3.5" />
            )}
          </Button>
          <Link
            to={`/u/${comment.authorUsername}`}
            className="flex items-center gap-1.5"
            onClick={(event) => event.stopPropagation()}
          >
            <UserAvatar
              seed={comment.authorAvatarSeed}
              username={comment.authorUsername}
              className="size-6"
            />
            <span className="text-muted-foreground text-xs font-medium hover:text-foreground hover:underline">
              {getDisplayName({ username: comment.authorUsername, displayName: comment.authorDisplayName })}
            </span>
          </Link>
          {comment.editedAtUtc && (
            <span className="text-muted-foreground text-xs">({t('item.edited')})</span>
          )}
          {isMinimized && (
            <span className="text-muted-foreground text-xs">({t('item.minimized')})</span>
          )}
        </div>

        {!isMinimized && (
          <>
            {isEditing ? (
              <EditCommentForm comment={comment} onDone={() => setIsEditing(false)} />
            ) : (
              <MarkdownBody className="text-sm" allowImages={false}>
                {comment.bodyMarkdown}
              </MarkdownBody>
            )}

            <div className="flex flex-wrap items-center gap-1">
              {!isPostLocked && (
                <Button variant="ghost" size="sm" onClick={() => setIsReplying((v) => !v)}>
                  <MessageCircle className="size-3.5" />
                  {t('item.reply')}
                </Button>
              )}
              {isAuthor && !isEditing && (
                <Button variant="ghost" size="sm" onClick={() => setIsEditing(true)}>
                  <Pencil className="size-3.5" />
                  {t('item.edit')}
                </Button>
              )}
              {isAuthor && (
                <Button variant="ghost" size="sm" onClick={() => setConfirmDeleteOpen(true)}>
                  <Trash2 className="size-3.5" />
                  {t('item.delete')}
                </Button>
              )}
              {isModerator && !isAuthor && (
                <Button variant="ghost" size="sm" onClick={() => setConfirmModRemoveOpen(true)}>
                  <Trash2 className="size-3.5" />
                  {tModeration('commentRemove.remove')}
                </Button>
              )}
              <ReportDialog targetType="Comment" targetId={comment.id} />
              <CommentSaveButton commentId={comment.id} />
              <ConfirmDialog
                open={confirmDeleteOpen}
                onOpenChange={setConfirmDeleteOpen}
                title={t('item.deleteConfirm')}
                onConfirm={handleDelete}
              />
              <ConfirmDialog
                open={confirmModRemoveOpen}
                onOpenChange={setConfirmModRemoveOpen}
                title={tModeration('commentRemove.confirmTitle')}
                onConfirm={handleModRemove}
              />
              {comment.replyCount > 0 && isAtInlineDepthLimit && (
                <Button variant="ghost" size="sm" asChild>
                  <Link to={`/posts/${postId}/comments/${comment.id}`}>
                    <MessageCircle className="size-3.5" />
                    {t('item.continueThread')}
                  </Link>
                </Button>
              )}
            </div>

            {isReplying && (
              <CommentComposer
                postId={postId}
                parentCommentId={comment.id}
                onDone={() => setIsReplying(false)}
                autoFocus
              />
            )}

            {!isAtInlineDepthLimit && (
              <div className="mt-2 flex flex-col gap-3 border-s ps-3">
                {replies.isPending && (
                  <p className="text-muted-foreground text-xs">{t('item.loadingReplies')}</p>
                )}
                {replies.data?.items.map((reply) => (
                  <CommentItem
                    key={reply.id}
                    postId={postId}
                    communityName={communityName}
                    comment={reply}
                    isPostLocked={isPostLocked}
                    sort={sort}
                    depth={depth + 1}
                    highlightCommentId={highlightCommentId}
                  />
                ))}
                {remainingReplyCount > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="w-fit"
                    onClick={() => setReplyPageSize((v) => v + REPLIES_PAGE_SIZE_INCREMENT)}
                  >
                    {t('item.showMoreReplies', {
                      count: Math.min(remainingReplyCount, REPLIES_PAGE_SIZE_INCREMENT),
                    })}
                  </Button>
                )}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
