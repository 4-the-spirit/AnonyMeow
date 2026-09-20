import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { formatLastSeen, isOnline } from '../presence'

interface PresenceIndicatorProps {
  lastSeenAt: string | null
  showLabel?: boolean
  className?: string
}

export function PresenceIndicator({ lastSeenAt, showLabel, className }: PresenceIndicatorProps) {
  const { t, i18n } = useTranslation('conversations')
  const online = isOnline(lastSeenAt)

  return (
    <span className={cn('inline-flex items-center gap-1.5', className)}>
      <span
        className={cn('size-2.5 shrink-0 rounded-full', online ? 'bg-green-500' : 'bg-muted-foreground/40')}
        aria-hidden
      />
      {showLabel && (
        <span className="text-muted-foreground text-xs">
          {online ? t('presence.online') : formatLastSeen(lastSeenAt, i18n.language, t)}
        </span>
      )}
    </span>
  )
}
