export type NotificationType = 'Reply' | 'Mention' | 'ModAction' | 'FriendRequest'
export type NotificationSourceType = 'Comment' | 'ModerationAction' | 'User'

export interface NotificationResponse {
  id: string
  type: NotificationType
  sourceType: NotificationSourceType
  sourceId: string
  isRead: boolean
  previewText: string
  createdAtUtc: string
}
