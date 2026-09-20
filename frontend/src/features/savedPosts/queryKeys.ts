export const savedPostKeys = {
  all: ['savedPosts'] as const,
  list: (page: number) => ['savedPosts', 'list', page] as const,
}
