import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { useAuth } from '@/features/auth/useAuth'
import { CommentSaveButton } from './CommentSaveButton'

const BASE_URL = 'https://test.local'

/** Mirrors SaveButton.test.tsx's gate: CommentSaveButton reads its localStorage cache via a
 * lazy useState initializer, so it must not mount until the current user is resolved. */
function ReadyGate({ commentId }: { commentId: string }) {
  const { status } = useAuth()
  if (status !== 'ready') return null
  return <CommentSaveButton commentId={commentId} />
}

describe('CommentSaveButton', () => {
  it('saves a comment and flips the label to Unsave', async () => {
    let saveCalled = false
    server.use(
      http.post(`${BASE_URL}/api/comments/comment-1/save`, () => {
        saveCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(<ReadyGate commentId="comment-1" />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Save' })
    await userEvent.click(button)

    await waitFor(() => expect(saveCalled).toBe(true))
    expect(await screen.findByRole('button', { name: 'Unsave' })).toBeInTheDocument()
  })

  it('unsaves a comment and flips the label back to Save', async () => {
    localStorage.setItem('anonymeow.savedComments.testuser', JSON.stringify(['comment-1']))
    let unsaveCalled = false
    server.use(
      http.delete(`${BASE_URL}/api/comments/comment-1/save`, () => {
        unsaveCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(<ReadyGate commentId="comment-1" />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Unsave' })
    await userEvent.click(button)

    await waitFor(() => expect(unsaveCalled).toBe(true))
    expect(await screen.findByRole('button', { name: 'Save' })).toBeInTheDocument()

    localStorage.clear()
  })
})
