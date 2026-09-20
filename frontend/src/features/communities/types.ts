import type { CreateFlairRequest } from '@/features/flairs/types'

export interface CommunityRule {
  title: string
  description: string
}

export interface CommunityResponse {
  name: string
  description: string | null
  rules: CommunityRule[]
  iconImageUrl: string
  bannerImageUrl: string
  memberCount: number
  createdAtUtc: string
}

export interface CreateCommunityRequest {
  name: string
  description?: string
  rules?: CommunityRule[]
  flairs?: CreateFlairRequest[]
  iconImageUrl: string
  bannerImageUrl: string
}

export interface UpdateCommunityRequest {
  description?: string
  rules?: CommunityRule[]
  iconImageUrl?: string
  bannerImageUrl?: string
}

export interface CommunityModeratorResponse {
  username: string
  displayName: string | null
  joinedAtUtc: string
}

export type CommunityRole = 'Member' | 'Moderator'

export interface CommunityMembershipResponse {
  name: string
  iconImageUrl: string | null
  role: CommunityRole
  joinedAtUtc: string
}

export interface CommunityMemberResponse {
  username: string
  displayName: string | null
  avatarSeed: string | null
  role: CommunityRole
  joinedAtUtc: string
}
