import { AlertTriangle } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api/problemDetails'

interface ErrorStateProps {
  title?: string
  description?: string
  error?: unknown
  onRetry?: () => void
}

export function ErrorState({ title, description, error, onRetry }: ErrorStateProps) {
  const { t } = useTranslation('common')
  const resolvedDescription =
    description ?? (error instanceof ApiError ? error.detail ?? error.message : undefined)

  return (
    <div className="flex flex-col items-center gap-2 py-8 text-center">
      <AlertTriangle className="text-destructive size-8" aria-hidden />
      <p className="font-medium">{title ?? t('errors.generic')}</p>
      {resolvedDescription && (
        <p className="text-muted-foreground max-w-sm text-sm">{resolvedDescription}</p>
      )}
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry} className="mt-2">
          {t('actions.retry')}
        </Button>
      )}
    </div>
  )
}
