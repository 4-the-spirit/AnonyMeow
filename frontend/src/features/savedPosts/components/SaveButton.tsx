import { useState } from 'react'
import { Bookmark } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useSavedPostsCache, useSavePost, useUnsavePost } from '../hooks'

export function SaveButton({ postId }: { postId: string }) {
  const { t } = useTranslation('savedPosts')
  const { isSaved } = useSavedPostsCache()
  const [saved, setSaved] = useState(() => isSaved(postId))
  const save = useSavePost(postId)
  const unsave = useUnsavePost(postId)

  async function handleClick() {
    try {
      if (saved) {
        await unsave.mutateAsync()
        setSaved(false)
        toast.success(t('unsaveSuccessToast'))
      } else {
        await save.mutateAsync()
        setSaved(true)
        toast.success(t('saveSuccessToast'))
      }
    } catch {
      toast.error(saved ? t('unsaveErrorToast') : t('saveErrorToast'))
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
      {saved ? t('unsave') : t('save')}
    </Button>
  )
}
