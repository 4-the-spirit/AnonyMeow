export type ReportTargetType = 'Post' | 'Comment' | 'DirectMessage'
export type ReportStatus = 'Open' | 'Reviewed' | 'Dismissed' | 'ActionTaken'
export type ReportOutcome = 'Dismiss' | 'ActionTaken'
export type ReportReasonCategory =
  | 'Spam'
  | 'Harassment'
  | 'HateSpeech'
  | 'Violence'
  | 'Misinformation'
  | 'Nsfw'
  | 'Other'

export interface ReportResponse {
  id: string
  targetType: ReportTargetType
  targetId: string
  category: ReportReasonCategory
  details: string | null
  status: ReportStatus
  createdAtUtc: string
}

export interface CreateReportRequest {
  category: ReportReasonCategory
  details?: string
}

export interface ResolveReportRequest {
  outcome: ReportOutcome
  actionReason?: string
}

export interface CreateBanRequest {
  username: string
  reason: string
}

export interface BanResponse {
  username: string
  reason: string
  createdAtUtc: string
}
