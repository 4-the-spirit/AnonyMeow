export const savedCommentKeys = {
  all: ['savedComments'] as const,
  list: (page: number) => ['savedComments', 'list', page] as const,
}
