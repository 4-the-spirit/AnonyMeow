import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { MarkdownEditor } from '@/components/MarkdownEditor/MarkdownEditor'
import { describePiiBlock, getPiiCategories } from '@/lib/api/problemDetails'
import { useRequireAuth } from '@/features/auth/useRequireAuth'
import { useCreateComment } from '../hooks'

interface CommentComposerProps {
  postId: string
  parentCommentId?: string
  onDone?: () => void
  autoFocus?: boolean
}

export function CommentComposer({
  postId,
  parentCommentId,
  onDone,
  autoFocus,
}: CommentComposerProps) {
  const { t } = useTranslation('comments')
  const { isAnonymous } = useRequireAuth()
  const createComment = useCreateComment(postId)
  const form = useForm({ defaultValues: { bodyMarkdown: '' } })

  if (isAnonymous) {
    return (
      <p className="text-muted-foreground border-input rounded-lg border border-dashed px-3 py-2 text-sm">
        <Link to="/signin" className="text-foreground underline">
          {t('composer.signInPrompt')}
        </Link>
      </p>
    )
  }

  async function onSubmit(values: { bodyMarkdown: string }) {
    if (!values.bodyMarkdown.trim()) return
    try {
      await createComment.mutateAsync({ ...values, parentCommentId })
      form.reset()
      onDone?.()
    } catch (error) {
      const piiCategories = getPiiCategories(error)
      toast.error(piiCategories ? describePiiBlock(piiCategories) : t('composer.errorToast'))
    }
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-2">
      <MarkdownEditor
        placeholder={parentCommentId ? t('composer.replyPlaceholder') : t('composer.newPlaceholder')}
        rows={parentCommentId ? 2 : 3}
        autoFocus={autoFocus}
        allowImages={false}
        {...form.register('bodyMarkdown')}
      />
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={createComment.isPending}>
          {parentCommentId ? t('composer.reply') : t('composer.comment')}
        </Button>
        {onDone && (
          <Button type="button" size="sm" variant="ghost" onClick={onDone}>
            {t('composer.cancel')}
          </Button>
        )}
      </div>
    </form>
  )
}
