import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useUpdatePostFlair } from '@/features/posts/hooks'
import type { PostResponse } from '@/features/posts/types'
import { useFlairs } from '../hooks'

/** Author-only. Only renders once the community has at least one flair to offer. A post's tag
 * is mandatory, so this is a reassignment control, not a way to clear it. */
export function PostFlairControl({ post }: { post: PostResponse }) {
  const { t } = useTranslation('moderation')
  const flairs = useFlairs(post.communityName)
  const updateFlair = useUpdatePostFlair(post.id)
  const [isSaving, setIsSaving] = useState(false)

  if (!flairs.data || flairs.data.length === 0) {
    return null
  }

  async function handleChange(value: string) {
    setIsSaving(true)
    try {
      await updateFlair.mutateAsync(value)
    } catch {
      toast.error(t('flairs.updateErrorToast'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Select value={post.flair?.id} onValueChange={handleChange} disabled={isSaving}>
      <SelectTrigger size="sm" aria-label={t('flairs.postFlairLabel')}>
        <SelectValue placeholder={t('flairs.setFlair')} />
      </SelectTrigger>
      <SelectContent>
        {flairs.data.map((flair) => (
          <SelectItem key={flair.id} value={flair.id}>
            {flair.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
