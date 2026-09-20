import { apiFetch } from '@/lib/api/client'
import type { ReactionTargetType } from './types'

function reactionUrl(targetType: ReactionTargetType, targetId: string, emoji: string) {
  const segment = targetType === 'Post' ? 'posts' : 'comments'
  return `/api/${segment}/${targetId}/reactions/${encodeURIComponent(emoji)}`
}

export function addReaction(targetType: ReactionTargetType, targetId: string, emoji: string) {
  return apiFetch<void>(reactionUrl(targetType, targetId, emoji), { method: 'PUT' })
}

export function removeReaction(targetType: ReactionTargetType, targetId: string, emoji: string) {
  return apiFetch<void>(reactionUrl(targetType, targetId, emoji), { method: 'DELETE' })
}
