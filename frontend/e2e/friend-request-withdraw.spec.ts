import { test, expect } from '@playwright/test'
import { completeProfile } from './helpers'

const ts = Date.now().toString().slice(-8)

test('withdrawing a sent friend request from the profile button', async ({ browser }) => {
  const aCtx = await browser.newContext()
  const bCtx = await browser.newContext()
  const a = await aCtx.newPage()
  const b = await bCtx.newPage()

  const usernameA = `pwfa${ts}`
  const usernameB = `pwfb${ts}`
  await completeProfile(a, usernameA)
  await completeProfile(b, usernameB)

  await a.goto(`/u/${usernameB}`)
  await a.getByRole('button', { name: 'Add friend' }).click()
  const cancelButton = a.getByRole('button', { name: 'Cancel request' })
  await expect(cancelButton).toBeVisible()
  await expect(cancelButton).toBeEnabled()

  await cancelButton.click()
  await expect(a.getByRole('button', { name: 'Add friend' })).toBeVisible()

  await aCtx.close()
  await bCtx.close()
})

test('withdrawing a sent friend request from the Friend Requests page outgoing list', async ({ browser }) => {
  const aCtx = await browser.newContext()
  const bCtx = await browser.newContext()
  const a = await aCtx.newPage()
  const b = await bCtx.newPage()

  const usernameA = `pwga${ts}`
  const usernameB = `pwgb${ts}`
  await completeProfile(a, usernameA)
  await completeProfile(b, usernameB)

  await a.goto(`/u/${usernameB}`)
  await a.getByRole('button', { name: 'Add friend' }).click()
  await expect(a.getByRole('button', { name: 'Cancel request' })).toBeVisible()

  await a.goto('/friends/requests')
  const outgoingRow = a.getByRole('listitem').filter({ hasText: `u/${usernameB}` })
  await expect(outgoingRow).toBeVisible()
  await outgoingRow.getByRole('button', { name: 'Cancel' }).click()
  await expect(outgoingRow).not.toBeVisible()

  await aCtx.close()
  await bCtx.close()
})
