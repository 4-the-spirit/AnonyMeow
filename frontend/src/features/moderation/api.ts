import { apiFetch } from '@/lib/api/client'
import type { PagedResponse } from '@/lib/api/types'
import type {
  BanResponse,
  CreateBanRequest,
  CreateReportRequest,
  ReportResponse,
  ReportStatus,
  ResolveReportRequest,
} from './types'

export function reportPost(postId: string, body: CreateReportRequest) {
  return apiFetch<ReportResponse>(`/api/posts/${postId}/reports`, { method: 'POST', body })
}

export function reportComment(commentId: string, body: CreateReportRequest) {
  return apiFetch<ReportResponse>(`/api/comments/${commentId}/reports`, {
    method: 'POST',
    body,
  })
}

export function reportMessage(messageId: string, body: CreateReportRequest) {
  return apiFetch<ReportResponse>(`/api/messages/${messageId}/reports`, {
    method: 'POST',
    body,
  })
}

export function listReports(communityName: string, status: ReportStatus | 'all', page: number) {
  return apiFetch<PagedResponse<ReportResponse>>(
    `/api/communities/${encodeURIComponent(communityName)}/mod/reports`,
    { searchParams: { status: status === 'all' ? undefined : status, page } }
  )
}

export function resolveReport(
  communityName: string,
  reportId: string,
  body: ResolveReportRequest
) {
  return apiFetch<ReportResponse>(
    `/api/communities/${encodeURIComponent(communityName)}/mod/reports/${reportId}/resolve`,
    { method: 'POST', body }
  )
}

export function pinPost(postId: string) {
  return apiFetch<void>(`/api/posts/${postId}/mod/pin`, { method: 'POST' })
}
export function unpinPost(postId: string) {
  return apiFetch<void>(`/api/posts/${postId}/mod/pin`, { method: 'DELETE' })
}
export function lockPost(postId: string) {
  return apiFetch<void>(`/api/posts/${postId}/mod/lock`, { method: 'POST' })
}
export function unlockPost(postId: string) {
  return apiFetch<void>(`/api/posts/${postId}/mod/lock`, { method: 'DELETE' })
}
export function removePost(postId: string, reason?: string) {
  return apiFetch<void>(`/api/posts/${postId}/mod/remove`, {
    method: 'POST',
    searchParams: { reason },
  })
}

export function removeComment(commentId: string, reason?: string) {
  return apiFetch<void>(`/api/comments/${commentId}/mod/remove`, {
    method: 'POST',
    searchParams: { reason },
  })
}

export function createBan(communityName: string, body: CreateBanRequest) {
  return apiFetch<BanResponse>(
    `/api/communities/${encodeURIComponent(communityName)}/mod/bans`,
    { method: 'POST', body }
  )
}

export function listBans(communityName: string) {
  return apiFetch<BanResponse[]>(
    `/api/communities/${encodeURIComponent(communityName)}/mod/bans`
  )
}

export function revokeBan(communityName: string, username: string) {
  return apiFetch<void>(
    `/api/communities/${encodeURIComponent(communityName)}/mod/bans/${encodeURIComponent(username)}`,
    { method: 'DELETE' }
  )
}
