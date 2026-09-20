import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import type { ReportStatus } from '@/features/moderation/types'
import { useAllReports } from '../hooks'

const STATUS_TAB_VALUES: (ReportStatus | 'all')[] = [
  'Open',
  'Reviewed',
  'Dismissed',
  'ActionTaken',
  'all',
]

export function AdminReportsTab() {
  const { t, i18n } = useTranslation('admin')
  const { t: tModeration } = useTranslation('moderation')
  const [status, setStatus] = useState<ReportStatus | 'all'>('Open')
  const [page, setPage] = useState(1)
  const reports = useAllReports(status, page)

  return (
    <div className="flex flex-col gap-3 pt-3">
      <Tabs
        value={status}
        onValueChange={(v) => {
          setStatus(v as ReportStatus | 'all')
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

      {reports.isPending && (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      )}

      {reports.isError && <ErrorState error={reports.error} onRetry={() => reports.refetch()} />}

      {reports.data && reports.data.items.length === 0 && (
        <EmptyState title={t('reports.none')} />
      )}

      {reports.data && reports.data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          {reports.data.items.map((report) => (
            <div key={report.id} className="rounded-lg border p-3">
              <div className="flex items-center gap-2">
                <Badge variant="outline">{report.targetType}</Badge>
                <Badge variant="secondary">{report.status}</Badge>
                <Badge variant="secondary">{tModeration(`reportCategories.${report.category}`)}</Badge>
              </div>
              {report.details && <p className="mt-1 text-sm">{report.details}</p>}
              <p className="text-muted-foreground text-xs">
                {new Date(report.createdAtUtc).toLocaleString(i18n.language)}
              </p>
            </div>
          ))}
          <PaginationControl
            page={reports.data.page}
            pageSize={reports.data.pageSize}
            totalCount={reports.data.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}
    </div>
  )
}
