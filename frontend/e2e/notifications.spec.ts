import { test, expect } from '@playwright/test'
import { completeProfile, createCommunity, createTextPost } from './helpers'

const ts = Date.now().toString().slice(-8)

test('a reply + mention notification can be clicked through to the highlighted comment', async ({
  browser,
}) => {
  const authorCtx = await browser.newContext()
  const replierCtx = await browser.newContext()
  const author = await authorCtx.newPage()
  const replier = await replierCtx.newPage()

  const authorUsername = `pwna${ts}`
  await completeProfile(author, authorUsername)
  const communityName = `pwnc${ts}`
  await createCommunity(author, communityName)
  const postId = await createTextPost(author, communityName, 'Notification test post', 'body text')

  await author.goto(`/posts/${postId}`)
  await author.getByPlaceholder('Join the discussion…').fill('Root comment')
  await author.getByRole('button', { name: 'Comment', exact: true }).click()
  await expect(author.getByText('Root comment')).toBeVisible()

  const replierUsername = `pwnr${ts}`
  await completeProfile(replier, replierUsername)
  await replier.goto(`/posts/${postId}`)
  await replier.getByRole('button', { name: 'Reply', exact: true }).click()
  await replier
    .getByPlaceholder('Write a reply…')
    .fill(`nice one @${authorUsername}`)
  await replier.getByRole('button', { name: 'Reply', exact: true }).last().click()

  // Author sees the unread-notifications badge appear on the bell (either pushed live over the
  // hub, or on the next query refetch) without needing a manual reload.
  await author.goto('/')
  await expect(author.getByRole('link', { name: 'Notifications' }).locator('span')).toBeVisible({
    timeout: 10000,
  })

  await author.goto('/notifications')
  const replyItem = author.getByRole('button').filter({ hasText: 'Reply' }).first()
  await expect(replyItem).toBeVisible()
  await replyItem.click()

  await expect(author).toHaveURL(new RegExp(`/posts/${postId}\\?highlightComment=`))
  await expect(author.getByText(`nice one @${authorUsername}`)).toBeVisible()

  await authorCtx.close()
  await replierCtx.close()
})

test('a friend request notification links to the friend requests page', async ({ browser }) => {
  const aCtx = await browser.newContext()
  const bCtx = await browser.newContext()
  const a = await aCtx.newPage()
  const b = await bCtx.newPage()

  const usernameA = `pwnfa${ts}`
  const usernameB = `pwnfb${ts}`
  await completeProfile(a, usernameA)
  await completeProfile(b, usernameB)

  await b.goto(`/u/${usernameA}`)
  await b.getByRole('button', { name: 'Add friend' }).click()

  await a.goto('/notifications')
  const friendRequestItem = a.getByRole('button').filter({ hasText: 'Friend request' }).first()
  await expect(friendRequestItem).toBeVisible({ timeout: 10000 })
  await friendRequestItem.click()

  await expect(a).toHaveURL('/friends/requests')
  await expect(a.getByRole('button', { name: 'Accept' })).toBeVisible()

  await aCtx.close()
  await bCtx.close()
})
