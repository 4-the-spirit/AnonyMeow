import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { MarkdownEditor } from '@/components/MarkdownEditor/MarkdownEditor'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { FlairSelectField } from '@/features/flairs/components/FlairSelectField'
import { useUpdatePost, useUpdatePostFlair } from '../hooks'
import { PostImageGallery } from './PostImageGallery'
import type { PostResponse } from '../types'

interface EditPostFormProps {
  post: PostResponse
  onDone: () => void
}

interface EditPostFormValues {
  title: string
  bodyMarkdown?: string
  flairId: string
}

export function EditPostForm({ post, onDone }: EditPostFormProps) {
  const { t } = useTranslation('posts')
  const { t: tCommon } = useTranslation('common')
  const updatePost = useUpdatePost(post.id)
  const updatePostFlair = useUpdatePostFlair(post.id)

  const schema = useMemo(
    () =>
      z.object({
        title: z.string().min(1, tCommon('validation.titleRequired')).max(300),
        bodyMarkdown: z.string().optional(),
        flairId: z.string().min(1, tCommon('validation.flairRequired')),
      }),
    [tCommon]
  )

  const form = useForm<EditPostFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: post.title,
      bodyMarkdown: post.bodyMarkdown ?? '',
      flairId: post.flair?.id ?? '',
    },
  })

  async function onSubmit(values: EditPostFormValues) {
    try {
      await updatePost.mutateAsync({
        title: values.title,
        bodyMarkdown: values.bodyMarkdown,
      })
      if (values.flairId !== post.flair?.id) {
        await updatePostFlair.mutateAsync(values.flairId)
      }
      toast.success(t('editForm.successToast'))
      onDone()
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('editForm.errorToast'))
      }
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <FormField
          control={form.control}
          name="title"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('create.titleLabel')}</FormLabel>
              <FormControl>
                <Input placeholder={t('create.titlePlaceholder')} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FlairSelectField control={form.control} name="flairId" communityName={post.communityName} />

        <FormField
          control={form.control}
          name="bodyMarkdown"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('create.bodyLabel')}</FormLabel>
              <FormControl>
                <MarkdownEditor placeholder={t('create.bodyPlaceholder')} rows={8} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {post.imageUrls.length > 0 && (
          <div className="flex flex-col gap-1.5">
            <p className="text-sm font-medium">{t('imageFields.sectionLabel')}</p>
            <p className="text-muted-foreground text-xs">{t('editForm.imagesLockedHint')}</p>
            <PostImageGallery imageUrls={post.imageUrls} alt={post.title} />
          </div>
        )}

        {post.pollOptions && post.pollOptions.length > 0 && (
          <p className="text-muted-foreground text-xs">{t('editForm.pollLockedHint')}</p>
        )}

        <div className="flex gap-2">
          <Button type="submit" size="sm" disabled={updatePost.isPending}>
            {t('editForm.save')}
          </Button>
          <Button type="button" size="sm" variant="outline" onClick={onDone}>
            {t('editForm.cancel')}
          </Button>
        </div>
      </form>
    </Form>
  )
}
