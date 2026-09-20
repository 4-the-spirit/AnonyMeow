export type ReactionTargetType = 'Post' | 'Comment'

export interface ReactionSummaryResponse {
  emoji: string
  count: number
  reactedByViewer: boolean
}
