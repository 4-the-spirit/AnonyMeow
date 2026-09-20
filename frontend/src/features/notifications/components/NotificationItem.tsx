import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { getComment } from '@/features/comments/api'
import { useMarkNotificationRead } from '../hooks'
import type { NotificationResponse } from '../types'

/**
 * Resolves where a notification should navigate to. Reply/Mention point at a comment id that
 * may belong to someone else entirely, so it's resolved via GET /api/comments/{id} (added
 * specifically for this) to get the postId + ancestor chain the post page's deep-link query
 * params already know how to auto-expand/highlight. FriendRequest always goes to the requests
 * page — sourceId there is a raw user id with no by-id profile lookup, but the requests page
 * already resolves usernames on its own. ModAction has no navigation target yet.
 */
async function resolveTarget(notification: NotificationResponse): Promise<string | null> {
  switch (notification.type) {
    case 'Reply':
    case 'Mention': {
      const comment = await getComment(notification.sourceId)
      if (!comment.postId) return null
      const params = new URLSearchParams({ highlightComment: comment.id })
      if (comment.ancestorCommentIds?.length) {
        params.set('ancestors', comment.ancestorCommentIds.join(','))
      }
      return `/posts/${comment.postId}?${params.toString()}`
    }
    case 'FriendRequest':
      return '/friends/requests'
    case 'ModAction':
      return null
  }
}

export function NotificationItem({ notification }: { notification: NotificationResponse }) {
  const { t, i18n } = useTranslation('notifications')
  const [isNavigating, setIsNavigating] = useState(false)
  const navigate = useNavigate()
  const markRead = useMarkNotificationRead()

  async function handleClick() {
    if (!notification.isRead) {
      markRead.mutate(notification.id)
    }

    setIsNavigating(true)
    try {
      const target = await resolveTarget(notification)
      if (target) {
        navigate(target)
      }
    } catch {
      toast.error(t('item.openErrorToast'))
    } finally {
      setIsNavigating(false)
    }
  }

  const isClickable = notification.type !== 'ModAction'

  return (
    <li>
      <button
        type="button"
        onClick={isClickable ? handleClick : undefined}
        disabled={isNavigating}
        className={`flex w-full flex-col items-start gap-1 rounded-lg border p-3 text-start ${
          isClickable ? 'hover:bg-muted' : 'cursor-default'
        } ${notification.isRead ? '' : 'bg-accent/40'}`}
      >
        <div className="flex w-full items-center justify-between gap-2">
          <Badge variant={notification.isRead ? 'outline' : 'default'}>
            {t(`types.${notification.type}`)}
          </Badge>
          <span className="text-muted-foreground text-xs">
            {new Date(notification.createdAtUtc).toLocaleString(i18n.language)}
          </span>
        </div>
        <p className="text-sm">{notification.previewText}</p>
      </button>
    </li>
  )
}
