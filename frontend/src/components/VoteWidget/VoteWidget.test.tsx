import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { VoteWidget } from './VoteWidget'

const BASE_URL = 'https://test.local'

describe('VoteWidget', () => {
  it('reflects a server-sourced viewerVote as the pressed/colored arrow on mount', () => {
    renderWithProviders(
      <VoteWidget
        targetType="Post"
        targetId="post-1"
        score={5}
        viewerVote={-1}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    expect(screen.getByRole('button', { name: 'Downvote' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Upvote' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('PUTs to /api/posts/:id/vote when upvoting a post', async () => {
    let receivedBody: unknown = null
    server.use(
      http.put(`${BASE_URL}/api/posts/post-1/vote`, async ({ request }) => {
        receivedBody = await request.json()
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <VoteWidget
        targetType="Post"
        targetId="post-1"
        score={5}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    await userEvent.click(screen.getByRole('button', { name: 'Upvote' }))

    await waitFor(() => expect(receivedBody).toEqual({ value: 1 }))
    expect(screen.getByText('6')).toBeInTheDocument()
  })

  it('PUTs to /api/comments/:id/vote when voting a comment', async () => {
    let calledUrl = ''
    server.use(
      http.put(`${BASE_URL}/api/comments/comment-1/vote`, ({ request }) => {
        calledUrl = request.url
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <VoteWidget
        targetType="Comment"
        targetId="comment-1"
        score={0}
        invalidateKey={['comments', 'comment-1']}
      />,
      { withAuth: true }
    )

    await userEvent.click(screen.getByRole('button', { name: 'Downvote' }))

    await waitFor(() => expect(calledUrl).toContain('/api/comments/comment-1/vote'))
    expect(screen.getByText('-1')).toBeInTheDocument()
  })

  it('removes the vote when clicking the same direction again', async () => {
    let deleteCalled = false
    server.use(
      http.put(`${BASE_URL}/api/posts/post-1/vote`, () => new HttpResponse(null, { status: 204 })),
      http.delete(`${BASE_URL}/api/posts/post-1/vote`, () => {
        deleteCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <VoteWidget
        targetType="Post"
        targetId="post-1"
        score={5}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    const upvote = screen.getByRole('button', { name: 'Upvote' })
    await userEvent.click(upvote)
    await waitFor(() => expect(screen.getByText('6')).toBeInTheDocument())
    await userEvent.click(upvote)

    await waitFor(() => expect(deleteCalled).toBe(true))
    expect(screen.getByText('5')).toBeInTheDocument()
  })

  it('swinging from upvote to downvote un-votes on the first click and only casts on the second', async () => {
    let putCallCount = 0
    let putBody: unknown = null
    let deleteCalled = false
    server.use(
      http.put(`${BASE_URL}/api/posts/post-1/vote`, async ({ request }) => {
        putCallCount += 1
        putBody = await request.json()
        return new HttpResponse(null, { status: 204 })
      }),
      http.delete(`${BASE_URL}/api/posts/post-1/vote`, () => {
        deleteCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(
      <VoteWidget
        targetType="Post"
        targetId="post-1"
        score={5}
        invalidateKey={['posts', 'detail', 'post-1']}
      />,
      { withAuth: true }
    )

    const upvote = screen.getByRole('button', { name: 'Upvote' })
    const downvote = screen.getByRole('button', { name: 'Downvote' })

    await userEvent.click(upvote)
    await waitFor(() => expect(putCallCount).toBe(1))
    expect(screen.getByText('6')).toBeInTheDocument()

    // First click on the opposite arrow only removes the existing vote (net -1), it does
    // not cast the downvote in the same click.
    await userEvent.click(downvote)
    await waitFor(() => expect(deleteCalled).toBe(true))
    expect(putCallCount).toBe(1)
    expect(screen.getByText('5')).toBeInTheDocument()

    // Second click actually casts the downvote (another net -1, not -2 total from the swing).
    await userEvent.click(downvote)
    await waitFor(() => expect(putCallCount).toBe(2))
    expect(putBody).toEqual({ value: -1 })
    expect(screen.getByText('4')).toBeInTheDocument()
  })
})
