import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('clicking an author name on a post navigates to their profile', async ({ page }) => {
  const username = `pwpl${ts}`
  await completeProfile(page, username)
  const communityName = `pwplc${ts}`
  await createCommunity(page, communityName)
  await createTextPost(page, communityName, 'Profile link test', 'body text')

  await page.goto(`/c/${communityName}`)
  await page.getByRole('link', { name: `u/${username}` }).first().click()

  await expect(page).toHaveURL(`/u/${username}`)
  await expect(page.getByRole('heading', { name: username })).toBeVisible()
})

test('clicking an author name on a comment navigates to their profile', async ({ page }) => {
  const username = `pwcl${ts}`
  await completeProfile(page, username)
  const communityName = `pwclc${ts}`
  await createCommunity(page, communityName)
  const postId = await createTextPost(page, communityName, 'Comment link test', 'body text')

  await page.goto(`/posts/${postId}`)
  await page.getByPlaceholder('Join the discussion…').fill('Hello world')
  await page.getByRole('button', { name: 'Comment', exact: true }).click()
  await expect(page.getByText('Hello world')).toBeVisible()

  await page.getByRole('link', { name: `u/${username}` }).last().click()

  await expect(page).toHaveURL(`/u/${username}`)
})
