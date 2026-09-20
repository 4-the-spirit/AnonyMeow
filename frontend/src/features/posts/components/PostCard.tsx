import { Link, useNavigate } from 'react-router-dom'
import { MessageSquare, Pin, Lock } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { VoteWidget } from '@/components/VoteWidget/VoteWidget'
import { SaveButton } from '@/features/savedPosts/components/SaveButton'
import { stripMarkdown } from '@/lib/markdown/stripMarkdown'
import { getDisplayName } from '@/lib/userDisplay'
import { postKeys } from '../queryKeys'
import type { PostResponse } from '../types'

export function PostCard({
  post,
  currentCommunityName,
}: {
  post: PostResponse
  /** The community whose listing this card is rendered in, if any — when it matches the
   * post's own community, the "in c/…" mention is redundant and gets hidden. */
  currentCommunityName?: string
}) {
  const { t } = useTranslation('posts')
  const navigate = useNavigate()
  const preview = post.bodyMarkdown ? stripMarkdown(post.bodyMarkdown) : null
  const showCommunity = post.communityName !== currentCommunityName

  return (
    <Card
      className="flex-row gap-3 p-4 transition-colors hover:bg-accent/40 cursor-pointer"
      onClick={() => navigate(`/posts/${post.id}`)}
    >
      <div onClick={(event) => event.stopPropagation()}>
        <VoteWidget
          targetType="Post"
          targetId={post.id}
          score={post.score}
          viewerVote={post.viewerVote}
          invalidateKey={postKeys.detail(post.id)}
        />
      </div>
      <div className="flex min-w-0 flex-1 flex-col gap-1.5">
        {(post.isPinned || post.isLocked) && (
          <div className="flex flex-wrap items-center gap-2">
            {post.isPinned && (
              <Badge variant="secondary">
                <Pin className="size-3" /> {t('card.pinned')}
              </Badge>
            )}
            {post.isLocked && (
              <Badge variant="secondary">
                <Lock className="size-3" /> {t('card.locked')}
              </Badge>
            )}
          </div>
        )}
        <p className="text-xl leading-snug font-bold">
          {post.flair && (
            <span style={{ color: post.flair.colorHex }}>{post.flair.name}: </span>
          )}
          {post.title}
        </p>
        <div className="flex items-center gap-1.5">
          <UserAvatar
            seed={post.authorAvatarSeed}
            username={post.authorUsername}
            className="size-6"
          />
          <p className="text-muted-foreground text-sm">
            {t('card.postedByPrefix')}{' '}
            <Link
              to={`/u/${post.authorUsername}`}
              className="hover:text-foreground hover:underline"
              onClick={(event) => event.stopPropagation()}
            >
              {getDisplayName({ username: post.authorUsername, displayName: post.authorDisplayName })}
            </Link>
            {showCommunity && (
              <>
                {' '}
                {t('card.postedByCommunityPrefix')}{' '}
                <Link
                  to={`/c/${post.communityName}`}
                  className="hover:text-foreground hover:underline"
                  onClick={(event) => event.stopPropagation()}
                >
                  c/{post.communityName}
                </Link>
              </>
            )}
            {post.editedAtUtc && (
              <>
                {' · '}
                <span>{t('card.edited')}</span>
              </>
            )}
          </p>
        </div>
        {preview && (
          <p className="text-muted-foreground line-clamp-2 text-sm">{preview}</p>
        )}
        <div className="flex items-center gap-1" onClick={(event) => event.stopPropagation()}>
          <Link
            to={`/posts/${post.id}`}
            className="text-muted-foreground flex items-center gap-1.5 text-sm hover:underline"
          >
            <MessageSquare className="size-4" />
            {t('card.commentCount', { count: post.commentCount })}
          </Link>
          <SaveButton postId={post.id} />
        </div>
      </div>
    </Card>
  )
}
