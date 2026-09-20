import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Direction } from 'radix-ui'
import { isRtl } from '@/lib/i18n'

/**
 * Corrects `<html lang>`/`dir` to match the active i18next language. `frontend/index.html`
 * keeps its static `lang="en"` as the pre-hydration default; this effect takes over immediately
 * on mount and again on every language change.
 *
 * Also wraps children in Radix's `Direction.Provider`. Every directionality-aware Radix
 * primitive (`Tabs`, `Select`, `DropdownMenu`, `RadioGroup`, `Accordion`, `ScrollArea`,
 * `Slider`, `ToggleGroup`, ...) accepts a `dir` prop and, critically, **defaults it to
 * `"ltr"`** — written as a literal `dir="ltr"` HTML attribute on its root — when no `dir` is
 * passed and no `Direction.Provider` ancestor exists. That silently overrides the inherited
 * `<html dir="rtl">` for that whole subtree, which is why, without this, a `Select` trigger or
 * anything wrapped in `Tabs` (e.g. `MarkdownEditor`'s Write/Preview tabs around its `Textarea`)
 * renders left-aligned even in Hebrew, while plain elements like `Input` — not backed by a
 * directional Radix primitive — render correctly. Providing `dir` here once means every current
 * and future component built on a Radix primitive picks up the right direction automatically;
 * no individual component needs to remember to pass `dir` itself.
 */
export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const { i18n } = useTranslation()

  useEffect(() => {
    function applyDocumentDirection(language: string) {
      document.documentElement.lang = language
      document.documentElement.dir = isRtl(language) ? 'rtl' : 'ltr'
    }

    applyDocumentDirection(i18n.language)
    i18n.on('languageChanged', applyDocumentDirection)
    return () => {
      i18n.off('languageChanged', applyDocumentDirection)
    }
  }, [i18n])

  return (
    <Direction.Provider dir={isRtl(i18n.language) ? 'rtl' : 'ltr'}>{children}</Direction.Provider>
  )
}
