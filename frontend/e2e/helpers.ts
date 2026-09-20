import type { Page } from '@playwright/test'
import { expect } from '@playwright/test'

/**
 * Drives the real CompleteProfilePage UI (no API shortcuts) to sign a fresh browser
 * context in as a brand-new dev user with a chosen username. A fresh context has no
 * stored token, so AuthProvider auto-mints one for a random oid on first load and
 * RootGate forces this screen until a username is set.
 */
export async function completeProfile(page: Page, username: string) {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Welcome to AnonyMeow' })).toBeVisible()
  await page.getByLabel('Username').fill(username)
  await page.getByLabel('Display name').fill(username)
  await page.getByRole('button', { name: 'Continue' }).click()
  // Back in the main app shell once the profile-completion gate clears. The header logo link's
  // accessible name comes from AnimatedEmoji's alt text ("cat face"), not the literal emoji.
  await expect(page.getByRole('link', { name: 'cat face AnonyMeow' })).toBeVisible()
}

export async function createCommunity(page: Page, name: string) {
  await page.goto('/communities/new')
  await page.getByLabel('Name').fill(name)
  await page.getByRole('button', { name: 'Create community' }).click()
  await expect(page).toHaveURL(`/c/${name}`)
}

export async function createTextPost(page: Page, communityName: string, title: string, body: string) {
  await page.goto(`/c/${communityName}/submit`)
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Body').fill(body)
  await page.getByRole('button', { name: 'Post' }).click()
  await page.waitForURL(/\/posts\/.+/)
  const url = page.url()
  const id = url.split('/posts/')[1]?.split(/[?#]/)[0]
  if (!id) throw new Error(`Could not extract post id from URL: ${url}`)
  return id
}
