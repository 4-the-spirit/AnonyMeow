import type { UseFormReturn } from 'react-hook-form'
import { Plus, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import type { PostFormValues } from './postFormSchema'

export function PollPostFields({ form }: { form: UseFormReturn<PostFormValues> }) {
  const { t } = useTranslation('posts')
  return (
    <FormField
      control={form.control}
      name="pollOptions"
      render={({ field }) => {
        const options = field.value && field.value.length > 0 ? field.value : ['', '']

        function updateOption(index: number, value: string) {
          const next = [...options]
          next[index] = value
          field.onChange(next)
        }

        function addOption() {
          field.onChange([...options, ''])
        }

        function removeOption(index: number) {
          field.onChange(options.filter((_, i) => i !== index))
        }

        return (
          <FormItem>
            <FormLabel>{t('pollFields.optionsLabel')}</FormLabel>
            <div className="flex flex-col gap-2">
              {options.map((option, index) => (
                <div key={index} className="flex items-center gap-2">
                  <Input
                    value={option}
                    placeholder={t('pollFields.optionPlaceholder', { number: index + 1 })}
                    onChange={(e) => updateOption(index, e.target.value)}
                  />
                  {options.length > 2 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => removeOption(index)}
                      aria-label={t('pollFields.removeOption', { number: index + 1 })}
                    >
                      <X className="size-4" />
                    </Button>
                  )}
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="w-fit"
                onClick={addOption}
              >
                <Plus className="size-4" /> {t('pollFields.addOption')}
              </Button>
            </div>
            <FormMessage />
          </FormItem>
        )
      }}
    />
  )
}
