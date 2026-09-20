import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'

export type VoteTargetType = 'Post' | 'Comment'

function voteUrl(targetType: VoteTargetType, targetId: string) {
  const segment = targetType === 'Post' ? 'posts' : 'comments'
  return `/api/${segment}/${targetId}/vote`
}

interface UseCastVoteOptions {
  targetType: VoteTargetType
  targetId: string
  /** Invalidated after a successful cast/remove so score refetches. */
  invalidateKey: readonly unknown[]
}

/**
 * Shared voting mutation for both posts and comments — PostResponse/CommentResponse only
 * expose aggregate score, never "my vote", so callers must track vote intent as local UI
 * state (see VoteWidget); this hook only owns the network call.
 */
export function useCastVote({ targetType, targetId, invalidateKey }: UseCastVoteOptions) {
  const queryClient = useQueryClient()

  const cast = useMutation({
    mutationFn: (value: 1 | -1) =>
      apiFetch<void>(voteUrl(targetType, targetId), { method: 'PUT', body: { value } }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invalidateKey }),
  })

  const remove = useMutation({
    mutationFn: () => apiFetch<void>(voteUrl(targetType, targetId), { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invalidateKey }),
  })

  return { cast, remove, isPending: cast.isPending || remove.isPending }
}
