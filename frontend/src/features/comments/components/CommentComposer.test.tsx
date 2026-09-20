import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { CommentComposer } from './CommentComposer'

const BASE_URL = 'https://test.local'

describe('CommentComposer', () => {
  it('shows a sign-in prompt instead of the form for an anonymous visitor', () => {
    renderWithProviders(<CommentComposer postId="post-1" />, { route: '/posts/post-1' })

    expect(screen.getByRole('link', { name: 'Sign in to join the discussion.' })).toHaveAttribute(
      'href',
      '/signin'
    )
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('lets a signed-in user post a comment', async () => {
    let posted: unknown = null
    server.use(
      http.post(`${BASE_URL}/api/posts/post-1/comments`, async ({ request }) => {
        posted = await request.json()
        return HttpResponse.json(
          {
            id: 'comment-1',
            authorUsername: 'testuser',
            bodyMarkdown: 'Hello there',
            score: 0,
            replyCount: 0,
            createdAtUtc: '2026-01-01T00:00:00Z',
          },
          { status: 201 }
        )
      })
    )

    renderWithProviders(<CommentComposer postId="post-1" />, { withAuth: true })

    const textbox = await screen.findByRole('textbox')
    await userEvent.type(textbox, 'Hello there')
    await userEvent.click(screen.getByRole('button', { name: 'Comment' }))

    await waitFor(() => expect(posted).toMatchObject({ bodyMarkdown: 'Hello there' }))
  })
})
