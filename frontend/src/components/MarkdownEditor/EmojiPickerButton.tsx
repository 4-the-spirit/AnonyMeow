import { Smile } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { EMOJI_CATEGORIES } from './emojiList'

interface EmojiPickerButtonProps {
  onSelect: (emoji: string) => void
  disabled?: boolean
}

export function EmojiPickerButton({ onSelect, disabled }: EmojiPickerButtonProps) {
  const { t } = useTranslation('common')

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={t('markdownEditor.emoji')}
          title={t('markdownEditor.emoji')}
          disabled={disabled}
        >
          <Smile />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="max-h-64 w-64 overflow-y-auto">
        {EMOJI_CATEGORIES.map((category) => (
          <div key={category.labelKey} className="mb-1">
            <p className="text-muted-foreground px-1.5 pt-1 text-xs font-medium">
              {t(category.labelKey)}
            </p>
            <div className="flex flex-wrap gap-0.5">
              {category.emojis.map((emoji, index) => (
                <DropdownMenuItem
                  key={`${category.labelKey}-${index}`}
                  className="justify-center px-1 py-1 text-base"
                  onClick={() => onSelect(emoji)}
                >
                  {emoji}
                </DropdownMenuItem>
              ))}
            </div>
          </div>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
