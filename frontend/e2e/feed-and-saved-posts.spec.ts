import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('feed shows posts only from joined communities', async ({ page }) => {
  const username = `pwfeed${ts}`
  const communityName = `pwfeedc${ts}`
  await completeProfile(page, username)
  await createCommunity(page, communityName)
  const title = `Feed test post ${ts}`
  await createTextPost(page, communityName, title, 'body for feed test')

  await page.goto('/feed')
  await expect(page.getByRole('heading', { name: 'Your feed' })).toBeVisible()
  await expect(page.getByText(title)).toBeVisible()
})

test('saving and unsaving a post from the post card', async ({ page }) => {
  const username = `pwsave${ts}`
  const communityName = `pwsavec${ts}`
  await completeProfile(page, username)
  await createCommunity(page, communityName)
  const title = `Save test post ${ts}`
  await createTextPost(page, communityName, title, 'body for save test')

  await page.goto(`/c/${communityName}`)
  await page.getByRole('button', { name: 'Save post' }).click()
  await expect(page.getByRole('button', { name: 'Unsave post' })).toBeVisible()

  await page.goto('/saved')
  await expect(page.getByRole('heading', { name: 'Saved', exact: true })).toBeVisible()
  await expect(page.getByText(title)).toBeVisible()

  await page.getByRole('button', { name: 'Unsave post' }).click()
  await expect(page.getByText(title)).not.toBeVisible()
})
