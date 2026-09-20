import { describe, expect, it, vi, afterEach, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import { renderWithProviders } from '@/test/test-utils'
import { PresenceIndicator } from './PresenceIndicator'

describe('PresenceIndicator', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-01T12:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('shows "Online" when last seen within the online threshold', () => {
    renderWithProviders(
      <PresenceIndicator lastSeenAt="2026-01-01T11:59:00Z" showLabel />
    )

    expect(screen.getByText('Online')).toBeInTheDocument()
  })

  it('shows a relative "last seen" time when outside the online threshold', () => {
    renderWithProviders(
      <PresenceIndicator lastSeenAt="2026-01-01T11:00:00Z" showLabel />
    )

    expect(screen.getByText(/Last seen/)).toBeInTheDocument()
  })

  it('shows "Hasn\'t been active yet" when there is no last-seen timestamp at all', () => {
    renderWithProviders(<PresenceIndicator lastSeenAt={null} showLabel />)

    expect(screen.getByText("Hasn't been active yet")).toBeInTheDocument()
  })

  it('omits the label when showLabel is not set', () => {
    renderWithProviders(<PresenceIndicator lastSeenAt="2026-01-01T11:59:00Z" />)

    expect(screen.queryByText('Online')).not.toBeInTheDocument()
  })
})
