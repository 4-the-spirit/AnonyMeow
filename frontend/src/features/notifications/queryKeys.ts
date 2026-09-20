export const notificationKeys = {
  list: (unreadOnly: boolean, page: number) => ['notifications', 'list', unreadOnly, page] as const,
  unreadCount: ['notifications', 'unreadCount'] as const,
}
