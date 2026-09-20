import { useEffect } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { ImageUploadField } from '@/components/ImageUploadField/ImageUploadField'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { useCommunity, useIsModerator, useUpdateCommunity } from '../hooks'
import { CommunityRulesField } from '../components/CommunityRulesField'

const schema = z.object({
  description: z.string().max(500).optional(),
  rules: z
    .array(
      z.object({
        title: z.string().min(1).max(100),
        description: z.string().min(1).max(500),
      })
    )
    .optional(),
  iconImageUrl: z.string().optional(),
  bannerImageUrl: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

export function CommunitySettingsPage() {
  const { t } = useTranslation('communities')
  const { communityName = '' } = useParams()
  const community = useCommunity(communityName)
  const { isModerator, isLoading: isModeratorLoading } = useIsModerator(communityName)
  const updateCommunity = useUpdateCommunity(communityName)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { description: '', rules: [], iconImageUrl: undefined, bannerImageUrl: undefined },
  })

  useEffect(() => {
    if (community.data) {
      form.reset({
        description: community.data.description ?? '',
        rules: community.data.rules,
        iconImageUrl: community.data.iconImageUrl,
        bannerImageUrl: community.data.bannerImageUrl,
      })
    }
  }, [community.data, form])

  async function onSubmit(values: FormValues) {
    try {
      await updateCommunity.mutateAsync(values)
      toast.success(t('settings.successToast'))
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('settings.errorToast'))
      }
    }
  }

  if (community.isPending || isModeratorLoading) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (community.isError) {
    return <ErrorState error={community.error} onRetry={() => community.refetch()} />
  }

  if (!isModerator) {
    return (
      <ErrorState
        title={t('settings.moderatorsOnlyTitle')}
        description={t('settings.moderatorsOnlyDescription', { name: communityName })}
      />
    )
  }

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-6 text-xl font-semibold">{t('settings.title', { name: communityName })}</h1>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <FormField
            control={form.control}
            name="description"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('settings.descriptionLabel')}</FormLabel>
                <FormControl>
                  <Textarea {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <CommunityRulesField control={form.control} name="rules" />
          <FormField
            control={form.control}
            name="iconImageUrl"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('settings.iconLabel')}</FormLabel>
                <FormControl>
                  <ImageUploadField value={field.value} onChange={field.onChange} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="bannerImageUrl"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('settings.bannerLabel')}</FormLabel>
                <FormControl>
                  <ImageUploadField value={field.value} onChange={field.onChange} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={updateCommunity.isPending}>
            {updateCommunity.isPending ? t('settings.saving') : t('settings.saveChanges')}
          </Button>
        </form>
      </Form>
    </div>
  )
}
