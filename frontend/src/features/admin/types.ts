export type PlatformRestrictionType = 'PostingRestricted' | 'FullSuspension'
export type PlatformRestrictionStatus = 'Active' | 'Lifted'
export type SpamFlagStatus = 'Open' | 'Reviewed' | 'Dismissed' | 'ActionTaken'
export type SpamFlagReason = 'RateLimitExceeded' | 'DuplicateContent' | 'LinkSpam'
export type SpamFlagTargetType = 'Post' | 'Comment' | 'DirectMessage'

export interface SpamFlagResponse {
  id: string
  targetType: SpamFlagTargetType
  targetId: string
  authorId: string
  reason: SpamFlagReason
  status: SpamFlagStatus
  reportId: string
  createdAtUtc: string
}

export interface PlatformRestrictionResponse {
  id: string
  username: string
  type: PlatformRestrictionType
  reason: string
  startAtUtc: string
  endAtUtc: string | null
  status: PlatformRestrictionStatus
}

export interface CreatePlatformRestrictionRequest {
  type: PlatformRestrictionType
  reason: string
  endAtUtc?: string
}

export interface ModerationActionResponse {
  id: string
  actionType: string
  targetId: string
  reason: string | null
  createdAtUtc: string
}
