import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { useAuditLog } from '../hooks'

export function AdminAuditLogTab() {
  const { t, i18n } = useTranslation('admin')
  const [page, setPage] = useState(1)
  const auditLog = useAuditLog(page)

  return (
    <div className="flex flex-col gap-3 pt-3">
      {auditLog.isPending && (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      )}

      {auditLog.isError && (
        <ErrorState error={auditLog.error} onRetry={() => auditLog.refetch()} />
      )}

      {auditLog.data && auditLog.data.items.length === 0 && (
        <EmptyState title={t('auditLog.none')} />
      )}

      {auditLog.data && auditLog.data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          {auditLog.data.items.map((entry) => (
            <div
              key={entry.id}
              className="flex items-center justify-between gap-3 rounded-lg border p-3"
            >
              <div>
                <Badge variant="outline">{entry.actionType}</Badge>
                {entry.reason && <p className="mt-1 text-sm">{entry.reason}</p>}
              </div>
              <p className="text-muted-foreground text-xs">
                {new Date(entry.createdAtUtc).toLocaleString(i18n.language)}
              </p>
            </div>
          ))}
          <PaginationControl
            page={auditLog.data.page}
            pageSize={auditLog.data.pageSize}
            totalCount={auditLog.data.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}
    </div>
  )
}
