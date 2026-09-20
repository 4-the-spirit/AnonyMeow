import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('a post containing an email is blocked with the detected category, a clean one succeeds', async ({
  page,
}) => {
  const username = `pwpiip${ts}`
  await completeProfile(page, username)
  const communityName = `pwpiic${ts}`
  await createCommunity(page, communityName)

  await page.goto(`/c/${communityName}/submit`)
  await page.getByLabel('Title').fill('contact info')
  await page.getByLabel('Body').fill('email me at someone@example.com')
  await page.getByRole('button', { name: 'Post' }).click()

  await expect(page.getByText(/personal information \(Email\)/)).toBeVisible()
  // Blocked — never navigated away from the submit form.
  await expect(page).toHaveURL(`/c/${communityName}/submit`)

  // A clean post with the same title succeeds.
  const postId = await createTextPost(page, communityName, 'contact info', 'nothing sensitive here')
  await expect(page).toHaveURL(`/posts/${postId}`)
})

test('a comment containing a phone number is blocked with the detected category', async ({
  page,
}) => {
  const username = `pwpiic2${ts}`
  await completeProfile(page, username)
  const communityName = `pwpiicc${ts}`
  await createCommunity(page, communityName)
  const postId = await createTextPost(page, communityName, 'pii comment test', 'body text')

  await page.goto(`/posts/${postId}`)
  await page.getByPlaceholder('Join the discussion…').fill('call me 555-123-4567')
  await page.getByRole('button', { name: 'Comment', exact: true }).click()

  await expect(page.getByText(/personal information \(Phone Number\)/)).toBeVisible()
  await expect(page.getByText('call me 555-123-4567')).not.toBeVisible()
})
