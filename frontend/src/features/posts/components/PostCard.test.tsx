import { describe, expect, it } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useLocation } from 'react-router-dom'
import { renderWithProviders } from '@/test/test-utils'
import { useAuth } from '@/features/auth/useAuth'
import { PostCard } from './PostCard'
import type { PostResponse } from '../types'

const basePost: PostResponse = {
  id: 'post-1',
  communityName: 'cats',
  authorUsername: 'alice',
  authorDisplayName: null,
  authorAvatarSeed: 'alice-seed',
  title: 'Look at this cat',
  bodyMarkdown: 'body',
  url: null,
  imageUrls: [],
  score: 3,
  commentCount: 2,
  isPinned: false,
  isLocked: false,
  createdAtUtc: '2026-01-01T00:00:00Z',
  editedAtUtc: null,
  pollOptions: null,
  flair: null,
  reactions: [],
  viewerVote: null,
}

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

/** Mirrors SaveButton.test.tsx's gate: SaveButton (rendered inside PostCard) reads its
 * localStorage cache keyed by the current username, so it must not mount before auth resolves. */
function ReadyGate({ currentCommunityName }: { currentCommunityName?: string }) {
  const { status } = useAuth()
  if (status !== 'ready') return null
  return (
    <>
      <PostCard post={basePost} currentCommunityName={currentCommunityName} />
      <LocationDisplay />
    </>
  )
}

describe('PostCard', () => {
  it('renders the title as a large, bold heading-style text, not a link', async () => {
    renderWithProviders(<ReadyGate />, { withAuth: true })

    const title = await screen.findByText('Look at this cat')
    expect(title.className).toContain('font-bold')
    expect(title.className).toContain('text-xl')
    expect(screen.queryByRole('link', { name: 'Look at this cat' })).toBeNull()
  })

  it('places the title before the author/meta row in reading order', async () => {
    renderWithProviders(<ReadyGate />, { withAuth: true })

    const title = await screen.findByText('Look at this cat')
    const authorLink = screen.getByRole('link', { name: 'alice' })

    // DOCUMENT_POSITION_FOLLOWING means authorLink comes after the title in the DOM.
    // eslint-disable-next-line no-bitwise
    expect(title.compareDocumentPosition(authorLink) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('navigates to the post when clicking anywhere on the card', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ReadyGate />, { withAuth: true })

    const title = await screen.findByText('Look at this cat')
    await user.click(title)

    expect(screen.getByTestId('location')).toHaveTextContent('/posts/post-1')
  })

  it('does not navigate to the post when clicking the author link', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ReadyGate />, { withAuth: true })

    const authorLink = await screen.findByRole('link', { name: 'alice' })
    await user.click(authorLink)

    expect(screen.getByTestId('location')).toHaveTextContent('/u/alice')
  })

  it('shows which community the post was posted in by default', async () => {
    renderWithProviders(<ReadyGate />, { withAuth: true })

    expect(await screen.findByRole('link', { name: 'c/cats' })).toHaveAttribute('href', '/c/cats')
  })

  it('hides the community mention when already viewing that community', async () => {
    renderWithProviders(<ReadyGate currentCommunityName="cats" />, { withAuth: true })

    await screen.findByRole('link', { name: 'alice' })
    expect(screen.queryByRole('link', { name: 'c/cats' })).toBeNull()
  })

  it('does not show an "edited" marker for an unedited post', async () => {
    renderWithProviders(<ReadyGate />, { withAuth: true })

    await screen.findByRole('link', { name: 'alice' })
    expect(screen.queryByText('edited')).toBeNull()
  })

  it('shows an "edited" marker when the post has been edited', async () => {
    function EditedGate() {
      const { status } = useAuth()
      if (status !== 'ready') return null
      return <PostCard post={{ ...basePost, editedAtUtc: '2026-01-02T00:00:00Z' }} />
    }
    renderWithProviders(<EditedGate />, { withAuth: true })

    expect(await screen.findByText('edited')).toBeInTheDocument()
  })
})
