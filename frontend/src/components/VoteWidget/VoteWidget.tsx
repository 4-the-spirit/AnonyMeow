import { useEffect, useRef, useState } from 'react'
import { ArrowBigUp, ArrowBigDown } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { useRequireAuth } from '@/features/auth/useRequireAuth'
import { useCastVote, type VoteTargetType } from '@/features/voting/useCastVote'

interface VoteWidgetProps {
  targetType: VoteTargetType
  targetId: string
  score: number
  /** The viewer's own vote as reported by the server; null/undefined if they haven't voted. */
  viewerVote?: 1 | -1 | null
  invalidateKey: readonly unknown[]
  orientation?: 'vertical' | 'horizontal'
}

/**
 * Mirrors ReactionBar's pattern: `myVote` is derived from the server-sourced `viewerVote` prop
 * so it survives reloads/remounts, with a local `override` layered on top only until the
 * cast/remove mutation's invalidation-driven refetch lands (then it self-reconciles).
 */
export function VoteWidget({
  targetType,
  targetId,
  score,
  viewerVote = null,
  invalidateKey,
  orientation = 'vertical',
}: VoteWidgetProps) {
  const { t } = useTranslation('common')
  const { requireAuth } = useRequireAuth()
  const [override, setOverride] = useState<1 | -1 | 0 | null>(null)
  const myVote = override ?? viewerVote ?? 0
  const { cast, remove, isPending } = useCastVote({ targetType, targetId, invalidateKey })

  // A cast/remove mutation invalidates `invalidateKey`, which refetches the authoritative
  // `score` from the server — the only thing that changes `score` while this widget stays
  // mounted (no polling/live updates). Until that refetch lands, `score` is stale by exactly
  // our own pending vote, so `overlay` bridges the gap. Once `score` changes, it already
  // reflects our vote, so the overlay is cleared to avoid double-counting it on top.
  const [overlay, setOverlay] = useState(0)
  const lastScoreRef = useRef(score)
  useEffect(() => {
    if (score !== lastScoreRef.current) {
      lastScoreRef.current = score
      setOverlay(0)
    }
  }, [score])

  // Same refetch-lands reconciliation, but for the vote direction itself.
  const lastViewerVoteRef = useRef(viewerVote)
  useEffect(() => {
    if (viewerVote !== lastViewerVoteRef.current) {
      lastViewerVoteRef.current = viewerVote
      setOverride(null)
    }
  }, [viewerVote])

  async function handleVote(value: 1 | -1) {
    const isRemoving = myVote === value || myVote !== 0
    const delta = isRemoving ? -myVote : value
    setOverlay((o) => o + delta)
    try {
      if (isRemoving) {
        // Also covers the swing case: un-vote first (net -1), the opposite vote is cast
        // on the next click, rather than moving karma by 2 in a single click.
        await remove.mutateAsync()
        setOverride(0)
      } else {
        await cast.mutateAsync(value)
        setOverride(value)
      }
    } catch {
      setOverlay((o) => o - delta)
      toast.error(t('voting.errorToast'))
    }
  }

  const displayedScore = score + overlay

  return (
    <div
      className={cn(
        'flex items-center gap-1',
        orientation === 'vertical' && 'flex-col'
      )}
    >
      <button
        type="button"
        aria-label={t('voting.upvote')}
        aria-pressed={myVote === 1}
        disabled={isPending}
        onClick={() => requireAuth(() => handleVote(1))}
        className={cn(
          'rounded p-0.5 hover:bg-muted disabled:opacity-50',
          myVote === 1 && 'text-primary'
        )}
      >
        <ArrowBigUp className="size-5" fill={myVote === 1 ? 'currentColor' : 'none'} />
      </button>
      <span className="min-w-4 text-center text-sm font-medium tabular-nums">
        {displayedScore}
      </span>
      <button
        type="button"
        aria-label={t('voting.downvote')}
        aria-pressed={myVote === -1}
        disabled={isPending}
        onClick={() => requireAuth(() => handleVote(-1))}
        className={cn(
          'rounded p-0.5 hover:bg-muted disabled:opacity-50',
          myVote === -1 && 'text-destructive'
        )}
      >
        <ArrowBigDown className="size-5" fill={myVote === -1 ? 'currentColor' : 'none'} />
      </button>
    </div>
  )
}
