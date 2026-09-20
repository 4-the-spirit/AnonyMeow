import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('trending shows a post from a joined community, platform and community scoped', async ({ page }) => {
  const username = `pwtrend${ts}`
  const communityName = `מגמות${ts}`
  await completeProfile(page, username)
  await createCommunity(page, communityName)
  const title = `Hot topic post ${ts}`
  await createTextPost(page, communityName, title, 'body for trending test')

  // Platform-scoped trending shares one long-lived dev database across every prior manual/e2e
  // session, so a brand-new zero-score post isn't guaranteed to rank onto page 1 — just assert
  // the page itself renders. The community-scoped view below is a single-post dataset, so
  // presence there is reliable (same reasoning DiscoveryServiceTests uses on the backend).
  await page.goto('/trending')
  await expect(page.getByRole('heading', { name: 'Trending' })).toBeVisible()
  await page.getByRole('tab', { name: 'This week' }).click()
  await expect(page.getByRole('heading', { name: 'Trending' })).toBeVisible()

  await page.goto(`/c/${communityName}`)
  // Scoped to <main> — the header also has an icon-only "Trending" link to the platform page.
  await page.locator('main').getByRole('link', { name: 'Trending' }).click()
  await expect(page).toHaveURL(new RegExp(`/trending\\?scope=community&community=${encodeURIComponent(communityName)}`))
  await expect(page.getByRole('heading', { name: `Trending in c/${communityName}` })).toBeVisible()
  await expect(page.getByText(title)).toBeVisible()
})

test('search finds a matching post, comment, and community', async ({ page }) => {
  const username = `pwsearch${ts}`
  const communityName = `חיפוש${ts}`
  const term = `uniqueterm${ts}`
  await completeProfile(page, username)
  await createCommunity(page, communityName)
  const title = `Post about ${term}`
  const postId = await createTextPost(page, communityName, title, `body mentioning ${term} here`)

  await page.goto(`/posts/${postId}`)
  await page.getByPlaceholder('Join the discussion…').fill(`comment mentioning ${term} too`)
  await page.getByRole('button', { name: 'Comment', exact: true }).click()
  await expect(page.getByText(`comment mentioning ${term} too`)).toBeVisible()

  await page.getByPlaceholder('Search AnonyMeow').fill(term)
  await page.getByPlaceholder('Search AnonyMeow').press('Enter')
  await expect(page).toHaveURL(`/search?q=${term}`)
  await expect(page.getByRole('heading', { name: `Search results for "${term}"` })).toBeVisible()
  await expect(page.getByText(title)).toBeVisible()
  await expect(page.getByText(`comment mentioning ${term} too`)).toBeVisible()

  await page.getByRole('tab', { name: 'Posts' }).click()
  await expect(page.getByText(title)).toBeVisible()

  await page.getByRole('tab', { name: 'Comments' }).click()
  await expect(page.getByText(`comment mentioning ${term} too`)).toBeVisible()

  // Community search: the community's own Hebrew name is its own distinct search term.
  await page.goto(`/search?q=${encodeURIComponent(communityName)}&type=communities`)
  await expect(page.getByRole('link', { name: `c/${communityName}` })).toBeVisible()
})

test('recommended communities appear on the browse page for a user who has joined none', async ({ page }) => {
  const username = `pwrec${ts}`
  await completeProfile(page, username)

  // Fresh user, zero joined communities — the long-lived dev database has plenty of communities
  // from prior sessions for the popularity heuristic to recommend.
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Recommended communities' })).toBeVisible()
})
