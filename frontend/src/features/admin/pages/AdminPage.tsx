import { useTranslation } from 'react-i18next'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { useAuth } from '@/features/auth/useAuth'
import { AdminReportsTab } from '../components/AdminReportsTab'
import { AdminSpamFlagsTab } from '../components/AdminSpamFlagsTab'
import { AdminAuditLogTab } from '../components/AdminAuditLogTab'
import { RestrictUserPanel } from '../components/RestrictUserPanel'

export function AdminPage() {
  const { t } = useTranslation('admin')
  const { status, user } = useAuth()

  if (status === 'loading') {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (!user?.isPlatformAdmin) {
    return <ErrorState title={t('adminsOnlyTitle')} description={t('adminsOnlyDescription')} />
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t('pageTitle')}</h1>
      <Tabs defaultValue="reports">
        <TabsList>
          <TabsTrigger value="reports">{t('tabs.reports')}</TabsTrigger>
          <TabsTrigger value="spamFlags">{t('tabs.spamFlags')}</TabsTrigger>
          <TabsTrigger value="restrictions">{t('tabs.restrictions')}</TabsTrigger>
          <TabsTrigger value="auditLog">{t('tabs.auditLog')}</TabsTrigger>
        </TabsList>
        <TabsContent value="reports">
          <AdminReportsTab />
        </TabsContent>
        <TabsContent value="spamFlags">
          <AdminSpamFlagsTab />
        </TabsContent>
        <TabsContent value="restrictions">
          <RestrictUserPanel />
        </TabsContent>
        <TabsContent value="auditLog">
          <AdminAuditLogTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}
