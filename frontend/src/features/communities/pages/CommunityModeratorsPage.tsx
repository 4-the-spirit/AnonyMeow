import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { useModerators } from '../hooks'

export function CommunityModeratorsPage() {
  const { t } = useTranslation('communities')
  const { communityName = '' } = useParams()
  const moderators = useModerators(communityName)

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-6 text-xl font-semibold">
        {t('moderatorsPage.title', { name: communityName })}
      </h1>

      {moderators.isPending && (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      )}

      {moderators.isError && (
        <ErrorState error={moderators.error} onRetry={() => moderators.refetch()} />
      )}

      {moderators.data && (
        <ul className="flex flex-col gap-3">
          {moderators.data.map((mod) => (
            <li key={mod.username} className="flex items-center gap-3">
              <UserAvatar seed={null} username={mod.username} />
              <div>
                <p className="text-sm font-medium">
                  {mod.displayName ?? mod.username}
                </p>
                <p className="text-muted-foreground text-xs">u/{mod.username}</p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
