export const conversationKeys = {
  list: (page: number) => ['conversations', 'list', page] as const,
  detail: (conversationId: string) => ['conversations', 'detail', conversationId] as const,
  messages: (conversationId: string, page: number) =>
    ['conversations', conversationId, 'messages', page] as const,
}
