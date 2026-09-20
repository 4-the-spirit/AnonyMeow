import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { useRequireAuth } from '@/features/auth/useRequireAuth'
import {
  useJoinCommunity,
  useJoinedCommunitiesCache,
  useLeaveCommunity,
} from '@/features/communities/hooks'

export function JoinLeaveButton({ communityName }: { communityName: string }) {
  const { t } = useTranslation('communities')
  const { requireAuth } = useRequireAuth()
  const { isJoined } = useJoinedCommunitiesCache()
  const [joined, setJoined] = useState(() => isJoined(communityName))
  const join = useJoinCommunity(communityName)
  const leave = useLeaveCommunity(communityName)

  async function handleClick() {
    try {
      if (joined) {
        await leave.mutateAsync()
        setJoined(false)
      } else {
        await join.mutateAsync()
        setJoined(true)
      }
    } catch {
      toast.error(joined ? t('join.errorLeave') : t('join.errorJoin'))
    }
  }

  const isPending = join.isPending || leave.isPending

  return (
    <Button
      variant={joined ? 'outline' : 'default'}
      onClick={() => requireAuth(handleClick)}
      disabled={isPending}
    >
      {joined ? t('join.leave') : t('join.join')}
    </Button>
  )
}
