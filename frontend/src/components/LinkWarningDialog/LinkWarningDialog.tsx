import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'

interface LinkWarningDialogProps {
  href: string
  children: React.ReactNode
  className?: string
}

function hostnameOf(href: string): string {
  try {
    return new URL(href).hostname
  } catch {
    return href
  }
}

// Reddit-style "you're leaving this site" interstitial: clicking a post's link opens a
// confirmation dialog naming the destination host, rather than navigating straight out.
export function LinkWarningDialog({ href, children, className }: LinkWarningDialogProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <button type="button" className={cn('text-primary break-all text-start hover:underline', className)}>
          {children}
        </button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('linkWarning.title')}</DialogTitle>
          <DialogDescription>{t('linkWarning.description', { hostname: hostnameOf(href) })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="outline">{t('linkWarning.cancel')}</Button>
          </DialogClose>
          {/* A real <a> navigation rather than window.open() — window.open() from inside a
           * Radix dialog's click handler can lose the browser's "user activation" state (the
           * dialog's own state update/unmount happens in the same tick), which makes Chrome's
           * popup blocker silently swallow it into an "about:blank#blocked" tab. A genuine
           * anchor click is never subject to that. */}
          <Button asChild>
            <a href={href} target="_blank" rel="noopener noreferrer" onClick={() => setOpen(false)}>
              {t('linkWarning.continue')}
            </a>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
