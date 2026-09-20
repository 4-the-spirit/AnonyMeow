/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  /** Reversibility kill-switch for the i18n layer — set to "false" to force English/LTR. */
  readonly VITE_I18N_ENABLED?: string
  /** Set to "true" once the backend has real native-auth config wired up (see
   * docs/azure-ciam-setup.md) — selects `nativeAuthAdapter` over the dev-only bypass. The
   * frontend needs no CIAM values itself; all of that lives server-side only. */
  readonly VITE_REAL_AUTH_ENABLED?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
