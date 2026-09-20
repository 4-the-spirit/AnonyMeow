import { useMemo } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  FormDescription,
} from '@/components/ui/form'
import { ImageUploadField } from '@/components/ImageUploadField/ImageUploadField'
import { communityNameSchema } from '@/lib/validation'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { DEFAULT_FLAIR_NAMES } from '@/features/flairs/defaultFlairs'
import { useCreateCommunity } from '../hooks'
import { CommunityFlairsField } from '../components/CommunityFlairsField'
import { CommunityRulesField } from '../components/CommunityRulesField'
import type { CreateCommunityRequest } from '../types'

const HEX_COLOR_REGEX = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/

type FormValues = CreateCommunityRequest

export function CreateCommunityPage() {
  const { t } = useTranslation('communities')
  const { t: tCommon } = useTranslation('common')
  const navigate = useNavigate()
  const createCommunity = useCreateCommunity()

  const schema = useMemo(
    () =>
      z.object({
        name: communityNameSchema(tCommon),
        description: z.string().max(500).optional(),
        rules: z
          .array(
            z.object({
              title: z.string().min(1, tCommon('validation.nameRequired')).max(100),
              description: z.string().min(1, tCommon('validation.messageRequired')).max(500),
            })
          )
          .optional(),
        flairs: z
          .array(
            z.object({
              name: z
                .string()
                .min(1, tCommon('validation.nameRequired'))
                .max(30)
                .refine((name) => !DEFAULT_FLAIR_NAMES.includes(name.trim()), {
                  message: t('create.flairDuplicatesDefault'),
                }),
              colorHex: z.string().regex(HEX_COLOR_REGEX, tCommon('validation.hexColorFormat')),
            })
          )
          .optional(),
        // Blob storage isn't provisioned locally, so ImageUploadField can't produce a real URL
        // in dev (see its own comment) — the backend mirrors this relaxation in Development
        // (CommunityEndpoints.ValidateImages), so only enforce the min-length check outside dev.
        iconImageUrl: import.meta.env.DEV ? z.string() : z.string().min(1, tCommon('validation.imageRequired')),
        bannerImageUrl: import.meta.env.DEV ? z.string() : z.string().min(1, tCommon('validation.imageRequired')),
      }),
    [t, tCommon]
  )

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      description: '',
      rules: [],
      flairs: [],
      iconImageUrl: '',
      bannerImageUrl: '',
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      const community = await createCommunity.mutateAsync(values)
      toast.success(t('create.successToast', { name: community.name }))
      navigate(`/c/${community.name}`)
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('create.errorToast'))
      }
    }
  }

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-1 text-xl font-semibold">{t('create.title')}</h1>
      <p className="text-muted-foreground mb-6 text-sm">{t('create.intro')}</p>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('create.nameLabel')}</FormLabel>
                <FormControl>
                  <Input placeholder={t('create.namePlaceholder')} {...field} />
                </FormControl>
                <FormDescription>{t('create.nameHint')}</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="description"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('create.descriptionLabel')}</FormLabel>
                <FormControl>
                  <Textarea placeholder={t('create.descriptionPlaceholder')} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <CommunityRulesField control={form.control} name="rules" />

          <CommunityFlairsField form={form} />

          <FormField
            control={form.control}
            name="iconImageUrl"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('create.iconLabel')}</FormLabel>
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
                <FormLabel>{t('create.bannerLabel')}</FormLabel>
                <FormControl>
                  <ImageUploadField value={field.value} onChange={field.onChange} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <Button type="submit" disabled={createCommunity.isPending}>
            {createCommunity.isPending ? t('create.creating') : t('create.submit')}
          </Button>
        </form>
      </Form>
    </div>
  )
}
