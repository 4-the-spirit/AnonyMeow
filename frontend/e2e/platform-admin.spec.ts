import { test, expect } from '@playwright/test'
import { completeProfile } from './helpers'

const ts = Date.now().toString().slice(-8)

test('non-admin sees the admins-only message on /admin', async ({ page }) => {
  await completeProfile(page, `pwnadm${ts}`)
  await page.goto('/admin')
  await expect(page.getByText('Platform admins only')).toBeVisible({ timeout: 10000 })
})

test('platform admin can restrict a user, see it enforced, then lift it', async ({ browser }) => {
  const adminCtx = await browser.newContext()
  const targetCtx = await browser.newContext()
  const admin = await adminCtx.newPage()
  const target = await targetCtx.newPage()

  const adminUsername = `pwadm${ts}`
  const targetUsername = `pwtgt${ts}`
  await completeProfile(admin, adminUsername)
  await completeProfile(target, targetUsername)

  // Flip IsPlatformAdmin for the admin user directly, same as the backend phase 9 log's own
  // manual verification pass — there's no API to set this flag, by design.
  const { execSync } = await import('node:child_process')
  execSync(
    `docker exec anonymeow-postgres psql -U postgres -d anonymeow -c "UPDATE \\"Users\\" SET \\"IsPlatformAdmin\\" = true WHERE \\"Username\\" = '${adminUsername}';"`
  )

  const communityName = `pwadmc${ts}`
  await target.goto('/communities/new')
  await target.getByLabel('Name').fill(communityName)
  await target.getByRole('button', { name: 'Create community' }).click()
  await expect(target).toHaveURL(`/c/${communityName}`)

  await admin.goto('/admin')
  await expect(admin.getByRole('heading', { name: 'Platform admin' })).toBeVisible()
  await admin.getByRole('tab', { name: 'Restrictions' }).click()
  await admin.getByLabel('Username').fill(targetUsername)
  await admin.getByLabel('Reason').fill('e2e verification restrict')
  await admin.getByRole('button', { name: 'Restrict user' }).click()
  await expect(admin.getByText(`${targetUsername} restricted`)).toBeVisible()

  // Enforcement: the restricted user can no longer post.
  await target.goto(`/c/${communityName}/submit`)
  await target.getByLabel('Title').fill('should be blocked')
  await target.getByLabel('Body').fill('should be blocked body')
  await target.getByRole('button', { name: 'Post' }).click()
  await expect(target.getByText('Could not create the post. Please try again.')).toBeVisible()

  // Audit log shows the restriction.
  await admin.getByRole('tab', { name: 'Audit log' }).click()
  await expect(admin.getByText('PlatformRestrict', { exact: true }).first()).toBeVisible({
    timeout: 10000,
  })

  // Lift it, then confirm posting works again. Assert on the DELETE response itself rather
  // than the toast text — sonner auto-dismisses it and the network call can be slow enough
  // (many prior e2e runs' worth of data in this dev DB) to lose the race against a fixed
  // toast-visibility window.
  await admin.getByRole('tab', { name: 'Restrictions' }).click()
  const liftSection = admin.getByText('Lift a restriction').locator('..')
  await liftSection.getByPlaceholder('username').fill(targetUsername)
  const [liftResponse] = await Promise.all([
    admin.waitForResponse(
      (res) => res.url().includes(`/api/admin/users/${targetUsername}/restrict`) && res.request().method() === 'DELETE'
    ),
    liftSection.getByRole('button', { name: 'Lift restriction' }).click(),
  ])
  expect(liftResponse.status()).toBe(204)

  await target.goto(`/c/${communityName}/submit`)
  await target.getByLabel('Title').fill('should work now')
  await target.getByLabel('Body').fill('should work now body')
  await target.getByRole('button', { name: 'Post' }).click()
  await target.waitForURL(/\/posts\/.+/)

  await adminCtx.close()
  await targetCtx.close()
})
