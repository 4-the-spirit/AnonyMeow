import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { EmptyState } from '@/components/EmptyState/EmptyState'
import { PaginationControl } from '@/components/PaginationControl/PaginationControl'
import { useMarkAllNotificationsRead, useNotifications } from '../hooks'
import { NotificationItem } from '../components/NotificationItem'

export function NotificationsPage() {
  const { t } = useTranslation('notifications')
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [page, setPage] = useState(1)
  const notifications = useNotifications(unreadOnly, page)
  const markAllRead = useMarkAllNotificationsRead()

  async function handleMarkAllRead() {
    try {
      await markAllRead.mutateAsync()
      toast.success(t('page.markAllReadSuccessToast'))
    } catch {
      toast.error(t('page.markAllReadErrorToast'))
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">{t('page.title')}</h1>
        <Button
          variant="outline"
          size="sm"
          onClick={handleMarkAllRead}
          disabled={markAllRead.isPending}
        >
          {t('page.markAllRead')}
        </Button>
      </div>

      <Button
        variant={unreadOnly ? 'default' : 'outline'}
        size="sm"
        className="w-fit"
        onClick={() => {
          setUnreadOnly((value) => !value)
          setPage(1)
        }}
      >
        {unreadOnly ? t('page.showingUnreadOnly') : t('page.showUnreadOnly')}
      </Button>

      {notifications.isPending ? (
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      ) : notifications.isError ? (
        <ErrorState error={notifications.error} onRetry={() => notifications.refetch()} />
      ) : notifications.data.items.length === 0 ? (
        <EmptyState
          title={unreadOnly ? t('page.noUnread') : t('page.noneYet')}
        />
      ) : (
        <>
          <ul className="flex flex-col gap-2">
            {notifications.data.items.map((notification) => (
              <NotificationItem key={notification.id} notification={notification} />
            ))}
          </ul>
          <PaginationControl
            page={notifications.data.page}
            pageSize={notifications.data.pageSize}
            totalCount={notifications.data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  )
}
