export const communityKeys = {
  list: (search: string, page: number) => ['communities', 'list', search, page] as const,
  detail: (name: string) => ['communities', 'detail', name] as const,
  moderators: (name: string) => ['communities', name, 'moderators'] as const,
  members: (name: string, search: string, page: number) =>
    ['communities', name, 'members', search, page] as const,
}
