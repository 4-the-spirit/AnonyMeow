import { test, expect } from '@playwright/test'
import { completeProfile } from './helpers'

const ts = Date.now().toString().slice(-8)

test('starting a conversation, sending a message, and seeing it live on the other side', async ({
  browser,
}) => {
  const aCtx = await browser.newContext()
  const bCtx = await browser.newContext()
  const a = await aCtx.newPage()
  const b = await bCtx.newPage()

  const usernameA = `pwma${ts}`
  const usernameB = `pwmb${ts}`
  await completeProfile(a, usernameA)
  await completeProfile(b, usernameB)

  await a.goto(`/u/${usernameB}`)
  await a.getByRole('button', { name: 'Message' }).click()
  await expect(a).toHaveURL(/\/messages\/.+/)

  await a.getByPlaceholder('Write a message…').fill('hello from a')
  await a.getByRole('button', { name: 'Send' }).click()
  const sentBubble = a.getByText('hello from a')
  await expect(sentBubble).toBeVisible()
  // The sender's own message aligns to the end (items-end ancestor).
  await expect(sentBubble.locator('xpath=ancestor::div[contains(@class,"items-end")]')).toBeVisible()

  await b.goto('/messages')
  await b.getByRole('link', { name: `u/${usernameA}` }).click()
  const receivedBubble = b.getByText('hello from a')
  await expect(receivedBubble).toBeVisible({ timeout: 10000 })
  // The other participant's message aligns to the start, and offers a Report option.
  await expect(
    receivedBubble.locator('xpath=ancestor::div[contains(@class,"items-start")]')
  ).toBeVisible()

  await b.getByPlaceholder('Write a message…').fill('hello back from b')
  await b.getByRole('button', { name: 'Send' }).click()
  await expect(b.getByText('hello back from b')).toBeVisible()

  // Delivered live to a over the hub, without a's page needing a manual reload.
  await expect(a.getByText('hello back from b')).toBeVisible({ timeout: 10000 })

  await aCtx.close()
  await bCtx.close()
})

test('blocking a user prevents them from sending further messages', async ({ browser }) => {
  const aCtx = await browser.newContext()
  const bCtx = await browser.newContext()
  const a = await aCtx.newPage()
  const b = await bCtx.newPage()

  const usernameA = `pwmba${ts}`
  const usernameB = `pwmbb${ts}`
  await completeProfile(a, usernameA)
  await completeProfile(b, usernameB)

  await a.goto(`/u/${usernameB}`)
  await a.getByRole('button', { name: 'Message' }).click()
  await a.getByPlaceholder('Write a message…').fill('before the block')
  await a.getByRole('button', { name: 'Send' }).click()
  await expect(a.getByText('before the block')).toBeVisible()

  // b blocks a after the conversation already exists.
  await b.goto(`/u/${usernameA}`)
  await b.getByRole('button', { name: 'Block' }).click()
  await expect(b.getByRole('button', { name: 'Unblock' })).toBeVisible()

  await a.getByPlaceholder('Write a message…').fill('are you still there?')
  await a.getByRole('button', { name: 'Send' }).click()
  await expect(a.getByText('Could not send message.')).toBeVisible()
  // The blocked send never rendered as a chat bubble (the composer keeps the draft text in the
  // textarea itself so the user doesn't lose it, which is why this checks bubbles specifically).
  await expect(a.locator('.bg-primary', { hasText: 'are you still there?' })).toHaveCount(0)

  await aCtx.close()
  await bCtx.close()
})
