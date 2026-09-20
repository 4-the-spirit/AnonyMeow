import { defineConfig, devices } from '@playwright/test'

/**
 * One-off E2E verification suite for the karma-swing / profile-link / comment-deep-link /
 * friend-request-withdraw change. Points at an already-running dev frontend (5173) and
 * backend (7225, self-signed dev cert) rather than spawning its own webServer, since both
 * are started manually against the local Postgres dev database for this verification pass.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5173',
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
