import type { FlairResponse } from '@/features/flairs/types'
import type { ReactionSummaryResponse } from '@/features/reactions/types'

export type SortOrder = 'hot' | 'new' | 'top' | 'controversial' | 'pinned'

export interface PollOptionResponse {
  id: string
  text: string
  voteCount: number
}

export interface PostResponse {
  id: string
  communityName: string
  authorUsername: string
  authorDisplayName: string | null
  authorAvatarSeed: string | null
  title: string
  bodyMarkdown: string | null
  url: string | null
  imageUrls: string[]
  score: number
  commentCount: number
  isPinned: boolean
  isLocked: boolean
  createdAtUtc: string
  editedAtUtc: string | null
  pollOptions: PollOptionResponse[] | null
  flair: FlairResponse | null
  reactions: ReactionSummaryResponse[]
  /** The viewer's own +1/-1 vote on this post; null if they haven't voted (or aren't signed in). */
  viewerVote: 1 | -1 | null
}

export interface CreatePostRequest {
  title: string
  bodyMarkdown?: string
  url?: string
  imageUrls?: string[]
  pollOptions?: string[]
  flairId: string
}

export interface UpdatePostRequest {
  title?: string
  bodyMarkdown?: string
  url?: string
}

export interface CastPollVoteRequest {
  pollOptionId: string
}

export interface CastVoteRequest {
  value: 1 | -1
}
