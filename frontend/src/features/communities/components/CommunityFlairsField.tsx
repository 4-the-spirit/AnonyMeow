import type { UseFormReturn } from 'react-hook-form'
import { Plus, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { FormField, FormItem, FormLabel, FormDescription, FormMessage } from '@/components/ui/form'
import { DEFAULT_FLAIR_NAMES } from '@/features/flairs/defaultFlairs'
import type { CreateFlairRequest } from '@/features/flairs/types'
import type { CreateCommunityRequest } from '../types'

export const DEFAULT_FLAIR_COLOR = '#22C55E'
const HEX_COLOR_REGEX = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/

/** Every community already launches with DEFAULT_FLAIR_NAMES (seeded server-side) — this field
 * only collects additional, optional custom tags, so it can start and stay empty. */
export function CommunityFlairsField({ form }: { form: UseFormReturn<CreateCommunityRequest> }) {
  const { t } = useTranslation('communities')

  return (
    <FormField
      control={form.control}
      name="flairs"
      render={({ field }) => {
        const flairs: CreateFlairRequest[] = field.value ?? []

        function update(index: number, patch: Partial<CreateFlairRequest>) {
          const next = flairs.map((f, i) => (i === index ? { ...f, ...patch } : f))
          field.onChange(next)
        }

        function addFlair() {
          field.onChange([...flairs, { name: '', colorHex: DEFAULT_FLAIR_COLOR }])
        }

        function removeFlair(index: number) {
          field.onChange(flairs.filter((_, i) => i !== index))
        }

        return (
          <FormItem>
            <FormLabel>{t('create.flairsLabel')}</FormLabel>
            <FormDescription>
              {t('create.defaultTagsHint', { names: DEFAULT_FLAIR_NAMES.join(', ') })}
            </FormDescription>
            <div className="flex flex-col gap-2">
              {flairs.map((flair, index) => {
                const isDuplicateOfDefault = DEFAULT_FLAIR_NAMES.includes(flair.name.trim())
                return (
                  <div key={index} className="flex flex-col gap-1">
                    <div className="flex items-center gap-2">
                      <Input
                        value={flair.name}
                        placeholder={t('create.flairNamePlaceholder', { number: index + 1 })}
                        onChange={(e) => update(index, { name: e.target.value })}
                      />
                      <Input
                        value={flair.colorHex}
                        placeholder={DEFAULT_FLAIR_COLOR}
                        className="w-28"
                        onChange={(e) => update(index, { colorHex: e.target.value })}
                      />
                      <span
                        aria-hidden
                        className="size-6 shrink-0 rounded-full border"
                        style={{
                          backgroundColor: HEX_COLOR_REGEX.test(flair.colorHex) ? flair.colorHex : undefined,
                        }}
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        onClick={() => removeFlair(index)}
                        aria-label={t('create.removeFlair', { number: index + 1 })}
                      >
                        <X className="size-4" />
                      </Button>
                    </div>
                    {isDuplicateOfDefault && (
                      <p className="text-destructive text-xs">{t('create.flairDuplicatesDefault')}</p>
                    )}
                  </div>
                )
              })}
              <Button type="button" variant="outline" size="sm" className="w-fit" onClick={addFlair}>
                <Plus className="size-4" /> {t('create.addFlair')}
              </Button>
            </div>
            <FormMessage />
          </FormItem>
        )
      }}
    />
  )
}
