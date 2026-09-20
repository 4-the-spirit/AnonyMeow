import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { useUpdateFlair } from '../hooks'
import { FlairColorField } from './FlairColorField'
import type { FlairResponse } from '../types'

const HEX_COLOR_REGEX = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/

type FormValues = {
  name: string
  colorHex: string
}

export function EditFlairForm({
  communityName,
  flair,
  onDone,
}: {
  communityName: string
  flair: FlairResponse
  onDone: () => void
}) {
  const { t } = useTranslation('moderation')
  const { t: tCommon } = useTranslation('common')
  const updateFlair = useUpdateFlair(communityName)
  const schema = useMemo(
    () =>
      z.object({
        name: z.string().min(1, tCommon('validation.nameRequired')).max(30),
        colorHex: z.string().regex(HEX_COLOR_REGEX, tCommon('validation.hexColorFormat')),
      }),
    [tCommon]
  )
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: flair.name, colorHex: flair.colorHex },
  })

  async function onSubmit(values: FormValues) {
    try {
      await updateFlair.mutateAsync({ id: flair.id, body: values })
      toast.success(t('flairs.updateSuccessToast'))
      onDone()
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('flairs.updateErrorToast'))
      }
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-wrap items-end gap-3">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('flairs.nameLabel')}</FormLabel>
              <FormControl>
                <Input placeholder={t('flairs.namePlaceholder')} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="colorHex"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('flairs.colorLabel')}</FormLabel>
              <FormControl>
                <FlairColorField value={field.value} onChange={field.onChange} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit" size="sm" disabled={updateFlair.isPending}>
          {updateFlair.isPending ? t('flairs.saving') : t('flairs.save')}
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={onDone}>
          {t('flairs.cancel')}
        </Button>
      </form>
    </Form>
  )
}
