import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from './api'
import type { ReactionTargetType } from './types'

interface UseToggleReactionOptions {
  targetType: ReactionTargetType
  targetId: string
  /** Invalidated after a successful add/remove so the summary refetches. */
  invalidateKey: readonly unknown[]
}

/**
 * Shared reaction mutation for both posts and comments — add/remove return 204, so callers
 * (see ReactionBar) apply a local optimistic overlay rather than waiting on this to resolve.
 */
export function useToggleReaction({ targetType, targetId, invalidateKey }: UseToggleReactionOptions) {
  const queryClient = useQueryClient()

  const toggle = useMutation({
    mutationFn: ({ emoji, reacted }: { emoji: string; reacted: boolean }) =>
      reacted
        ? api.removeReaction(targetType, targetId, emoji)
        : api.addReaction(targetType, targetId, emoji),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invalidateKey }),
  })

  return { toggle, isPending: toggle.isPending }
}
