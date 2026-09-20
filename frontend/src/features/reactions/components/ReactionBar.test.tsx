import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import i18n from '@/lib/i18n'
import { formatCompactNumber } from '@/lib/formatCompactNumber'
import { ReactionBar } from './ReactionBar'

const BASE_URL = 'https://test.local'

describe('ReactionBar', () => {
  it('PUTs to /api/posts/:id/reactions/:emoji when picking an emoji with no prior reaction', async () => {
    let calledUrl = ''
    server.use(
      http.put(`${BASE_URL}/api/posts/post-1/reactions/:emoji`, ({ request }) => {
        calledUrl = request.url
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <ReactionBar
        targetType="Post"
        targetId="post-1"
        reactions={[]}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    await userEvent.click(screen.getByRole('button', { name: /React/ }))
    await userEvent.click(screen.getByRole('menuitem', { name: 'fire' }))

    await waitFor(() =>
      expect(calledUrl).toContain(`/api/posts/post-1/reactions/${encodeURIComponent('🔥')}`)
    )
    // The button itself carries no count text (that only lives in the hover tooltip); it
    // just swaps its icon to the picked emoji, keeping the visible "React" label.
    const button = screen.getByRole('button', { name: /React/ })
    expect(button).toBeInTheDocument()
    expect(screen.getByAltText('fire')).toBeInTheDocument()
  })

  it('DELETEs the reaction when picking the already-active emoji again', async () => {
    let deleteCalled = false
    server.use(
      http.delete(`${BASE_URL}/api/comments/comment-1/reactions/:emoji`, () => {
        deleteCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <ReactionBar
        targetType="Comment"
        targetId="comment-1"
        reactions={[{ emoji: '👍', count: 3, reactedByViewer: true }]}
        invalidateKey={['comments']}
      />,
      { withAuth: true }
    )

    const button = screen.getByRole('button', { name: /React/ })
    expect(screen.getByAltText('thumbs up')).toBeInTheDocument()
    await userEvent.click(button)
    await userEvent.click(screen.getByRole('menuitem', { name: 'thumbs up' }))

    await waitFor(() => expect(deleteCalled).toBe(true))
  })

  it('PUTs the newly picked emoji (not a DELETE) when switching away from an existing reaction', async () => {
    let putUrl = ''
    let deleteCalled = false
    server.use(
      http.put(`${BASE_URL}/api/posts/post-1/reactions/:emoji`, ({ request }) => {
        putUrl = request.url
        return new HttpResponse(null, { status: 204 })
      }),
      http.delete(`${BASE_URL}/api/posts/post-1/reactions/:emoji`, () => {
        deleteCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <ReactionBar
        targetType="Post"
        targetId="post-1"
        reactions={[{ emoji: '👍', count: 1, reactedByViewer: true }]}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    await userEvent.click(screen.getByRole('button', { name: /React/ }))
    await userEvent.click(screen.getByRole('menuitem', { name: 'fire' }))

    await waitFor(() =>
      expect(putUrl).toContain(`/api/posts/post-1/reactions/${encodeURIComponent('🔥')}`)
    )
    expect(deleteCalled).toBe(false)
    // Switching (not adding/removing) swaps the button's icon to the newly picked emoji.
    expect(screen.getByAltText('fire')).toBeInTheDocument()
  })

  it('shows a per-emoji count breakdown in the hover tooltip', async () => {
    renderWithProviders(
      <ReactionBar
        targetType="Post"
        targetId="post-1"
        reactions={[
          { emoji: '👍', count: 3, reactedByViewer: false },
          { emoji: '🔥', count: 1200, reactedByViewer: false },
        ]}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    await userEvent.hover(screen.getByRole('button', { name: /React/ }))

    // Intl's compact formatter can wrap the digits in invisible bidi control characters
    // (e.g. U+200F in the 'he' locale) — strip those before comparing displayed text. Radix
    // also renders the tooltip content twice (once visible, once in a visually-hidden a11y
    // announcer), so assert with `getAllByText` rather than the single-match `getByText`.
    const stripBidi = (text: string) => text.replace(/[‎‏⁦-⁩]/g, '')
    const expectedLarge = stripBidi(formatCompactNumber(1200, i18n.language))
    const expectedSmall = stripBidi(formatCompactNumber(3, i18n.language))
    await waitFor(() =>
      expect(screen.getAllByText((text) => stripBidi(text) === expectedLarge).length).toBeGreaterThan(0)
    )
    expect(screen.getAllByText((text) => stripBidi(text) === expectedSmall).length).toBeGreaterThan(0)
  })
})
