import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ConfirmDialog/ConfirmDialog'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { useIsModerator } from '@/features/communities/hooks'
import { useDeleteFlair, useFlairs } from '../hooks'
import { CreateFlairForm } from '../components/CreateFlairForm'
import { EditFlairForm } from '../components/EditFlairForm'
import { FlairBadge } from '../components/FlairBadge'

export function CommunityFlairsPage() {
  const { t } = useTranslation('moderation')
  const { communityName = '' } = useParams()
  const { isModerator, isLoading: isModeratorLoading } = useIsModerator(communityName)
  const flairs = useFlairs(communityName)
  const deleteFlair = useDeleteFlair(communityName)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)

  async function handleDelete(id: string) {
    try {
      await deleteFlair.mutateAsync(id)
      toast.success(t('flairs.deleteSuccessToast'))
    } catch {
      toast.error(t('flairs.deleteErrorToast'))
    }
  }

  if (isModeratorLoading || flairs.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (flairs.isError) {
    return <ErrorState error={flairs.error} onRetry={() => flairs.refetch()} />
  }

  if (!isModerator) {
    return (
      <ErrorState
        title={t('moderatorsOnlyTitle')}
        description={t('moderatorsOnlyFlairsDescription', { name: communityName })}
      />
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold">{t('flairs.pageTitle', { name: communityName })}</h1>

      <CreateFlairForm communityName={communityName} />

      {flairs.data.length === 0 ? (
        <EmptyState
          title={t('flairs.noneYetTitle')}
          description={t('flairs.noneYetDescription')}
        />
      ) : (
        <ul className="flex flex-col gap-2">
          {flairs.data.map((flair) =>
            editingId === flair.id ? (
              <li key={flair.id} className="rounded-lg border p-2">
                <EditFlairForm communityName={communityName} flair={flair} onDone={() => setEditingId(null)} />
              </li>
            ) : (
              <li
                key={flair.id}
                className="flex items-center justify-between gap-3 rounded-lg border p-2"
              >
                <div className="flex items-center gap-2">
                  <FlairBadge flair={flair} />
                  {flair.isDefault && <Badge variant="secondary">{t('flairs.defaultBadge')}</Badge>}
                </div>
                {!flair.isDefault && (
                  <div className="flex gap-1">
                    <Button variant="ghost" size="sm" onClick={() => setEditingId(flair.id)}>
                      {t('flairs.edit')}
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => setPendingDeleteId(flair.id)}>
                      {t('flairs.delete')}
                    </Button>
                  </div>
                )}
              </li>
            )
          )}
        </ul>
      )}

      <ConfirmDialog
        open={pendingDeleteId !== null}
        onOpenChange={(open) => !open && setPendingDeleteId(null)}
        title={t('flairs.deleteConfirm')}
        onConfirm={() => pendingDeleteId && handleDelete(pendingDeleteId)}
      />
    </div>
  )
}
