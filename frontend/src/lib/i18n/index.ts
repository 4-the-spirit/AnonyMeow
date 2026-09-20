import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import en from './locales/en'
import he from './locales/he'

export const SUPPORTED_LANGUAGES = ['en', 'he'] as const
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number]
export const RTL_LANGUAGES: readonly SupportedLanguage[] = ['he']

/**
 * Single reversibility switch for the whole Hebrew/i18n effort: set VITE_I18N_ENABLED=false to
 * force English/LTR everywhere without touching any component code — every `t()` call still
 * resolves via the English resource bundle (`fallbackLng`), so the app behaves exactly as it
 * did before this change.
 */
export const I18N_ENABLED = import.meta.env.VITE_I18N_ENABLED !== 'false'

/**
 * The app is Hebrew-only for now — the language switcher UI is hidden (see AppLayout) and `lng`
 * is pinned to 'he' so no stale localStorage choice or browser locale can override it. The `en`
 * resource bundle, detector wiring, and this fallback stay in place so a future language switch
 * only requires re-adding the switcher and relaxing `lng` back to `undefined`.
 */
void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: { en, he },
    fallbackLng: 'he',
    lng: I18N_ENABLED ? 'he' : 'en',
    ns: Object.keys(en),
    defaultNS: 'common',
    interpolation: { escapeValue: false },
    detection: {
      // Only honor an explicit prior choice (persisted by the language switcher) — never
      // auto-switch a user's language based on browser locale on first visit.
      order: ['localStorage'],
      caches: ['localStorage'],
      lookupLocalStorage: 'anonymeow.language',
    },
  })

export function isRtl(language: string): boolean {
  return (RTL_LANGUAGES as readonly string[]).includes(language)
}

export default i18n
