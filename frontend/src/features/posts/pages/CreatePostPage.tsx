import { useMemo } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { MultiImageUploadField } from '@/components/ImageUploadField/MultiImageUploadField'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { applyServerErrors, describePiiBlock, getPiiCategories } from '@/lib/api/problemDetails'
import { FlairSelectField } from '@/features/flairs/components/FlairSelectField'
import { useCreatePost } from '../hooks'
import { buildPostFormSchema, type PostFormValues } from '../components/forms/postFormSchema'
import { BodyField } from '../components/forms/BodyField'
import { PollPostFields } from '../components/forms/PollPostFields'
import { MAX_IMAGE_COUNT, MAX_IMAGE_SIZE_BYTES } from '@/components/ImageUploadField/imageLimits'
import type { CreatePostRequest } from '../types'

export function CreatePostPage() {
  const { t } = useTranslation('posts')
  const { t: tCommon } = useTranslation('common')
  const { communityName = '' } = useParams()
  const navigate = useNavigate()
  const createPost = useCreatePost(communityName)

  const postFormSchema = useMemo(() => buildPostFormSchema(tCommon), [tCommon])

  const form = useForm<PostFormValues>({
    resolver: zodResolver(postFormSchema),
    defaultValues: {
      title: '',
      bodyMarkdown: '',
      imageUrls: [],
      hasPoll: false,
      pollOptions: ['', ''],
      flairId: '',
    },
  })

  const hasPoll = form.watch('hasPoll')

  async function onSubmit(values: PostFormValues) {
    const body: CreatePostRequest = {
      title: values.title,
      flairId: values.flairId,
      ...(values.bodyMarkdown?.trim() ? { bodyMarkdown: values.bodyMarkdown } : {}),
      ...(values.imageUrls?.length ? { imageUrls: values.imageUrls } : {}),
      ...(values.hasPoll
        ? { pollOptions: (values.pollOptions ?? []).map((o) => o.trim()).filter(Boolean) }
        : {}),
    }

    try {
      const post = await createPost.mutateAsync(body)
      toast.success(t('create.successToast'))
      navigate(`/posts/${post.id}`)
    } catch (error) {
      const piiCategories = getPiiCategories(error)
      if (piiCategories) {
        toast.error(describePiiBlock(piiCategories))
      } else if (!applyServerErrors(form.setError, error)) {
        toast.error(t('create.errorToast'))
      }
    }
  }

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-6 text-xl font-semibold">{t('create.title', { name: communityName })}</h1>

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

          <FlairSelectField control={form.control} name="flairId" communityName={communityName} />

          <BodyField form={form} />

          <FormField
            control={form.control}
            name="imageUrls"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('imageFields.sectionLabel')}</FormLabel>
                <FormControl>
                  <MultiImageUploadField
                    value={field.value ?? []}
                    onChange={field.onChange}
                    maxCount={MAX_IMAGE_COUNT}
                    maxSizeBytes={MAX_IMAGE_SIZE_BYTES}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="hasPoll"
            render={({ field }) => (
              <FormItem>
                <div className="flex items-center gap-2">
                  <FormControl>
                    <input
                      type="checkbox"
                      className="size-4"
                      checked={field.value}
                      onChange={(e) => {
                        field.onChange(e.target.checked)
                        if (!e.target.checked) {
                          form.setValue('pollOptions', undefined)
                        }
                      }}
                    />
                  </FormControl>
                  <FormLabel className="mb-0">{t('create.addPollLabel')}</FormLabel>
                </div>
              </FormItem>
            )}
          />
          {hasPoll && <PollPostFields form={form} />}

          <Button type="submit" disabled={createPost.isPending}>
            {createPost.isPending ? t('create.posting') : t('create.submit')}
          </Button>
        </form>
      </Form>
    </div>
  )
}
