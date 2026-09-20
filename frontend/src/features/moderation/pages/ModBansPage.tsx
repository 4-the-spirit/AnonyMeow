import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { useIsModerator } from '@/features/communities/hooks'
import { useBans } from '../hooks'
import { BanTable } from '../components/BanTable'
import { CreateBanDialog } from '../components/CreateBanDialog'

export function ModBansPage() {
  const { t } = useTranslation('moderation')
  const { communityName = '' } = useParams()
  const { isModerator, isLoading: isModeratorLoading } = useIsModerator(communityName)
  const bans = useBans(communityName)

  if (isModeratorLoading) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (!isModerator) {
    return (
      <ErrorState
        title={t('moderatorsOnlyTitle')}
        description={t('moderatorsOnlyBansDescription', { name: communityName })}
      />
    )
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">{t('bans.pageTitle', { name: communityName })}</h1>
        <CreateBanDialog communityName={communityName} />
      </div>

      {bans.isPending && (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      )}
      {bans.isError && <ErrorState error={bans.error} onRetry={() => bans.refetch()} />}
      {bans.data && <BanTable communityName={communityName} bans={bans.data} />}
    </div>
  )
}
