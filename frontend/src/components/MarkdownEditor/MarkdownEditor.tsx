import * as React from 'react'
import { Bold, Code, Italic, Link as LinkIcon, List, ListOrdered, Quote } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { MarkdownBody } from '@/components/MarkdownBody/MarkdownBody'
import { EmojiPickerButton } from './EmojiPickerButton'
import {
  applyLinkWithUrl,
  applyMarkdownAction,
  insertText,
  type MarkdownAction,
  type MarkdownInsertionResult,
} from './markdownInsertions'

interface MarkdownEditorProps {
  value?: string
  onChange?: React.ChangeEventHandler<HTMLTextAreaElement>
  onBlur?: React.FocusEventHandler<HTMLTextAreaElement>
  name?: string
  placeholder?: string
  rows?: number
  disabled?: boolean
  autoFocus?: boolean
  /** Forwarded to the Preview tab's MarkdownBody — off for comments, which have no image feature. */
  allowImages?: boolean
  /** Forwarded to the inner `<textarea>`, not the outer wrapper — shadcn's `FormControl`
   * (a Radix `Slot`) merges these onto this component's root element, but the root here is
   * the Tabs wrapper, not the textarea. Without this, `<FormLabel htmlFor>` and
   * aria-describedby/aria-invalid from `FormMessage`/`FormDescription` would attach to the
   * wrong element. */
  id?: string
  'aria-describedby'?: string
  'aria-invalid'?: boolean | 'true' | 'false'
}

// 'link' is handled separately below via LinkInsertButton (a popover with URL/text inputs)
// instead of the raw-markdown toggle every other action here uses.
const TOOLBAR_ACTIONS_BEFORE_LINK: { action: MarkdownAction; labelKey: string; Icon: typeof Bold }[] = [
  { action: 'bold', labelKey: 'markdownEditor.bold', Icon: Bold },
  { action: 'italic', labelKey: 'markdownEditor.italic', Icon: Italic },
]
const TOOLBAR_ACTIONS_AFTER_LINK: { action: MarkdownAction; labelKey: string; Icon: typeof Bold }[] = [
  { action: 'bulletList', labelKey: 'markdownEditor.bulletList', Icon: List },
  { action: 'numberedList', labelKey: 'markdownEditor.numberedList', Icon: ListOrdered },
  { action: 'blockquote', labelKey: 'markdownEditor.quote', Icon: Quote },
  { action: 'code', labelKey: 'markdownEditor.code', Icon: Code },
]

/**
 * Drop-in replacement for `<Textarea>` in markdown body fields — behaves like a native
 * textarea with respect to controlled/uncontrolled usage, since the app uses both patterns:
 * react-hook-form's `Controller`/`FormField` passes a `value` prop (controlled), while bare
 * `{...form.register(...)}` does not — it sets the DOM node's initial value imperatively via
 * `ref` and expects the element to otherwise manage its own value (uncontrolled). When `value`
 * is omitted, this component mirrors that DOM-owned value into state so the toolbar and the
 * Preview tab can see it too.
 */
export const MarkdownEditor = React.forwardRef<HTMLTextAreaElement, MarkdownEditorProps>(
  (
    {
      value: controlledValue,
      onChange,
      onBlur,
      name,
      placeholder,
      rows = 6,
      disabled,
      autoFocus,
      allowImages = true,
      id,
      'aria-describedby': ariaDescribedBy,
      'aria-invalid': ariaInvalid,
    },
    forwardedRef
  ) => {
    const { t } = useTranslation('common')
    const isControlled = controlledValue !== undefined
    const [uncontrolledValue, setUncontrolledValue] = React.useState('')
    const value = isControlled ? controlledValue : uncontrolledValue

    const textareaRef = React.useRef<HTMLTextAreaElement>(null)
    React.useImperativeHandle(forwardedRef, () => textareaRef.current as HTMLTextAreaElement)

    const [linkPopoverOpen, setLinkPopoverOpen] = React.useState(false)
    const [linkText, setLinkText] = React.useState('')
    const [linkUrl, setLinkUrl] = React.useState('')
    const linkSelectionRef = React.useRef({ start: 0, end: 0 })

    React.useLayoutEffect(() => {
      if (!isControlled && textareaRef.current) {
        setUncontrolledValue(textareaRef.current.value)
      }
      // Runs once on mount only, after register()'s ref callback has set the DOM node's
      // initial value — re-running on every render would stomp on the user's typing.
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    function handleChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
      if (!isControlled) setUncontrolledValue(event.target.value)
      onChange?.(event)
    }

    function applyResult(result: MarkdownInsertionResult) {
      const textarea = textareaRef.current
      if (!textarea) return
      if (!isControlled) {
        // Uncontrolled: nothing re-renders the DOM value for us, so write it directly.
        textarea.value = result.text
      }
      // Synthesized in the shape react-hook-form's register()/Controller onChange handlers
      // expect (`event.target.name`/`.value`) — there is no real DOM event for a toolbar click.
      handleChange({ target: { name, value: result.text } } as React.ChangeEvent<HTMLTextAreaElement>)
      requestAnimationFrame(() => {
        textarea.focus()
        textarea.setSelectionRange(result.selectionStart, result.selectionEnd)
      })
    }

    function runAction(action: MarkdownAction) {
      const textarea = textareaRef.current
      if (!textarea || disabled) return
      applyResult(applyMarkdownAction(value, textarea.selectionStart, textarea.selectionEnd, action))
    }

    function openLinkPopover() {
      const textarea = textareaRef.current
      if (!textarea || disabled) return
      const start = textarea.selectionStart
      const end = textarea.selectionEnd
      linkSelectionRef.current = { start, end }
      setLinkText(value.slice(start, end))
      setLinkUrl('')
      setLinkPopoverOpen(true)
    }

    function insertLink() {
      const textarea = textareaRef.current
      if (!textarea || !linkUrl.trim()) return
      const { start, end } = linkSelectionRef.current
      applyResult(applyLinkWithUrl(value, start, end, linkText, linkUrl.trim()))
      setLinkPopoverOpen(false)
    }

    function insertEmoji(emoji: string) {
      const textarea = textareaRef.current
      if (!textarea || disabled) return
      applyResult(insertText(value, textarea.selectionStart, textarea.selectionEnd, emoji))
    }

    return (
      <Tabs defaultValue="write" className="gap-1.5">
        <TabsList className="self-start">
          <TabsTrigger value="write">{t('markdownEditor.write')}</TabsTrigger>
          <TabsTrigger value="preview">{t('markdownEditor.preview')}</TabsTrigger>
        </TabsList>
        <TabsContent value="write" className="flex flex-col gap-1.5">
          <div className="flex flex-wrap gap-0.5" role="toolbar" aria-label={t('markdownEditor.formatting')}>
            {TOOLBAR_ACTIONS_BEFORE_LINK.map(({ action, labelKey, Icon }) => (
              <Button
                key={action}
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={t(labelKey)}
                title={t(labelKey)}
                disabled={disabled}
                onClick={() => runAction(action)}
              >
                <Icon />
              </Button>
            ))}
            <Popover open={linkPopoverOpen} onOpenChange={setLinkPopoverOpen}>
              <PopoverTrigger asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  aria-label={t('markdownEditor.link')}
                  title={t('markdownEditor.link')}
                  disabled={disabled}
                  onClick={openLinkPopover}
                >
                  <LinkIcon />
                </Button>
              </PopoverTrigger>
              <PopoverContent className="flex flex-col gap-3">
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="markdown-link-text">{t('markdownEditor.linkTextLabel')}</Label>
                  <Input
                    id="markdown-link-text"
                    value={linkText}
                    onChange={(e) => setLinkText(e.target.value)}
                    placeholder={t('markdownEditor.linkTextPlaceholder')}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="markdown-link-url">{t('markdownEditor.linkUrlLabel')}</Label>
                  <Input
                    id="markdown-link-url"
                    value={linkUrl}
                    onChange={(e) => setLinkUrl(e.target.value)}
                    placeholder={t('markdownEditor.linkUrlPlaceholder')}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault()
                        insertLink()
                      }
                    }}
                  />
                </div>
                <Button type="button" size="sm" disabled={!linkUrl.trim()} onClick={insertLink}>
                  {t('markdownEditor.linkInsert')}
                </Button>
              </PopoverContent>
            </Popover>
            {TOOLBAR_ACTIONS_AFTER_LINK.map(({ action, labelKey, Icon }) => (
              <Button
                key={action}
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={t(labelKey)}
                title={t(labelKey)}
                disabled={disabled}
                onClick={() => runAction(action)}
              >
                <Icon />
              </Button>
            ))}
            <EmojiPickerButton onSelect={insertEmoji} disabled={disabled} />
          </div>
          <Textarea
            ref={textareaRef}
            id={id}
            name={name}
            aria-describedby={ariaDescribedBy}
            aria-invalid={ariaInvalid}
            {...(isControlled ? { value } : {})}
            onChange={handleChange}
            onBlur={onBlur}
            placeholder={placeholder}
            rows={rows}
            disabled={disabled}
            autoFocus={autoFocus}
          />
        </TabsContent>
        <TabsContent value="preview">
          <MarkdownBody
            className="min-h-16 rounded-lg border border-input px-2.5 py-2"
            allowImages={allowImages}
          >
            {value.trim() ? value : t('markdownEditor.nothingToPreview')}
          </MarkdownBody>
        </TabsContent>
      </Tabs>
    )
  }
)
MarkdownEditor.displayName = 'MarkdownEditor'
