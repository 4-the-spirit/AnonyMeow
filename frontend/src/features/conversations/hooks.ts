import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import * as api from './api'
import { conversationKeys } from './queryKeys'
import type { SendMessageRequest, StartConversationRequest } from './types'

export function useConversations(page: number) {
  return useQuery({
    queryKey: conversationKeys.list(page),
    queryFn: () => api.listConversations(page),
  })
}

/** Polled while a conversation is open so the presence indicator (online/last-seen) stays
 * reasonably fresh without needing a live socket — matches the heartbeat-based presence model. */
export function useConversation(conversationId: string) {
  return useQuery({
    queryKey: conversationKeys.detail(conversationId),
    queryFn: () => api.getConversation(conversationId),
    enabled: !!conversationId,
    refetchInterval: 30_000,
  })
}

export function useMessages(conversationId: string, page: number) {
  return useQuery({
    queryKey: conversationKeys.messages(conversationId, page),
    queryFn: () => api.listMessages(conversationId, page),
    enabled: !!conversationId,
  })
}

/** On success, navigates straight to the (possibly just-created, possibly already-existing —
 * the backend get-or-creates) conversation. */
export function useStartConversation() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  return useMutation({
    mutationFn: (body: StartConversationRequest) => api.startConversation(body),
    onSuccess: (conversation) => {
      queryClient.invalidateQueries({ queryKey: ['conversations', 'list'] })
      navigate(`/messages/${conversation.id}`)
    },
  })
}

export function useSendMessage(conversationId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: SendMessageRequest) => api.sendMessage(conversationId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations', conversationId, 'messages'] })
    },
  })
}

export function useSetConversationPin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ conversationId, pinned }: { conversationId: string; pinned: boolean }) =>
      api.setConversationPin(conversationId, pinned),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations', 'list'] })
    },
  })
}

export function useDeleteConversation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (conversationId: string) => api.deleteConversation(conversationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations', 'list'] })
    },
  })
}
