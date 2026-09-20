import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { NotificationResponse } from './types'

export function listNotifications(unreadOnly: boolean, page: number) {
  return apiFetch<PagedResponse<NotificationResponse>>('/api/notifications', {
    searchParams: { unreadOnly, page },
  })
}

export function markNotificationRead(id: string) {
  return apiFetch<void>(`/api/notifications/${id}/read`, { method: 'POST' })
}

export function markAllNotificationsRead() {
  return apiFetch<void>('/api/notifications/read-all', { method: 'POST' })
}
