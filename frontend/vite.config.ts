/// <reference types="vitest/config" />
import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    css: true,
    // e2e/ holds Playwright specs (also *.spec.ts) — a different test runner entirely.
    exclude: ['**/node_modules/**', 'e2e/**'],
    // Component tests assert on English strings — use the existing VITE_I18N_ENABLED
    // reversibility switch (see lib/i18n/index.ts) to keep them on the English bundle
    // even though the app now defaults to Hebrew.
    env: { VITE_I18N_ENABLED: 'false' },
  },
})
