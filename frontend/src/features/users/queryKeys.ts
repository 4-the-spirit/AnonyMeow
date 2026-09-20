export const userKeys = {
  me: ['users', 'me'] as const,
  detail: (username: string) => ['users', 'detail', username] as const,
  posts: (username: string, page: number) => ['users', username, 'posts', page] as const,
  comments: (username: string, page: number) =>
    ['users', username, 'comments', page] as const,
  communities: (username: string, page: number) =>
    ['users', username, 'communities', page] as const,
  usernameAvailability: (value: string) => ['users', 'check-username', value] as const,
}
