import { useTranslation } from 'react-i18next'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'

interface ConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  onConfirm: () => void
}

// In-app replacement for window.confirm() for destructive actions, so the confirmation matches
// the rest of the app's UI instead of a native browser dialog (see LinkWarningDialog for the
// equivalent "leaving the site" confirmation used for outbound links).
export function ConfirmDialog({ open, onOpenChange, title, description, onConfirm }: ConfirmDialogProps) {
  const { t } = useTranslation('common')

  function handleConfirm() {
    onOpenChange(false)
    onConfirm()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="outline">{t('actions.cancel')}</Button>
          </DialogClose>
          <Button variant="destructive" onClick={handleConfirm}>
            {t('actions.confirm')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
