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
import { useCreateFlair } from '../hooks'
import { FlairColorField } from './FlairColorField'

const HEX_COLOR_REGEX = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/
const DEFAULT_COLOR = '#22C55E'

type FormValues = {
  name: string
  colorHex: string
}

export function CreateFlairForm({ communityName }: { communityName: string }) {
  const { t } = useTranslation('moderation')
  const { t: tCommon } = useTranslation('common')
  const createFlair = useCreateFlair(communityName)
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
    defaultValues: { name: '', colorHex: DEFAULT_COLOR },
  })

  async function onSubmit(values: FormValues) {
    try {
      await createFlair.mutateAsync(values)
      toast.success(t('flairs.createSuccessToast'))
      form.reset({ name: '', colorHex: DEFAULT_COLOR })
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('flairs.createErrorToast'))
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
                <FlairColorField value={field.value} onChange={field.onChange} placeholder={DEFAULT_COLOR} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit" disabled={createFlair.isPending}>
          {createFlair.isPending ? t('flairs.adding') : t('flairs.addFlair')}
        </Button>
      </form>
    </Form>
  )
}
