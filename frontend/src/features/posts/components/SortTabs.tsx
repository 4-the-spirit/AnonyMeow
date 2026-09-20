import type { LucideIcon } from 'lucide-react'
import { Flame, Clock, TrendingUp, Swords, Pin } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import type { SortOrder } from '../types'

// "Pinned" only makes sense for a single community's post list — comment sorting
// (CommentSection) and the cross-community home feed (FeedPage) pass SORT_VALUES_WITHOUT_PINNED
// instead.
export const POST_SORT_VALUES: SortOrder[] = ['hot', 'new', 'top', 'controversial', 'pinned']
export const SORT_VALUES_WITHOUT_PINNED: SortOrder[] = ['hot', 'new', 'top', 'controversial']

const SORT_ICONS: Record<SortOrder, LucideIcon> = {
  hot: Flame,
  new: Clock,
  top: TrendingUp,
  controversial: Swords,
  pinned: Pin,
}

export function SortTabs({
  value,
  onChange,
  sorts = POST_SORT_VALUES,
}: {
  value: SortOrder
  onChange: (sort: SortOrder) => void
  sorts?: SortOrder[]
}) {
  const { t } = useTranslation('posts')
  return (
    <Tabs value={value} onValueChange={(v) => onChange(v as SortOrder)}>
      <TabsList>
        {sorts.map((sort) => {
          const Icon = SORT_ICONS[sort]
          return (
            <TabsTrigger key={sort} value={sort}>
              <Icon className="size-4" />
              {t(`sorts.${sort}`)}
            </TabsTrigger>
          )
        })}
      </TabsList>
    </Tabs>
  )
}
