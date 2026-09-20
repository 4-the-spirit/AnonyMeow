import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('profile Comments tab deep-links to the post and auto-expands a nested reply', async ({ page }) => {
  const username = `pwdl${ts}`
  await completeProfile(page, username)
  const communityName = `pwdlc${ts}`
  await createCommunity(page, communityName)
  const postId = await createTextPost(page, communityName, 'Deep link test post', 'body text')

  await page.goto(`/posts/${postId}`)
  await page.getByPlaceholder('Join the discussion…').fill('Root comment')
  await page.getByRole('button', { name: 'Comment', exact: true }).click()
  await expect(page.getByText('Root comment')).toBeVisible()

  await page.getByRole('button', { name: 'Reply', exact: true }).click()
  await page.getByPlaceholder('Write a reply…').fill('Nested reply comment')
  // Two "Reply" buttons exist once the composer is open: the toggle and the submit
  // (which comes later in DOM order).
  await page.getByRole('button', { name: 'Reply', exact: true }).last().click()
  // Replies are expanded by default, so the new reply is visible without an extra click.
  await expect(page.getByText('Nested reply comment')).toBeVisible()

  await page.goto(`/u/${username}`)
  await page.getByRole('tab', { name: 'Comments' }).click()
  await expect(page.getByText('Nested reply comment')).toBeVisible()

  const replyCard = page.locator('div.rounded-lg.border.p-3').filter({ hasText: 'Nested reply comment' })
  await replyCard.getByRole('link', { name: /View in post/ }).click()

  await expect(page).toHaveURL(new RegExp(`/posts/${postId}\\?highlightComment=.*ancestors=`))
  // Replies are expanded by default, so the reply is visible without any manual expand step.
  await expect(page.getByText('Nested reply comment')).toBeVisible()
  // Scrolled-to and highlighted shortly after mount.
  await expect(page.locator('.ring-primary')).toBeVisible({ timeout: 2000 })
})
