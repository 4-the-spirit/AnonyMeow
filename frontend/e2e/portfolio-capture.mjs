// One-off script (not part of the CI/e2e spec suite) that drives the real app through a
// representative walkthrough and captures screenshots + a video for use on a portfolio site.
// Run manually: node e2e/portfolio-capture.mjs
// Requires the backend (https://localhost:7225, dev-only /api/dev/token auth bypass) and the
// frontend dev server to already be running (with VITE_I18N_ENABLED=false so the walkthrough
// uses the English/LTR UI rather than the Hebrew-pinned default).
import { chromium } from '@playwright/test'
import path from 'node:path'
import fs from 'node:fs'

const FRONTEND_URL = process.env.FRONTEND_URL || 'http://localhost:5175'
const OUT_DIR = path.resolve('..', '..', 'docs', 'portfolio-media')
const SHOTS_DIR = path.join(OUT_DIR, 'screenshots')
const VIDEO_DIR = path.join(OUT_DIR, 'video')

fs.mkdirSync(SHOTS_DIR, { recursive: true })
fs.mkdirSync(VIDEO_DIR, { recursive: true })

let shotIndex = 0
async function shot(page, name) {
  shotIndex += 1
  const fileName = `${String(shotIndex).padStart(2, '0')}-${name}.png`
  await page.screenshot({ path: path.join(SHOTS_DIR, fileName), fullPage: true })
  console.log(`  -> ${fileName}`)
}

async function main() {
  const browser = await chromium.launch()

  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    ignoreHTTPSErrors: true,
    recordVideo: { dir: VIDEO_DIR, size: { width: 1440, height: 900 } },
  })
  const page = await context.newPage()

  // Suffixed so reruns against the same dev database don't collide with permanent,
  // already-taken usernames/community names from a prior capture run.
  const runId = String(Date.now()).slice(-6)
  const primaryUsername = `purring_fox_${runId}`
  const secondUsername = `moderator_mia_${runId}`
  const communityName = `תמונות-חתולים-${runId}`

  console.log('1. Sign up + onboarding')
  await page.goto(FRONTEND_URL + '/signup')
  await page.getByRole('button', { name: 'Create account' }).click()
  await page.getByRole('heading', { name: 'Welcome to AnonyMeow' }).waitFor()
  await shot(page, 'welcome-onboarding')
  const shuffleBtn = page.getByRole('button', { name: 'Shuffle avatar' })
  if (await shuffleBtn.count()) {
    await shuffleBtn.click()
    await page.waitForTimeout(300)
    await shuffleBtn.click()
    await page.waitForTimeout(300)
  }
  await page.getByLabel('Username').fill(primaryUsername)
  await page.getByLabel('Display name').fill('Purring Fox')
  await shot(page, 'onboarding-filled')
  await page.getByRole('button', { name: 'Continue' }).click()
  await page.getByRole('link', { name: /AnonyMeow/ }).first().waitFor()

  console.log('2. Community browse (empty)')
  await page.goto(FRONTEND_URL + '/')
  await shot(page, 'community-browse')

  console.log('3. Create community')
  await page.goto(FRONTEND_URL + '/communities/new')
  await page.getByLabel('Name').fill(communityName)
  const descField = page.getByLabel('Description')
  if (await descField.count()) {
    await descField.fill('A cozy corner for sharing cat photos, stories, and everything feline.')
  }
  const rulesField = page.getByLabel('Rules (optional)')
  if (await rulesField.count()) {
    await rulesField.fill('1. Be kind.\n2. Cats only.\n3. No spam.')
  }
  await shot(page, 'create-community-form')
  await page.getByRole('button', { name: 'Create community' }).click()
  await page.waitForURL(new RegExp(`/c/${encodeURIComponent(communityName)}$`))
  await shot(page, 'community-detail')

  console.log('4. Create a flair (optional)')
  const flairsLink = page.getByRole('link', { name: /Flairs/i })
  if (await flairsLink.count()) {
    await flairsLink.click()
    await page.waitForURL(/\/flairs$/)
    const flairNameField = page.getByLabel(/name/i).first()
    if (await flairNameField.count()) {
      try {
        await flairNameField.fill('Cute Overload')
        const addFlairBtn = page.getByRole('button', { name: /add flair/i })
        if (await addFlairBtn.count()) {
          await addFlairBtn.click()
          await page.waitForTimeout(500)
        }
      } catch {
        // Flair form shape may vary; not critical to the walkthrough.
      }
    }
    await shot(page, 'community-flairs')
    await page.goto(FRONTEND_URL + `/c/${encodeURIComponent(communityName)}`)
  }

  console.log('5. Submit a poll post')
  await page.getByRole('link', { name: 'Submit post' }).click()
  await page.waitForURL(/\/submit$/)
  await page.getByLabel('Title').fill('Best cat toy?')
  const addPollCheckbox = page.getByLabel('Add a poll')
  await addPollCheckbox.check()
  const pollOptionInputs = page.getByPlaceholder(/Option \d/)
  await pollOptionInputs.nth(0).fill('Laser pointer')
  await pollOptionInputs.nth(1).fill('Cardboard box')
  const addOptionBtn = page.getByRole('button', { name: 'Add option' })
  if (await addOptionBtn.count()) {
    await addOptionBtn.click()
    await page.getByPlaceholder(/Option \d/).nth(2).fill('Feather wand')
  }
  await shot(page, 'poll-submit-form')
  await page.getByRole('button', { name: 'Post' }).click()
  await page.waitForURL(/\/posts\/.+/)
  const postUrl = page.url()
  await shot(page, 'post-detail-before-vote')

  console.log('6. Vote on the poll')
  const firstPollOption = page.getByText('Laser pointer').first()
  await firstPollOption.click()
  await page.waitForTimeout(500)
  await shot(page, 'poll-with-results')

  console.log('7. Second user signs up')
  const context2 = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    ignoreHTTPSErrors: true,
  })
  const page2 = await context2.newPage()
  await page2.goto(FRONTEND_URL + '/signup')
  await page2.getByRole('button', { name: 'Create account' }).click()
  await page2.getByRole('heading', { name: 'Welcome to AnonyMeow' }).waitFor()
  await page2.getByLabel('Username').fill(secondUsername)
  await page2.getByLabel('Display name').fill('Moderator Mia')
  await page2.getByRole('button', { name: 'Continue' }).click()
  await page2.getByRole('link', { name: /AnonyMeow/ }).first().waitFor()

  // Upvoting is done by the second user rather than the post's own author — the backend
  // rejects self-votes on posts/comments (VotingService throws SelfVoteException).
  console.log('8. Upvote the post (as the second user)')
  await page2.goto(postUrl)
  await page2.getByRole('button', { name: 'Upvote' }).first().click()
  await page2.waitForTimeout(300)
  await shot(page2, 'post-upvoted')

  console.log('9. Comments and nested reply')
  const commentBox = page.getByPlaceholder('Join the discussion…')
  await commentBox.fill('My cat ignores every toy except the box it came in. 😂')
  await page.getByRole('button', { name: 'Comment' }).click()
  await page.waitForTimeout(500)
  const replyBtn = page.getByRole('button', { name: 'Reply' }).first()
  if (await replyBtn.count()) {
    await replyBtn.click()
    const replyBox = page.getByPlaceholder('Write a reply…')
    await replyBox.fill('Same here — the box always wins.')
    await page.getByRole('button', { name: 'Reply' }).last().click()
    await page.waitForTimeout(500)
  }
  await shot(page, 'comments-thread')

  console.log('10. Second user: profile, message, friend request')
  await page2.goto(FRONTEND_URL + `/u/${primaryUsername}`)
  await shot(page2, 'profile-page-visitor-view')

  const messageBtn = page2.getByRole('button', { name: 'Message' })
  if (await messageBtn.count()) {
    await messageBtn.click()
    await page2.waitForURL(/\/messages\//)
    const composer = page2.getByPlaceholder('Write a message…')
    await composer.fill('Hey! Loved your poll about cat toys 🐱')
    await page2.getByRole('button', { name: 'Send' }).click()
    await page2.waitForTimeout(500)
    await shot(page2, 'conversation-thread')
    await page2.goto(FRONTEND_URL + `/u/${primaryUsername}`)
    await page2.waitForTimeout(500)
  }

  const addFriendBtn = page2.getByRole('button', { name: 'Add friend' })
  if (await addFriendBtn.count()) {
    await addFriendBtn.click()
    await page2.waitForTimeout(400)
    await shot(page2, 'friend-request-sent')
  }

  console.log('11. Back to first user: notifications + accept friend request')
  await page.bringToFront()
  await page.goto(FRONTEND_URL + '/notifications')
  await page.waitForTimeout(500)
  await shot(page, 'notifications-list')

  await page.goto(FRONTEND_URL + `/u/${secondUsername}`)
  await page.waitForTimeout(500)
  const acceptBtn = page.getByRole('button', { name: 'Accept' })
  if (await acceptBtn.count()) {
    await acceptBtn.click()
    await page.waitForTimeout(400)
  }
  await shot(page, 'friend-request-accepted')

  console.log('12. Final feed view')
  await page.goto(FRONTEND_URL + '/')
  await page.waitForTimeout(300)
  await shot(page, 'final-feed-populated')

  await context2.close()
  await context.close()
  await browser.close()

  // Playwright names video files by an internal id; rename to something readable.
  const videoFiles = fs.readdirSync(VIDEO_DIR).filter((f) => f.endsWith('.webm'))
  if (videoFiles.length) {
    const src = path.join(VIDEO_DIR, videoFiles[0])
    const dest = path.join(VIDEO_DIR, 'anonymeow-walkthrough.webm')
    fs.renameSync(src, dest)
    console.log(`Video saved: ${dest}`)
  }

  console.log(`\nDone. Post URL used: ${postUrl}`)
  console.log(`Screenshots: ${SHOTS_DIR}`)
  console.log(`Video: ${VIDEO_DIR}`)
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})
