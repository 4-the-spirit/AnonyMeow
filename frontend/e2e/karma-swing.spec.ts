import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('swinging a vote takes two clicks and each moves the score by exactly 1', async ({ browser }) => {
  const authorCtx = await browser.newContext()
  const voterCtx = await browser.newContext()
  const author = await authorCtx.newPage()
  const voter = await voterCtx.newPage()

  await completeProfile(author, `pwka${ts}`)
  const communityName = `pwkc${ts}`
  await createCommunity(author, communityName)
  const postId = await createTextPost(author, communityName, 'Karma swing test', 'body text')

  await completeProfile(voter, `pwkv${ts}`)
  await voter.goto(`/posts/${postId}`)

  const score = voter.locator('span.tabular-nums').first()
  await expect(score).toHaveText('0')

  await voter.getByRole('button', { name: 'Upvote' }).click()
  await expect(score).toHaveText('1')

  // Swing click: un-votes only (net -1), does NOT jump straight to -1.
  await voter.getByRole('button', { name: 'Downvote' }).click()
  await expect(score).toHaveText('0')

  // Second click actually casts the downvote (another net -1).
  await voter.getByRole('button', { name: 'Downvote' }).click()
  await expect(score).toHaveText('-1')

  await authorCtx.close()
  await voterCtx.close()
})
