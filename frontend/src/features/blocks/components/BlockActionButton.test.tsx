import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { useAuth } from '@/features/auth/useAuth'
import { BlockActionButton } from './BlockActionButton'

const BASE_URL = 'https://test.local'

/**
 * BlockActionButton reads its localStorage cache once, via a lazy useState initializer, so it
 * must not mount until the current user is resolved (the same condition RootGate enforces in
 * the real app — AppLayout's comment notes children only render once auth status is "ready").
 * Mirrors that gate here since renderWithProviders mounts immediately, unlike RootGate.
 */
function ReadyGate({ username }: { username: string }) {
  const { status } = useAuth()
  if (status !== 'ready') return null
  return <BlockActionButton username={username} />
}

describe('BlockActionButton', () => {
  it('blocks a user and flips the label to Unblock', async () => {
    let blockedUsername = ''
    server.use(
      http.post(`${BASE_URL}/api/users/alice/block`, () => {
        blockedUsername = 'alice'
        return HttpResponse.json(
          { blockedUsername: 'alice', createdAtUtc: '2026-01-01T00:00:00Z' },
          { status: 201 }
        )
      })
    )

    renderWithProviders(<ReadyGate username="alice" />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Block' })
    await userEvent.click(button)

    await waitFor(() => expect(blockedUsername).toBe('alice'))
    expect(await screen.findByRole('button', { name: 'Unblock' })).toBeInTheDocument()
  })

  it('unblocks a user and flips the label back to Block', async () => {
    localStorage.setItem('anonymeow.blockedUsers.testuser', JSON.stringify(['alice']))
    let unblockedUsername = ''
    server.use(
      http.delete(`${BASE_URL}/api/users/alice/block`, () => {
        unblockedUsername = 'alice'
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderWithProviders(<ReadyGate username="alice" />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Unblock' })
    await userEvent.click(button)

    await waitFor(() => expect(unblockedUsername).toBe('alice'))
    expect(await screen.findByRole('button', { name: 'Block' })).toBeInTheDocument()

    localStorage.clear()
  })

  it('calls onBlockedChange with the new state on every successful toggle', async () => {
    server.use(
      http.post(`${BASE_URL}/api/users/alice/block`, () =>
        HttpResponse.json({ blockedUsername: 'alice', createdAtUtc: '2026-01-01T00:00:00Z' }, { status: 201 })
      )
    )
    const changes: boolean[] = []

    function ReadyGateWithCallback() {
      const { status } = useAuth()
      if (status !== 'ready') return null
      return <BlockActionButton username="alice" onBlockedChange={(blocked) => changes.push(blocked)} />
    }

    renderWithProviders(<ReadyGateWithCallback />, { withAuth: true })

    const button = await screen.findByRole('button', { name: 'Block' })
    await userEvent.click(button)

    await waitFor(() => expect(changes).toEqual([true]))
  })
})
