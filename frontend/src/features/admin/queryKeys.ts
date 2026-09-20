import type { ReportStatus } from '@/features/moderation/types'
import type { SpamFlagStatus } from './types'

export const adminKeys = {
  reports: (status: ReportStatus | 'all', page: number) => ['admin', 'reports', status, page] as const,
  spamFlags: (status: SpamFlagStatus | 'all', page: number) =>
    ['admin', 'spamFlags', status, page] as const,
  auditLog: (page: number) => ['admin', 'auditLog', page] as const,
}
