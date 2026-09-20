import { useParams, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ShieldCheck } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { useCommunityModActions, useIsModerator, useMembers } from '../hooks'

export function CommunityMembersPage() {
  const { t } = useTranslation('communities')
  const { communityName = '' } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('search') ?? ''
  const page = Number(searchParams.get('page') ?? '1')

  const members = useMembers(communityName, search, page)
  const { isModerator } = useIsModerator(communityName)
  const { promote, demote } = useCommunityModActions(communityName)

  async function handlePromote(username: string) {
    try {
      await promote.mutateAsync(username)
      toast.success(t('membersPage.promoteSuccessToast', { username: `u/${username}` }))
    } catch {
      toast.error(t('membersPage.promoteErrorToast'))
    }
  }

  async function handleDemote(username: string) {
    try {
      await demote.mutateAsync(username)
      toast.success(t('membersPage.demoteSuccessToast', { username: `u/${username}` }))
    } catch {
      toast.error(t('membersPage.demoteErrorToast'))
    }
  }

  function handleSearchChange(value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set('search', value)
    else next.delete('search')
    next.set('page', '1')
    setSearchParams(next)
  }

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-6 text-xl font-semibold">
        {t('membersPage.title', { name: communityName })}
      </h1>

      <Input
        placeholder={t('membersPage.searchPlaceholder')}
        value={search}
        onChange={(e) => handleSearchChange(e.target.value)}
        className="mb-4"
      />

      {members.isPending && (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      )}

      {members.isError && (
        <ErrorState error={members.error} onRetry={() => members.refetch()} />
      )}

      {members.data && members.data.items.length === 0 && (
        <EmptyState
          title={search ? t('membersPage.noneFoundTrySearch') : t('membersPage.noneYetTitle')}
        />
      )}

      {members.data && members.data.items.length > 0 && (
        <ul className="flex flex-col gap-3">
          {members.data.items.map((member) => (
            <li key={member.username} className="flex items-center gap-3">
              <UserAvatar seed={member.avatarSeed} username={member.username} />
              <div className="flex-1">
                <p className="text-sm font-medium">{member.displayName ?? member.username}</p>
                <p className="text-muted-foreground text-xs">u/{member.username}</p>
              </div>
              {member.role === 'Moderator' && (
                <Badge variant="secondary">
                  <ShieldCheck className="size-3" /> {t('membersPage.moderatorBadge')}
                </Badge>
              )}
              {isModerator && member.role === 'Moderator' && (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={demote.isPending}
                  onClick={() => handleDemote(member.username)}
                >
                  {t('membersPage.demote')}
                </Button>
              )}
              {isModerator && member.role !== 'Moderator' && (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={promote.isPending}
                  onClick={() => handlePromote(member.username)}
                >
                  {t('membersPage.promote')}
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {members.data && (
        <PaginationControl
          page={members.data.page}
          pageSize={members.data.pageSize}
          totalCount={members.data.totalCount}
          onPageChange={(nextPage) => {
            const next = new URLSearchParams(searchParams)
            next.set('page', String(nextPage))
            setSearchParams(next)
          }}
        />
      )}
    </div>
  )
}
