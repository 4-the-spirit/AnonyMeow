import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from '@/lib/i18n'
import { cn } from '@/lib/utils'

const LANGUAGE_LABELS: Record<SupportedLanguage, string> = {
  en: 'EN',
  he: 'עב',
}

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation('common')

  return (
    <div className="flex items-center gap-0.5" role="group" aria-label={t('language')}>
      {SUPPORTED_LANGUAGES.map((lng) => (
        <Button
          key={lng}
          type="button"
          variant="ghost"
          size="sm"
          aria-pressed={i18n.language === lng}
          className={cn(i18n.language === lng && 'bg-muted text-foreground')}
          onClick={() => i18n.changeLanguage(lng)}
        >
          {LANGUAGE_LABELS[lng]}
        </Button>
      ))}
    </div>
  )
}
