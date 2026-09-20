import type { Control, FieldValues, Path } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useFlairs } from '../hooks'

/** A required tag picker for post creation/editing — every post must carry one of the
 * community's tags (defaults + any custom ones a moderator added). */
export function FlairSelectField<T extends FieldValues>({
  control,
  name,
  communityName,
}: {
  control: Control<T>
  name: Path<T>
  communityName: string
}) {
  const { t } = useTranslation('posts')
  const flairs = useFlairs(communityName)

  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          <FormLabel>{t('create.flairLabel')}</FormLabel>
          <Select value={field.value} onValueChange={field.onChange} disabled={!flairs.data}>
            <FormControl>
              <SelectTrigger>
                <SelectValue placeholder={t('create.flairPlaceholder')} />
              </SelectTrigger>
            </FormControl>
            <SelectContent>
              {flairs.data?.map((flair) => (
                <SelectItem key={flair.id} value={flair.id}>
                  {flair.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <FormMessage />
        </FormItem>
      )}
    />
  )
}
