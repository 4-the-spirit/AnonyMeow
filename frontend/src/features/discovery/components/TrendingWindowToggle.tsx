import { useTranslation } from 'react-i18next'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import type { TrendingWindow } from '../types'

const WINDOW_VALUES: TrendingWindow[] = ['day', 'week']

export function TrendingWindowToggle({
  value,
  onChange,
}: {
  value: TrendingWindow
  onChange: (window: TrendingWindow) => void
}) {
  const { t } = useTranslation('discovery')
  return (
    <Tabs value={value} onValueChange={(v) => onChange(v as TrendingWindow)}>
      <TabsList>
        {WINDOW_VALUES.map((window) => (
          <TabsTrigger key={window} value={window}>
            {t(`windows.${window}`)}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>
  )
}
