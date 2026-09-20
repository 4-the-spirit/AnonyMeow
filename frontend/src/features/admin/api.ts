import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type { ReportResponse, ReportStatus } from '@/features/moderation/types'
import type {
  CreatePlatformRestrictionRequest,
  ModerationActionResponse,
  PlatformRestrictionResponse,
  SpamFlagResponse,
  SpamFlagStatus,
} from './types'

export function listAllReports(status: ReportStatus | 'all', page: number) {
  return apiFetch<PagedResponse<ReportResponse>>('/api/admin/reports', {
    searchParams: { status: status === 'all' ? undefined : status, page },
  })
}

export function listSpamFlags(status: SpamFlagStatus | 'all', page: number) {
  return apiFetch<PagedResponse<SpamFlagResponse>>('/api/admin/spam-flags', {
    searchParams: { status: status === 'all' ? undefined : status, page },
  })
}

export function restrictUser(username: string, body: CreatePlatformRestrictionRequest) {
  return apiFetch<PlatformRestrictionResponse>(
    `/api/admin/users/${encodeURIComponent(username)}/restrict`,
    { method: 'POST', body }
  )
}

export function liftRestriction(username: string) {
  return apiFetch<void>(`/api/admin/users/${encodeURIComponent(username)}/restrict`, {
    method: 'DELETE',
  })
}

export function listAuditLog(page: number) {
  return apiFetch<PagedResponse<ModerationActionResponse>>('/api/admin/audit-log', {
    searchParams: { page },
  })
}
