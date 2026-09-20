import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { useBlockedUsersCache, useBlockUser, useUnblockUser } from '../hooks'

interface BlockActionButtonProps {
  username: string
  /** Notified on every successful block/unblock — lets a parent (e.g. the conversation header)
   * react immediately instead of only picking up the change on its next unrelated re-render. */
  onBlockedChange?: (blocked: boolean) => void
}

export function BlockActionButton({ username, onBlockedChange }: BlockActionButtonProps) {
  const { t } = useTranslation('moderation')
  const { isBlocked } = useBlockedUsersCache()
  const [blocked, setBlocked] = useState(() => isBlocked(username))
  const block = useBlockUser(username)
  const unblock = useUnblockUser(username)

  async function handleClick() {
    try {
      if (blocked) {
        await unblock.mutateAsync()
        setBlocked(false)
        onBlockedChange?.(false)
        toast.success(t('blocks.unblockedSuccessToast', { username: `u/${username}` }))
      } else {
        await block.mutateAsync()
        setBlocked(true)
        onBlockedChange?.(true)
        toast.success(t('blocks.blockedSuccessToast', { username: `u/${username}` }))
      }
    } catch {
      toast.error(blocked ? t('blocks.unblockErrorToast') : t('blocks.blockErrorToast'))
    }
  }

  const isPending = block.isPending || unblock.isPending

  return (
    <Button variant="outline" onClick={handleClick} disabled={isPending}>
      {blocked ? t('blocks.unblock') : t('blocks.block')}
    </Button>
  )
}
