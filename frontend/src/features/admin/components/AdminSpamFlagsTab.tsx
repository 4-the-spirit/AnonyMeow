import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { useSpamFlags } from '../hooks'
import type { SpamFlagStatus } from '../types'

const STATUS_TAB_VALUES: (SpamFlagStatus | 'all')[] = [
  'Open',
  'Reviewed',
  'Dismissed',
  'ActionTaken',
  'all',
]

export function AdminSpamFlagsTab() {
  const { t, i18n } = useTranslation('admin')
  const { t: tModeration } = useTranslation('moderation')
  const [status, setStatus] = useState<SpamFlagStatus | 'all'>('Open')
  const [page, setPage] = useState(1)
  const spamFlags = useSpamFlags(status, page)

  return (
    <div className="flex flex-col gap-3 pt-3">
      <Tabs
        value={status}
        onValueChange={(v) => {
          setStatus(v as SpamFlagStatus | 'all')
          setPage(1)
        }}
      >
        <TabsList>
          {STATUS_TAB_VALUES.map((value) => (
            <TabsTrigger key={value} value={value}>
              {tModeration(`queue.status${value === 'all' ? 'All' : value}`)}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {spamFlags.isPending && (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      )}

      {spamFlags.isError && (
        <ErrorState error={spamFlags.error} onRetry={() => spamFlags.refetch()} />
      )}

      {spamFlags.data && spamFlags.data.items.length === 0 && (
        <EmptyState title={t('spamFlags.none')} />
      )}

      {spamFlags.data && spamFlags.data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          {spamFlags.data.items.map((flag) => (
            <div key={flag.id} className="rounded-lg border p-3">
              <div className="flex items-center gap-2">
                <Badge variant="outline">{flag.targetType}</Badge>
                <Badge variant="secondary">{t(`spamFlags.reason${flag.reason}`)}</Badge>
                <Badge variant="secondary">{flag.status}</Badge>
              </div>
              <p className="text-muted-foreground text-xs">
                {new Date(flag.createdAtUtc).toLocaleString(i18n.language)}
              </p>
            </div>
          ))}
          <PaginationControl
            page={spamFlags.data.page}
            pageSize={spamFlags.data.pageSize}
            totalCount={spamFlags.data.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}
    </div>
  )
}
