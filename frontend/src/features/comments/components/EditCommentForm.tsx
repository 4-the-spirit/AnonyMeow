import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { MarkdownEditor } from '@/components/MarkdownEditor/MarkdownEditor'
import { useUpdateComment } from '../hooks'
import type { CommentResponse } from '../types'

export function EditCommentForm({
  comment,
  onDone,
}: {
  comment: CommentResponse
  onDone: () => void
}) {
  const { t } = useTranslation('comments')
  const updateComment = useUpdateComment(comment.id)
  const form = useForm({ defaultValues: { bodyMarkdown: comment.bodyMarkdown } })

  async function onSubmit(values: { bodyMarkdown: string }) {
    try {
      await updateComment.mutateAsync(values)
      toast.success(t('editForm.successToast'))
      onDone()
    } catch {
      toast.error(t('editForm.errorToast'))
    }
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-2">
      <MarkdownEditor rows={3} allowImages={false} {...form.register('bodyMarkdown')} />
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={updateComment.isPending}>
          {t('editForm.save')}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={onDone}>
          {t('editForm.cancel')}
        </Button>
      </div>
    </form>
  )
}
