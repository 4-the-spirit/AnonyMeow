import type { ReportStatus } from './types'

export const moderationKeys = {
  reports: (communityName: string, status: ReportStatus | 'all', page: number) =>
    ['moderation', communityName, 'reports', status, page] as const,
  bans: (communityName: string) => ['moderation', communityName, 'bans'] as const,
}
