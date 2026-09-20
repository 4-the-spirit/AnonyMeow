import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useCastPollVote } from '../hooks'
import type { PollOptionResponse } from '../types'

export function PollVoteWidget({
  postId,
  options,
}: {
  postId: string
  options: PollOptionResponse[]
}) {
  const { t } = useTranslation('posts')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const castVote = useCastPollVote(postId)
  const totalVotes = options.reduce((sum, o) => sum + o.voteCount, 0)

  async function vote(optionId: string) {
    try {
      await castVote.mutateAsync({ pollOptionId: optionId })
      setSelectedId(optionId)
    } catch {
      toast.error(t('detail.pollVoteErrorToast'))
    }
  }

  return (
    <div className="flex flex-col gap-2">
      {options.map((option) => {
        const percent = totalVotes > 0 ? Math.round((option.voteCount / totalVotes) * 100) : 0
        return (
          <Button
            key={option.id}
            type="button"
            variant="outline"
            disabled={castVote.isPending}
            onClick={() => vote(option.id)}
            className={cn(
              'relative h-auto justify-start overflow-hidden py-2',
              selectedId === option.id && 'border-primary'
            )}
          >
            <span
              className="bg-accent absolute inset-y-0 start-0 -z-10"
              style={{ width: `${percent}%` }}
              aria-hidden
            />
            <span className="flex w-full justify-between">
              <span>{option.text}</span>
              <span className="text-muted-foreground text-xs">
                {option.voteCount} ({percent}%)
              </span>
            </span>
          </Button>
        )
      })}
    </div>
  )
}
