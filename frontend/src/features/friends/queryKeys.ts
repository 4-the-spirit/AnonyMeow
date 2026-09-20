export const friendKeys = {
  list: (username: string) => ['friends', username, 'list'] as const,
  myRequests: ['friends', 'me', 'requests'] as const,
}
