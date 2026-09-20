import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import * as api from './api'
import { notificationKeys } from './queryKeys'

export function useNotifications(unreadOnly: boolean, page: number) {
  return useQuery({
    queryKey: notificationKeys.list(unreadOnly, page),
    queryFn: () => api.listNotifications(unreadOnly, page),
  })
}

/** Lean read of just the unread total, for the AppLayout bell badge. */
export function useUnreadNotificationCount() {
  const { status } = useAuth()
  return useQuery({
    queryKey: notificationKeys.unreadCount,
    queryFn: () => api.listNotifications(true, 1),
    select: (data) => data.totalCount,
    enabled: status === 'ready',
  })
}

function useInvalidateNotifications() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['notifications'] })
}

export function useMarkNotificationRead() {
  const invalidate = useInvalidateNotifications()
  return useMutation({
    mutationFn: (id: string) => api.markNotificationRead(id),
    onSuccess: invalidate,
  })
}

export function useMarkAllNotificationsRead() {
  const invalidate = useInvalidateNotifications()
  return useMutation({
    mutationFn: api.markAllNotificationsRead,
    onSuccess: invalidate,
  })
}
