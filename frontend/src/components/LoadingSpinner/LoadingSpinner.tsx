import { Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

export function LoadingSpinner({ className }: { className?: string }) {
  const { t } = useTranslation('common')
  return (
    <Loader2
      role="status"
      aria-label={t('loading')}
      className={cn('text-muted-foreground size-6 animate-spin', className)}
    />
  )
}
