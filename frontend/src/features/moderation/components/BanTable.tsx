import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { useRevokeBan } from '../hooks'
import type { BanResponse } from '../types'

export function BanTable({ communityName, bans }: { communityName: string; bans: BanResponse[] }) {
  const { t, i18n } = useTranslation('moderation')
  const revokeBan = useRevokeBan(communityName)

  async function handleRevoke(username: string) {
    try {
      await revokeBan.mutateAsync(username)
      toast.success(t('bans.unbanSuccessToast', { username: `u/${username}` }))
    } catch {
      toast.error(t('bans.unbanErrorToast'))
    }
  }

  if (bans.length === 0) {
    return <EmptyState title={t('bans.noneActive')} />
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>{t('bans.columnUsername')}</TableHead>
          <TableHead>{t('bans.columnReason')}</TableHead>
          <TableHead>{t('bans.columnBannedOn')}</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {bans.map((ban) => (
          <TableRow key={ban.username}>
            <TableCell>u/{ban.username}</TableCell>
            <TableCell>{ban.reason}</TableCell>
            <TableCell>{new Date(ban.createdAtUtc).toLocaleDateString(i18n.language)}</TableCell>
            <TableCell>
              <Button
                size="sm"
                variant="outline"
                disabled={revokeBan.isPending}
                onClick={() => handleRevoke(ban.username)}
              >
                {t('bans.unban')}
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
