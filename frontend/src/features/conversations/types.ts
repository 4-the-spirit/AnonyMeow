export interface ConversationResponse {
  id: string
  otherUsername: string
  otherUserDisplayName: string | null
  /** The other participant's AppUser id, resolved relative to the caller — lets the frontend
   * derive isMine on a message without needing an endpoint that exposes the caller's own id. */
  otherUserId: string
  createdAtUtc: string
  /** Null if the other participant has never been seen (or presence can't be resolved). Compare
   * against a short "online" threshold client-side; older than that, show a relative "last seen". */
  otherUserLastSeenAt: string | null
  otherUserAvatarSeed: string | null
  isPinned: boolean
}

export interface MessageResponse {
  id: string
  conversationId: string
  senderId: string
  body: string
  isRead: boolean
  createdAtUtc: string
  replyToMessageId: string | null
  /** Null both when this isn't a reply and when the replied-to message could no longer be
   * resolved (e.g. removed) — use replyToMessageId to tell those two cases apart if needed. */
  replyToSenderId: string | null
  replyToBodyPreview: string | null
}

export interface StartConversationRequest {
  username: string
}

export interface SendMessageRequest {
  body: string
  replyToMessageId?: string
}
