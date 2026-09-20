import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { FriendActionButton } from './FriendActionButton'

const BASE_URL = 'https://test.local'

function mockFriendState(options: {
  friends?: string[]
  incoming?: string[]
  outgoing?: string[]
}) {
  const { friends = [], incoming = [], outgoing = [] } = options
  server.use(
    http.get(`${BASE_URL}/api/users/testuser/friends`, () =>
      HttpResponse.json(
        friends.map((username) => ({
          username,
          displayName: null,
          avatarSeed: null,
          friendsSinceUtc: '2026-01-01T00:00:00Z',
        }))
      )
    ),
    http.get(`${BASE_URL}/api/users/me/friend-requests`, () =>
      HttpResponse.json({
        incoming: incoming.map((requesterUsername) => ({
          requesterUsername,
          addresseeUsername: 'testuser',
          status: 'Pending',
          createdAtUtc: '2026-01-01T00:00:00Z',
        })),
        outgoing: outgoing.map((addresseeUsername) => ({
          requesterUsername: 'testuser',
          addresseeUsername,
          status: 'Pending',
          createdAtUtc: '2026-01-01T00:00:00Z',
        })),
      })
    )
  )
}

describe('FriendActionButton', () => {
  it('shows "Add friend" and sends a request when no relationship exists', async () => {
    mockFriendState({})
    let requestedUsername = ''
    server.use(
      http.post(`${BASE_URL}/api/users/alice/friend-requests`, () => {
        requestedUsername = 'alice'
        return HttpResponse.json(
          { requesterUsername: 'testuser', addresseeUsername: 'alice', status: 'Pending', createdAtUtc: '2026-01-01T00:00:00Z' },
          { status: 201 }
        )
      })
    )

    renderWithProviders(<FriendActionButton username="alice" />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Add friend' })
    await userEvent.click(button)

    await waitFor(() => expect(requestedUsername).toBe('alice'))
  })

  it('shows "Friends ✓" when already friends', async () => {
    mockFriendState({ friends: ['alice'] })

    renderWithProviders(<FriendActionButton username="alice" />, { withAuth: true })

    expect(await screen.findByRole('button', { name: 'Friends ✓' })).toBeInTheDocument()
  })

  it('confirms before removing a friend, via an in-app dialog rather than window.confirm', async () => {
    mockFriendState({ friends: ['alice'] })
    let removedUsername = ''
    server.use(
      http.delete(`${BASE_URL}/api/friends/alice`, () => {
        removedUsername = 'alice'
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(<FriendActionButton username="alice" />, { withAuth: true })

    await userEvent.click(await screen.findByRole('button', { name: 'Friends ✓' }))

    const dialog = await screen.findByRole('dialog')
    expect(dialog).toHaveTextContent('Remove u/alice as a friend?')
    expect(removedUsername).toBe('')

    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }))
    await waitFor(() => expect(removedUsername).toBe('alice'))
  })

  it('shows a "Cancel request" button that withdraws an outgoing request', async () => {
    mockFriendState({ outgoing: ['alice'] })
    let cancelledUsername = ''
    server.use(
      http.delete(`${BASE_URL}/api/users/alice/friend-requests`, () => {
        cancelledUsername = 'alice'
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(<FriendActionButton username="alice" />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Cancel request' })
    expect(button).toBeEnabled()
    await userEvent.click(button)

    await waitFor(() => expect(cancelledUsername).toBe('alice'))
  })

  it('shows Accept/Decline when the other user sent an incoming request', async () => {
    mockFriendState({ incoming: ['alice'] })

    renderWithProviders(<FriendActionButton username="alice" />, { withAuth: true })

    expect(await screen.findByRole('button', { name: 'Accept' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Decline' })).toBeInTheDocument()
  })
})
