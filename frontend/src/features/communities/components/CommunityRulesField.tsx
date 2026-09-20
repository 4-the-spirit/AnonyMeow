import type { Control, FieldValues, Path, PathValue } from 'react-hook-form'
import { Plus, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import type { CommunityRule } from '../types'

/** Rules are optional — this bullet list can start and stay empty, unlike CommunityFlairsField's
 * sibling shape (which it otherwise mirrors: manual add/remove via field.onChange). */
export function CommunityRulesField<T extends FieldValues>({
  control,
  name,
}: {
  control: Control<T>
  name: Path<T>
}) {
  const { t } = useTranslation('communities')

  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => {
        const rules: CommunityRule[] = field.value ?? []

        function update(index: number, patch: Partial<CommunityRule>) {
          const next = rules.map((r, i) => (i === index ? { ...r, ...patch } : r))
          field.onChange(next as PathValue<T, Path<T>>)
        }

        function addRule() {
          field.onChange([...rules, { title: '', description: '' }] as PathValue<T, Path<T>>)
        }

        function removeRule(index: number) {
          field.onChange(rules.filter((_, i) => i !== index) as PathValue<T, Path<T>>)
        }

        return (
          <FormItem>
            <FormLabel>{t('create.rulesLabel')}</FormLabel>
            <ol className="flex flex-col gap-3 ps-0">
              {rules.map((rule, index) => (
                <li key={index} className="flex list-none flex-col gap-2 rounded-lg border p-3">
                  <div className="flex items-center gap-2">
                    <span className="text-muted-foreground text-sm font-medium">{index + 1}.</span>
                    <Input
                      value={rule.title}
                      placeholder={t('create.ruleTitlePlaceholder')}
                      onChange={(e) => update(index, { title: e.target.value })}
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => removeRule(index)}
                      aria-label={t('create.removeRule', { number: index + 1 })}
                    >
                      <X className="size-4" />
                    </Button>
                  </div>
                  <Textarea
                    value={rule.description}
                    placeholder={t('create.ruleDescriptionPlaceholder')}
                    onChange={(e) => update(index, { description: e.target.value })}
                  />
                </li>
              ))}
            </ol>
            <Button type="button" variant="outline" size="sm" className="w-fit" onClick={addRule}>
              <Plus className="size-4" /> {t('create.addRule')}
            </Button>
            <FormMessage />
          </FormItem>
        )
      }}
    />
  )
}
