import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { tokenStorage } from '@/lib/auth/tokenStorage'
import type { MessageResponse } from '@/features/conversations/types'
import { createNotificationHubConnection } from './hubConnection'
import type { NotificationResponse } from './types'

/**
 * Mounted once inside AppLayout (which only renders once auth status is "ready", so a token is
 * guaranteed available). Keeps the notification bell/list live without polling.
 */
export function useNotificationHubConnection() {
  const queryClient = useQueryClient()

  useEffect(() => {
    const token = tokenStorage.getToken()
    if (!token) return

    const connection = createNotificationHubConnection(token)

    connection.on('ReceiveNotification', (notification: NotificationResponse) => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
      toast.info(notification.previewText)
    })

    // DM delivery reuses this same hub/group rather than a second one (see the Phase 5 backend
    // log) — refetch the conversation's messages and the conversation list (new-conversation
    // case) whenever one arrives.
    connection.on('ReceiveMessage', (message: MessageResponse) => {
      queryClient.invalidateQueries({
        queryKey: ['conversations', message.conversationId, 'messages'],
      })
      queryClient.invalidateQueries({ queryKey: ['conversations', 'list'] })
    })

    connection.start().catch(() => {
      // Silent: the notification list still works via polling/manual refresh if the hub can't
      // connect (e.g. dev token expired); no need to surface a toast for a background channel.
    })

    return () => {
      connection.stop()
    }
  }, [queryClient])
}
