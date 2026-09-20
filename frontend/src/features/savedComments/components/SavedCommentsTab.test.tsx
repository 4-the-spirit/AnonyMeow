import { describe, expect, it } from 'vitest'
import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import type { CommentResponse } from '@/features/comments/types'
import { SavedCommentsTab } from './SavedCommentsTab'

const BASE_URL = 'https://test.local'

const longBody =
  'This is a very long saved comment body that should be truncated in the list view. '.repeat(5)

const savedComment: CommentResponse = {
  id: 'comment-1',
  authorUsername: 'alice',
  authorDisplayName: null,
  authorAvatarSeed: 'alice-seed',
  bodyMarkdown: longBody,
  score: 4,
  replyCount: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
  editedAtUtc: null,
  reactions: [],
  postId: 'post-1',
  ancestorCommentIds: [],
  communityName: 'cats',
}

describe('SavedCommentsTab', () => {
  it('shows the author, community, and a clamped body preview for each saved comment', async () => {
    server.use(
      http.get(`${BASE_URL}/api/users/me/saved-comments`, () =>
        HttpResponse.json({ items: [savedComment], page: 1, pageSize: 20, totalCount: 1 })
      )
    )

    renderWithProviders(<SavedCommentsTab />, { withAuth: true })

    const authorLink = await screen.findByRole('link', { name: 'alice' })
    expect(authorLink).toHaveAttribute('href', '/u/alice')
    expect(authorLink.parentElement).toHaveTextContent('Posted by alice in c/cats')

    const preview = screen.getByText(/This is a very long saved comment body/)
    expect(preview.className).toContain('line-clamp-2')
  })
})
