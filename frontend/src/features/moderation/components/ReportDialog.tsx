import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Flag } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { useReportComment, useReportMessage, useReportPost } from '../hooks'
import type { ReportReasonCategory, ReportTargetType } from '../types'

const TARGET_KEY: Record<ReportTargetType, string> = {
  Post: 'targetPost',
  Comment: 'targetComment',
  DirectMessage: 'targetMessage',
}

const CATEGORIES: ReportReasonCategory[] = [
  'Spam',
  'Harassment',
  'HateSpeech',
  'Violence',
  'Misinformation',
  'Nsfw',
  'Other',
]

export function ReportDialog({
  targetType,
  targetId,
}: {
  targetType: ReportTargetType
  targetId: string
}) {
  const { t } = useTranslation('moderation')
  const { t: tCommon } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const reportPost = useReportPost(targetId)
  const reportComment = useReportComment(targetId)
  const reportMessage = useReportMessage(targetId)
  const mutation =
    targetType === 'Post' ? reportPost : targetType === 'Comment' ? reportComment : reportMessage

  const schema = useMemo(
    () =>
      z.object({
        category: z.enum(CATEGORIES as [ReportReasonCategory, ...ReportReasonCategory[]], {
          message: tCommon('validation.reasonRequired'),
        }),
        details: z.string().max(500).optional(),
      }),
    [tCommon]
  )
  const form = useForm({
    resolver: zodResolver(schema),
    defaultValues: { category: undefined as unknown as ReportReasonCategory, details: '' },
  })

  async function onSubmit(values: { category: ReportReasonCategory; details?: string }) {
    try {
      await mutation.mutateAsync({
        category: values.category,
        details: values.details?.trim() ? values.details.trim() : undefined,
      })
      toast.success(t('report.successToast'))
      setOpen(false)
      form.reset()
    } catch {
      toast.error(t('report.errorToast'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="ghost" size="sm">
          <Flag className="size-3.5" />
          {t('report.reportButton')}
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('report.dialogTitle', { target: t(`report.${TARGET_KEY[targetType]}`) })}</DialogTitle>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <FormField
              control={form.control}
              name="category"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('report.categoryLabel')}</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder={t('report.categoryPlaceholder')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {CATEGORIES.map((category) => (
                        <SelectItem key={category} value={category}>
                          {t(`reportCategories.${category}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="details"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('report.detailsLabel')}</FormLabel>
                  <FormControl>
                    <Textarea placeholder={t('report.detailsPlaceholder')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button type="submit" disabled={mutation.isPending}>
                {t('report.submit')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
