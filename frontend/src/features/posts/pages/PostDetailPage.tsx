import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ChevronLeft, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { ConfirmDialog } from '@/components/ConfirmDialog/ConfirmDialog'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { VoteWidget } from '@/components/VoteWidget/VoteWidget'
import { MarkdownBody } from '@/components/MarkdownBody/MarkdownBody'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { LinkWarningDialog } from '@/components/LinkWarningDialog/LinkWarningDialog'
import { useAuth } from '@/features/auth/useAuth'
import { useIsModerator } from '@/features/communities/hooks'
import { ReportDialog } from '@/features/moderation/components/ReportDialog'
import { ModActionMenu } from '@/features/moderation/components/ModActionMenu'
import { CommentSection } from '@/features/comments/components/CommentSection'
import { PostFlairControl } from '@/features/flairs/components/PostFlairControl'
import { SaveButton } from '@/features/savedPosts/components/SaveButton'
import { usePost, useDeletePost } from '../hooks'
import { postKeys } from '../queryKeys'
import { EditPostForm } from '../components/EditPostForm'
import { PollVoteWidget } from '../components/PollVoteWidget'
import { PostImageGallery } from '../components/PostImageGallery'
import { getDisplayName } from '@/lib/userDisplay'

export function PostDetailPage() {
  const { t } = useTranslation('posts')
  const { postId = '' } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [isEditing, setIsEditing] = useState(false)
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

  const { data: post, isPending, isError, error, refetch } = usePost(postId)
  const communityName = post?.communityName ?? ''
  const { isModerator } = useIsModerator(communityName)
  const deletePost = useDeletePost(postId)

  if (isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => refetch()} />
  }

  const isAuthor = user?.username === post.authorUsername

  async function handleDelete() {
    try {
      await deletePost.mutateAsync()
      toast.success(t('detail.deleteSuccessToast'))
      navigate(`/c/${communityName}`)
    } catch {
      toast.error(t('detail.deleteErrorToast'))
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <Link
        to={`/c/${communityName}`}
        className="text-muted-foreground hover:text-foreground flex w-fit items-center gap-1 text-sm hover:underline"
      >
        <ChevronLeft className="size-4 rtl:rotate-180" />
        c/{communityName}
      </Link>

      <Card className="flex-row gap-3 p-4">
        <VoteWidget
          targetType="Post"
          targetId={post.id}
          score={post.score}
          viewerVote={post.viewerVote}
          invalidateKey={postKeys.detail(post.id)}
        />
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            {post.isPinned && <Badge variant="secondary">{t('card.pinned')}</Badge>}
            {post.isLocked && <Badge variant="secondary">{t('card.locked')}</Badge>}
            {isAuthor && <PostFlairControl post={post} />}
          </div>
          <h1 className="text-xl font-semibold">
            {post.flair && (
              <span style={{ color: post.flair.colorHex }}>{post.flair.name}: </span>
            )}
            {post.title}
          </h1>
          <div className="flex items-center gap-1.5">
            <UserAvatar
              seed={post.authorAvatarSeed}
              username={post.authorUsername}
              className="size-6"
            />
            <p className="text-muted-foreground text-xs">
              {t('card.postedByPrefix')}{' '}
              <Link to={`/u/${post.authorUsername}`} className="hover:text-foreground hover:underline">
                {getDisplayName({ username: post.authorUsername, displayName: post.authorDisplayName })}
              </Link>{' '}
              {t('card.postedByCommunityPrefix')}{' '}
              <Link to={`/c/${post.communityName}`} className="hover:text-foreground hover:underline">
                c/{post.communityName}
              </Link>
            </p>
          </div>

          {isEditing ? (
            <EditPostForm post={post} onDone={() => setIsEditing(false)} />
          ) : (
            <>
              {post.bodyMarkdown && <MarkdownBody>{post.bodyMarkdown}</MarkdownBody>}
              {post.imageUrls.length > 0 && <PostImageGallery imageUrls={post.imageUrls} alt={post.title} />}
              {post.url && <LinkWarningDialog href={post.url}>{post.url}</LinkWarningDialog>}
              {post.pollOptions && <PollVoteWidget postId={post.id} options={post.pollOptions} />}
            </>
          )}

          <div className="flex items-center gap-2">
            {isAuthor && !isEditing && (
              <Button variant="ghost" size="sm" onClick={() => setIsEditing(true)}>
                <Pencil className="size-3.5" />
                {t('detail.edit')}
              </Button>
            )}
            {isAuthor && (
              <Button variant="ghost" size="sm" onClick={() => setConfirmDeleteOpen(true)}>
                <Trash2 className="size-3.5" />
                {t('detail.delete')}
              </Button>
            )}
            <ReportDialog targetType="Post" targetId={post.id} />
            <SaveButton postId={post.id} />
            {isModerator && <ModActionMenu post={post} />}
          </div>
        </div>
      </Card>

      <ConfirmDialog
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        title={t('detail.deleteConfirm')}
        onConfirm={handleDelete}
      />

      <CommentSection postId={post.id} communityName={communityName} isPostLocked={post.isLocked} />
    </div>
  )
}
