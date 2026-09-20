import { toast } from 'sonner'
import { MoreVertical } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { usePostModActions } from '../hooks'
import type { PostResponse } from '@/features/posts/types'

/** Only rendered when the caller has already confirmed useIsModerator(communityName). */
export function ModActionMenu({ post }: { post: PostResponse }) {
  const { t } = useTranslation('moderation')
  const actions = usePostModActions(post.id)

  async function run(action: Promise<unknown>, errorMessage: string) {
    try {
      await action
    } catch {
      toast.error(errorMessage)
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label={t('actionMenu.label')}>
          <MoreVertical className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem
          onClick={() =>
            run(
              post.isPinned ? actions.unpin.mutateAsync() : actions.pin.mutateAsync(),
              t('actionMenu.errorUpdatePinState')
            )
          }
        >
          {post.isPinned ? t('actionMenu.unpin') : t('actionMenu.pin')}
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={() =>
            run(
              post.isLocked ? actions.unlock.mutateAsync() : actions.lock.mutateAsync(),
              t('actionMenu.errorUpdateLockState')
            )
          }
        >
          {post.isLocked ? t('actionMenu.unlock') : t('actionMenu.lock')}
        </DropdownMenuItem>
        <DropdownMenuItem
          variant="destructive"
          onClick={() => run(actions.remove.mutateAsync(undefined), t('actionMenu.errorRemovePost'))}
        >
          {t('actionMenu.remove')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
