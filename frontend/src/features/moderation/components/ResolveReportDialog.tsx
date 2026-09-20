import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
} from '@/components/ui/dialog'
import { useResolveReport } from '../hooks'
import type { ReportResponse } from '../types'

export function ResolveReportDialog({
  communityName,
  report,
}: {
  communityName: string
  report: ReportResponse
}) {
  const { t } = useTranslation('moderation')
  const [open, setOpen] = useState(false)
  const [actionReason, setActionReason] = useState('')
  const resolveReport = useResolveReport(communityName)

  async function resolve(outcome: 'Dismiss' | 'ActionTaken') {
    try {
      await resolveReport.mutateAsync({
        reportId: report.id,
        body: { outcome, actionReason: actionReason || undefined },
      })
      toast.success(t('resolveDialog.successToast'))
      setOpen(false)
    } catch {
      toast.error(t('resolveDialog.errorToast'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm" variant="outline" disabled={report.status !== 'Open'}>
          {t('resolveDialog.resolve')}
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {t('resolveDialog.dialogTitle', { targetType: report.targetType.toLowerCase() })}
          </DialogTitle>
        </DialogHeader>
        <p className="text-sm">
          <strong>{t('resolveDialog.categoryPrefix')}</strong> {t(`reportCategories.${report.category}`)}
          {report.details && <> — {report.details}</>}
        </p>
        <Textarea
          placeholder={t('resolveDialog.notePlaceholder')}
          value={actionReason}
          onChange={(e) => setActionReason(e.target.value)}
        />
        <DialogFooter className="gap-2">
          <Button
            variant="outline"
            disabled={resolveReport.isPending}
            onClick={() => resolve('Dismiss')}
          >
            {t('resolveDialog.dismiss')}
          </Button>
          <Button disabled={resolveReport.isPending} onClick={() => resolve('ActionTaken')}>
            {t('resolveDialog.actionTaken')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
