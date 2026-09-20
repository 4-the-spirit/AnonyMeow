import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'

interface PaginationControlProps {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function PaginationControl({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: PaginationControlProps) {
  const { t } = useTranslation('common')
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  if (totalPages <= 1) {
    return null
  }

  return (
    <div className="flex items-center justify-center gap-3 py-4">
      <Button
        variant="outline"
        size="sm"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        {t('pagination.previous')}
      </Button>
      <span className="text-muted-foreground text-sm">
        {t('pagination.pageOfTotal', { page, totalPages })}
      </span>
      <Button
        variant="outline"
        size="sm"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        {t('pagination.next')}
      </Button>
    </div>
  )
}
