import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { BlockActionButton } from '@/features/blocks/components/BlockActionButton'
import { useBlockedUsersCache } from '@/features/blocks/hooks'
import { useConversation, useMessages } from '../hooks'
import { MessageBubble } from '../components/MessageBubble'
import { MessageComposer } from '../components/MessageComposer'
import { PresenceIndicator } from '../components/PresenceIndicator'
import { getDisplayName } from '@/lib/userDisplay'
import type { MessageResponse } from '../types'

export function ConversationPage() {
  const { t } = useTranslation('conversations')
  const { conversationId = '' } = useParams()
  const [page, setPage] = useState(1)
  const [replyTo, setReplyTo] = useState<MessageResponse | null>(null)
  const [blockedOverride, setBlockedOverride] = useState<boolean | null>(null)
  const { isBlocked } = useBlockedUsersCache()
  const conversation = useConversation(conversationId)
  const messages = useMessages(conversationId, page)

  if (conversation.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (conversation.isError) {
    return <ErrorState error={conversation.error} onRetry={() => conversation.refetch()} />
  }

  const { data: conv } = conversation
  const blockedByMe = blockedOverride ?? isBlocked(conv.otherUsername)
  const otherDisplayName = getDisplayName({ username: conv.otherUsername, displayName: conv.otherUserDisplayName })

  return (
    // Fixed to the viewport height (minus the app shell's header + main padding) so the message
    // list is the only thing that scrolls — the composer below it stays fully visible even as its
    // textarea grows, instead of being pushed off-screen and requiring a page-level scroll.
    <div className="mx-auto flex h-[calc(100dvh-116px)] max-w-lg flex-col gap-4">
      <div className="flex shrink-0 items-center gap-3">
        <Link to="/messages" className="text-muted-foreground hover:text-foreground">
          <ArrowLeft className="size-5 rtl:rotate-180" />
        </Link>
        <UserAvatar seed={conv.otherUserAvatarSeed} username={conv.otherUsername} className="size-9" />
        <div className="flex min-w-0 flex-1 flex-col">
          <h1 className="truncate text-xl font-bold">{otherDisplayName}</h1>
          <PresenceIndicator lastSeenAt={conv.otherUserLastSeenAt} showLabel />
        </div>
        <BlockActionButton username={conv.otherUsername} onBlockedChange={setBlockedOverride} />
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto">
        {messages.isPending ? (
          <div className="flex justify-center py-10">
            <LoadingSpinner />
          </div>
        ) : messages.isError ? (
          <ErrorState error={messages.error} onRetry={() => messages.refetch()} />
        ) : messages.data.items.length === 0 ? (
          <EmptyState title={t('page.noneYetTitle')} description={t('page.noneYetDescription')} />
        ) : (
          <>
            <div className="flex flex-col gap-3">
              {messages.data.items.map((message) => (
                <MessageBubble
                  key={message.id}
                  message={message}
                  isMine={message.senderId !== conv.otherUserId}
                  onReply={setReplyTo}
                />
              ))}
            </div>
            <PaginationControl
              page={messages.data.page}
              pageSize={messages.data.pageSize}
              totalCount={messages.data.totalCount}
              onPageChange={setPage}
            />
          </>
        )}
      </div>

      <div className="bg-background shrink-0 border-t px-4 pt-3 pb-3 -mx-4">
        <MessageComposer
          conversationId={conversationId}
          otherDisplayName={otherDisplayName}
          blockedByMe={blockedByMe}
          replyTo={replyTo}
          onCancelReply={() => setReplyTo(null)}
        />
      </div>
    </div>
  )
}
