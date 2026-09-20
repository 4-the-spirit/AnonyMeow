import type { ReactionSummaryResponse } from '@/features/reactions/types'

export interface CommentResponse {
  id: string
  authorUsername: string
  authorDisplayName: string | null
  authorAvatarSeed: string | null
  bodyMarkdown: string
  score: number
  replyCount: number
  createdAtUtc: string
  editedAtUtc: string | null
  /** null only on the create-comment response — the backend omits it there instead of []. */
  reactions: ReactionSummaryResponse[] | null
  postId?: string
  /** Root -> immediate-parent chain; only populated alongside postId. Empty for a top-level comment. */
  ancestorCommentIds?: string[]
  /** Populated alongside postId — only the assembly-backed listings (saved/top-level/replies/search) set this. */
  communityName?: string | null
  /** The viewer's own +1/-1 vote on this comment; null if they haven't voted (or aren't signed in). */
  viewerVote?: 1 | -1 | null
}

export interface CreateCommentRequest {
  bodyMarkdown: string
  parentCommentId?: string
}

export interface UpdateCommentRequest {
  bodyMarkdown: string
}
