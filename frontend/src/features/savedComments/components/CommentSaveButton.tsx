import { useState } from 'react'
import { Bookmark } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useSavedCommentsCache, useSaveComment, useUnsaveComment } from '../hooks'

export function CommentSaveButton({ commentId }: { commentId: string }) {
  const { t } = useTranslation('comments')
  const { isSaved } = useSavedCommentsCache()
  const [saved, setSaved] = useState(() => isSaved(commentId))
  const save = useSaveComment(commentId)
  const unsave = useUnsaveComment(commentId)

  async function handleClick() {
    try {
      if (saved) {
        await unsave.mutateAsync()
        setSaved(false)
        toast.success(t('item.unsaveSuccessToast'))
      } else {
        await save.mutateAsync()
        setSaved(true)
        toast.success(t('item.saveSuccessToast'))
      }
    } catch {
      toast.error(saved ? t('item.unsaveErrorToast') : t('item.saveErrorToast'))
    }
  }

  const isPending = save.isPending || unsave.isPending

  return (
    <Button
      type="button"
      variant="ghost"
      size="sm"
      aria-pressed={saved}
      disabled={isPending}
      onClick={handleClick}
    >
      <Bookmark className={cn('size-3.5', saved && 'fill-current')} />
      {saved ? t('item.unsave') : t('item.save')}
    </Button>
  )
}
