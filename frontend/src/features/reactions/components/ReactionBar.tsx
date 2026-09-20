import { useState } from 'react'
import { SmilePlus } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { AnimatedEmoji } from '@/components/AnimatedEmoji/AnimatedEmoji'
import { useRequireAuth } from '@/features/auth/useRequireAuth'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { formatCompactNumber } from '@/lib/formatCompactNumber'
import { ALLOWED_REACTION_EMOJIS } from '../constants'
import { useToggleReaction } from '../hooks'
import type { ReactionSummaryResponse, ReactionTargetType } from '../types'

interface ReactionBarProps {
  targetType: ReactionTargetType
  targetId: string
  reactions: ReactionSummaryResponse[]
  invalidateKey: readonly unknown[]
}

/**
 * A viewer may hold at most one active reaction per target — the server replaces any prior
 * different-emoji reaction on add — so this renders as a single button (icon + label, styled
 * like Reply/Report) rather than a row of independently toggleable emoji chips. Picking the
 * already-active emoji removes it; picking a different one switches to it. Hovering the
 * button reveals the per-emoji breakdown.
 */
export function ReactionBar({ targetType, targetId, reactions, invalidateKey }: ReactionBarProps) {
  const { t, i18n } = useTranslation('reactions')
  const { requireAuth } = useRequireAuth()
  const [override, setOverride] = useState<{ emoji: string | null } | null>(null)
  const { toggle, isPending } = useToggleReaction({ targetType, targetId, invalidateKey })

  const viewerReaction = reactions.find((r) => r.reactedByViewer)?.emoji ?? null
  const currentEmoji = override ? override.emoji : viewerReaction
  const breakdown = reactions.filter((r) => r.count > 0).sort((a, b) => b.count - a.count)

  async function handleSelect(emoji: string) {
    const reacted = currentEmoji === emoji
    try {
      await toggle.mutateAsync({ emoji, reacted })
      setOverride({ emoji: reacted ? null : emoji })
    } catch {
      toast.error(t('errorToast'))
    }
  }

  return (
    <Tooltip>
      <DropdownMenu>
        <TooltipTrigger asChild>
          <DropdownMenuTrigger asChild>
            <Button type="button" variant="ghost" size="sm" disabled={isPending}>
              {currentEmoji ? (
                <AnimatedEmoji emoji={currentEmoji} size={14} />
              ) : (
                <SmilePlus className="size-3.5" />
              )}
              {t('reactButton')}
            </Button>
          </DropdownMenuTrigger>
        </TooltipTrigger>
        <DropdownMenuContent align="start" className="flex w-auto flex-row flex-wrap gap-1 p-1">
          {ALLOWED_REACTION_EMOJIS.map((emoji) => (
            <DropdownMenuItem
              key={emoji}
              className="justify-center px-1.5 py-1"
              onClick={() => requireAuth(() => handleSelect(emoji))}
            >
              <AnimatedEmoji emoji={emoji} size={36} />
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>
      <TooltipContent className="flex-row items-start gap-3 p-2">
        {breakdown.length > 0 ? (
          breakdown.map((r) => (
            <div key={r.emoji} className="flex flex-col items-center gap-0.5">
              <AnimatedEmoji emoji={r.emoji} size={20} />
              <span className="tabular-nums">{formatCompactNumber(r.count, i18n.language)}</span>
            </div>
          ))
        ) : (
          <span>{t('reactButton')}</span>
        )}
      </TooltipContent>
    </Tooltip>
  )
}
