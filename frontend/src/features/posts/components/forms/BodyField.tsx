import type { UseFormReturn } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { MarkdownEditor } from '@/components/MarkdownEditor/MarkdownEditor'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import type { PostFormValues } from './postFormSchema'

export function BodyField({ form }: { form: UseFormReturn<PostFormValues> }) {
  const { t } = useTranslation('posts')
  return (
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
  )
}
