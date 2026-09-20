import { toast } from 'sonner'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import {
  useAcceptFriendRequest,
  useCancelFriendRequest,
  useDeclineFriendRequest,
  useMyFriendRequests,
} from '../hooks'
import { getDisplayName } from '@/lib/userDisplay'
import type { FriendRequestResponse } from '../types'

function IncomingRequestRow({ request }: { request: FriendRequestResponse }) {
  const { t } = useTranslation('friends')
  const accept = useAcceptFriendRequest(request.requesterUsername)
  const decline = useDeclineFriendRequest(request.requesterUsername)
  const requesterDisplayName = getDisplayName({
    username: request.requesterUsername,
    displayName: request.requesterDisplayName,
  })

  async function handleAccept() {
    try {
      await accept.mutateAsync()
      toast.success(t('action.acceptSuccessToast', { username: requesterDisplayName }))
    } catch {
      toast.error(t('action.acceptErrorToast'))
    }
  }

  async function handleDecline() {
    try {
      await decline.mutateAsync()
      toast.success(t('action.declineSuccessToast'))
    } catch {
      toast.error(t('action.declineErrorToast'))
    }
  }

  return (
    <li className="flex items-center justify-between gap-3 rounded-lg border p-3">
      <Link to={`/u/${request.requesterUsername}`} className="font-medium hover:underline">
        {requesterDisplayName}
      </Link>
      <div className="flex gap-2">
        <Button size="sm" onClick={handleAccept} disabled={accept.isPending}>
          {t('action.accept')}
        </Button>
        <Button size="sm" variant="outline" onClick={handleDecline} disabled={decline.isPending}>
          {t('action.decline')}
        </Button>
      </div>
    </li>
  )
}

function OutgoingRequestRow({ request }: { request: FriendRequestResponse }) {
  const { t } = useTranslation('friends')
  const cancel = useCancelFriendRequest(request.addresseeUsername)
  const addresseeDisplayName = getDisplayName({
    username: request.addresseeUsername,
    displayName: request.addresseeDisplayName,
  })

  async function handleCancel() {
    try {
      await cancel.mutateAsync()
      toast.success(t('action.cancelSuccessToast'))
    } catch {
      toast.error(t('action.cancelErrorToast'))
    }
  }

  return (
    <li className="flex items-center justify-between gap-3 rounded-lg border p-3">
      <Link to={`/u/${request.addresseeUsername}`} className="font-medium hover:underline">
        {addresseeDisplayName}
      </Link>
      <Button size="sm" variant="outline" onClick={handleCancel} disabled={cancel.isPending}>
        {t('requestsPage.cancel')}
      </Button>
    </li>
  )
}

export function FriendRequestsPage() {
  const { t } = useTranslation('friends')
  const requests = useMyFriendRequests()

  if (requests.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (requests.isError) {
    return <ErrorState error={requests.error} onRetry={() => requests.refetch()} />
  }

  const { incoming, outgoing } = requests.data

  return (
    <div className="mx-auto flex max-w-lg flex-col gap-6">
      <h1 className="text-xl font-semibold">{t('requestsPage.title')}</h1>

      <section className="flex flex-col gap-3">
        <h2 className="text-muted-foreground text-sm font-medium">{t('requestsPage.incoming')}</h2>
        {incoming.length === 0 ? (
          <EmptyState title={t('requestsPage.noIncoming')} />
        ) : (
          <ul className="flex flex-col gap-2">
            {incoming.map((request) => (
              <IncomingRequestRow key={request.requesterUsername} request={request} />
            ))}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-muted-foreground text-sm font-medium">{t('requestsPage.outgoing')}</h2>
        {outgoing.length === 0 ? (
          <EmptyState title={t('requestsPage.noOutgoing')} />
        ) : (
          <ul className="flex flex-col gap-2">
            {outgoing.map((request) => (
              <OutgoingRequestRow key={request.addresseeUsername} request={request} />
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
