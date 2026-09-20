import { Reply } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { ReportDialog } from '@/features/moderation/components/ReportDialog'
import type { MessageResponse } from '../types'

interface MessageBubbleProps {
  message: MessageResponse
  isMine: boolean
  onReply?: (message: MessageResponse) => void
}

export function MessageBubble({ message, isMine, onReply }: MessageBubbleProps) {
  const { t, i18n } = useTranslation('conversations')
  const isReply = message.replyToMessageId !== null

  return (
    <div className={cn('group flex flex-col gap-1', isMine ? 'items-end' : 'items-start')}>
      {isReply && (
        <div
          className={cn(
            'text-muted-foreground border-muted-foreground/30 max-w-[75%] truncate rounded-t-lg border-s-2 px-2 py-1 text-xs',
            isMine ? 'me-1' : 'ms-1'
          )}
        >
          {message.replyToBodyPreview ?? t('reply.originalUnavailable')}
        </div>
      )}
      <div
        className={cn(
          'max-w-[75%] rounded-2xl px-3 py-2 text-base wrap-break-word',
          isMine ? 'bg-primary text-primary-foreground' : 'bg-muted'
        )}
      >
        {message.body}
      </div>
      <div className="flex items-center gap-2">
        <span className="text-muted-foreground text-xs">
          {new Date(message.createdAtUtc).toLocaleTimeString(i18n.language)}
        </span>
        <span className="flex items-center gap-1 opacity-0 group-hover:opacity-100">
          {onReply && (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={t('reply.replyButton')}
              title={t('reply.replyButton')}
              onClick={() => onReply(message)}
            >
              <Reply className="size-3.5" />
            </Button>
          )}
          {!isMine && <ReportDialog targetType="DirectMessage" targetId={message.id} />}
        </span>
      </div>
    </div>
  )
}
