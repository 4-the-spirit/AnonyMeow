import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type {
  ConversationResponse,
  MessageResponse,
  SendMessageRequest,
  StartConversationRequest,
} from './types'

export function startConversation(body: StartConversationRequest) {
  return apiFetch<ConversationResponse>('/api/conversations', { method: 'POST', body })
}

export function listConversations(page: number) {
  return apiFetch<PagedResponse<ConversationResponse>>('/api/conversations', {
    searchParams: { page },
  })
}

export function getConversation(conversationId: string) {
  return apiFetch<ConversationResponse>(`/api/conversations/${conversationId}`)
}

export function listMessages(conversationId: string, page: number) {
  return apiFetch<PagedResponse<MessageResponse>>(
    `/api/conversations/${conversationId}/messages`,
    { searchParams: { page } }
  )
}

export function sendMessage(conversationId: string, body: SendMessageRequest) {
  return apiFetch<MessageResponse>(`/api/conversations/${conversationId}/messages`, {
    method: 'POST',
    body,
  })
}

export function setConversationPin(conversationId: string, pinned: boolean) {
  return apiFetch<ConversationResponse>(`/api/conversations/${conversationId}/pin`, {
    method: 'PATCH',
    body: { pinned },
  })
}

export function deleteConversation(conversationId: string) {
  return apiFetch<void>(`/api/conversations/${conversationId}`, { method: 'DELETE' })
}
