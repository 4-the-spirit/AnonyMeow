import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ConfirmDialog/ConfirmDialog'
import { useAuth } from '@/features/auth/useAuth'
import {
  useAcceptFriendRequest,
  useCancelFriendRequest,
  useDeclineFriendRequest,
  useFriends,
  useMyFriendRequests,
  useRemoveFriend,
  useSendFriendRequest,
} from '../hooks'

/**
 * Rendered on another user's profile only (never the viewer's own — UserProfilePage already
 * gates that). Relationship state has no single "status between us" endpoint, so it's derived
 * from the viewer's own friend list (always visible to its owner, regardless of the other
 * user's FriendListVisibility) plus their own pending requests.
 */
export function FriendActionButton({ username }: { username: string }) {
  const { t } = useTranslation('friends')
  const { user: currentUser } = useAuth()
  const myFriends = useFriends(currentUser?.username ?? '')
  const myRequests = useMyFriendRequests()
  const [confirmRemoveOpen, setConfirmRemoveOpen] = useState(false)

  const sendRequest = useSendFriendRequest(username)
  const acceptRequest = useAcceptFriendRequest(username)
  const declineRequest = useDeclineFriendRequest(username)
  const removeFriend = useRemoveFriend(username)
  const cancelRequest = useCancelFriendRequest(username)

  if (!currentUser || myFriends.isPending || myRequests.isPending) {
    return null
  }

  const isFriend = myFriends.data?.some((f) => f.username === username) ?? false
  const hasOutgoingRequest =
    myRequests.data?.outgoing.some((r) => r.addresseeUsername === username) ?? false
  const hasIncomingRequest =
    myRequests.data?.incoming.some((r) => r.requesterUsername === username) ?? false

  async function handleRemove() {
    try {
      await removeFriend.mutateAsync()
      toast.success(t('action.removeSuccessToast'))
    } catch {
      toast.error(t('action.removeErrorToast'))
    }
  }

  async function handleSend() {
    try {
      await sendRequest.mutateAsync()
      toast.success(t('action.sendSuccessToast'))
    } catch {
      toast.error(t('action.sendErrorToast'))
    }
  }

  async function handleAccept() {
    try {
      await acceptRequest.mutateAsync()
      toast.success(t('action.acceptSuccessToast', { username: `u/${username}` }))
    } catch {
      toast.error(t('action.acceptErrorToast'))
    }
  }

  async function handleDecline() {
    try {
      await declineRequest.mutateAsync()
      toast.success(t('action.declineSuccessToast'))
    } catch {
      toast.error(t('action.declineErrorToast'))
    }
  }

  async function handleCancel() {
    try {
      await cancelRequest.mutateAsync()
      toast.success(t('action.cancelSuccessToast'))
    } catch {
      toast.error(t('action.cancelErrorToast'))
    }
  }

  if (isFriend) {
    return (
      <>
        <Button
          variant="outline"
          onClick={() => setConfirmRemoveOpen(true)}
          disabled={removeFriend.isPending}
        >
          {t('action.friends')}
        </Button>
        <ConfirmDialog
          open={confirmRemoveOpen}
          onOpenChange={setConfirmRemoveOpen}
          title={t('action.removeConfirm', { username: `u/${username}` })}
          onConfirm={handleRemove}
        />
      </>
    )
  }

  if (hasIncomingRequest) {
    return (
      <div className="flex gap-2">
        <Button onClick={handleAccept} disabled={acceptRequest.isPending}>
          {t('action.accept')}
        </Button>
        <Button variant="outline" onClick={handleDecline} disabled={declineRequest.isPending}>
          {t('action.decline')}
        </Button>
      </div>
    )
  }

  if (hasOutgoingRequest) {
    return (
      <Button variant="outline" onClick={handleCancel} disabled={cancelRequest.isPending}>
        {t('action.cancelRequest')}
      </Button>
    )
  }

  return (
    <Button onClick={handleSend} disabled={sendRequest.isPending}>
      {t('action.addFriend')}
    </Button>
  )
}
