import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { useIsModerator } from '@/features/communities/hooks'
import { useReports } from '../hooks'
import { ResolveReportDialog } from '../components/ResolveReportDialog'
import type { ReportStatus } from '../types'

const STATUS_TAB_VALUES: (ReportStatus | 'all')[] = ['Open', 'Reviewed', 'Dismissed', 'ActionTaken', 'all']

export function ModQueuePage() {
  const { t, i18n } = useTranslation('moderation')
  const { communityName = '' } = useParams()
  const { isModerator, isLoading: isModeratorLoading } = useIsModerator(communityName)
  const [status, setStatus] = useState<ReportStatus | 'all'>('Open')
  const [page, setPage] = useState(1)

  const statusTabs = STATUS_TAB_VALUES.map((value) => ({
    value,
    label: t(`queue.status${value === 'all' ? 'All' : value}`),
  }))

  const reports = useReports(communityName, status, page)

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
        description={t('moderatorsOnlyQueueDescription', { name: communityName })}
      />
    )
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t('queue.pageTitle', { name: communityName })}</h1>

      <Tabs
        value={status}
        onValueChange={(v) => {
          setStatus(v as ReportStatus | 'all')
          setPage(1)
        }}
      >
        <TabsList>
          {statusTabs.map((tab) => (
            <TabsTrigger key={tab.value} value={tab.value}>
              {tab.label}
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
        <EmptyState title={t('queue.noneHere')} />
      )}

      {reports.data && reports.data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          {reports.data.items.map((report) => (
            <div
              key={report.id}
              className="flex items-center justify-between gap-3 rounded-lg border p-3"
            >
              <div>
                <div className="flex items-center gap-2">
                  <Badge variant="outline">{report.targetType}</Badge>
                  <Badge variant="secondary">{report.status}</Badge>
                  <Badge variant="secondary">{t(`reportCategories.${report.category}`)}</Badge>
                </div>
                {report.details && <p className="mt-1 text-sm">{report.details}</p>}
                <p className="text-muted-foreground text-xs">
                  {new Date(report.createdAtUtc).toLocaleString(i18n.language)}
                </p>
              </div>
              <ResolveReportDialog communityName={communityName} report={report} />
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
