import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { MoreVertical, Pin, PinOff, Trash2 } from 'lucide-react'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ConfirmDialog/ConfirmDialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { getDisplayName } from '@/lib/userDisplay'
import { useConversations, useDeleteConversation, useSetConversationPin } from '../hooks'
import { PresenceIndicator } from '../components/PresenceIndicator'
import { isOnline } from '../presence'
import type { ConversationResponse } from '../types'

export function ConversationsListPage() {
  const { t, i18n } = useTranslation('conversations')
  const [page, setPage] = useState(1)
  const conversations = useConversations(page)
  const setPinned = useSetConversationPin()
  const deleteConversation = useDeleteConversation()
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  async function handleTogglePin(conversation: ConversationResponse) {
    try {
      await setPinned.mutateAsync({ conversationId: conversation.id, pinned: !conversation.isPinned })
    } catch {
      toast.error(t('list.pinErrorToast'))
    }
  }

  async function handleDelete(conversationId: string) {
    try {
      await deleteConversation.mutateAsync(conversationId)
      toast.success(t('list.deleteSuccessToast'))
    } catch {
      toast.error(t('list.deleteErrorToast'))
    }
  }

  if (conversations.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (conversations.isError) {
    return <ErrorState error={conversations.error} onRetry={() => conversations.refetch()} />
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t('list.title')}</h1>

      {conversations.data.items.length === 0 ? (
        <EmptyState
          title={t('list.noneYetTitle')}
          description={t('list.noneYetDescription')}
        />
      ) : (
        <>
          <ul className="flex flex-col gap-2">
            {conversations.data.items.map((conversation) => (
              <li
                key={conversation.id}
                className="hover:bg-muted flex items-center gap-1 rounded-lg border ps-3"
              >
                <Link
                  to={`/messages/${conversation.id}`}
                  className="flex min-w-0 flex-1 items-center gap-3 py-3"
                >
                  <div className="relative shrink-0">
                    <UserAvatar
                      seed={conversation.otherUserAvatarSeed}
                      username={conversation.otherUsername}
                      className="size-10"
                    />
                    {isOnline(conversation.otherUserLastSeenAt) && (
                      <span className="border-background absolute -inset-e-0.5 -bottom-0.5 size-3 rounded-full border-2 bg-green-500" />
                    )}
                  </div>
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="flex items-center gap-1 truncate text-base font-semibold">
                      {conversation.isPinned && (
                        <Pin className="text-muted-foreground size-3.5 shrink-0" />
                      )}
                      {getDisplayName({
                        username: conversation.otherUsername,
                        displayName: conversation.otherUserDisplayName,
                      })}
                    </span>
                    <PresenceIndicator lastSeenAt={conversation.otherUserLastSeenAt} showLabel />
                  </div>
                  <span className="text-muted-foreground shrink-0 text-xs">
                    {new Date(conversation.createdAtUtc).toLocaleDateString(i18n.language)}
                  </span>
                </Link>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      className="me-2 shrink-0"
                      aria-label={t('list.actionsLabel')}
                    >
                      <MoreVertical className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => handleTogglePin(conversation)}>
                      {conversation.isPinned ? <PinOff /> : <Pin />}
                      {conversation.isPinned ? t('list.unpin') : t('list.pin')}
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      variant="destructive"
                      onClick={() => setPendingDeleteId(conversation.id)}
                    >
                      <Trash2 />
                      {t('list.delete')}
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </li>
            ))}
          </ul>
          <PaginationControl
            page={conversations.data.page}
            pageSize={conversations.data.pageSize}
            totalCount={conversations.data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}

      <ConfirmDialog
        open={pendingDeleteId !== null}
        onOpenChange={(open) => !open && setPendingDeleteId(null)}
        title={t('list.deleteConfirm')}
        onConfirm={() => pendingDeleteId && handleDelete(pendingDeleteId)}
      />
    </div>
  )
}
