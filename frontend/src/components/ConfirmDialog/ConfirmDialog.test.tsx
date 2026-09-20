import { describe, expect, it, vi } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/test-utils'
import { ConfirmDialog } from './ConfirmDialog'

describe('ConfirmDialog', () => {
  it('does not call onConfirm until the confirm button is clicked', async () => {
    const onConfirm = vi.fn()
    renderWithProviders(
      <ConfirmDialog open onOpenChange={() => {}} title="Delete this comment?" onConfirm={onConfirm} />
    )

    expect(screen.getByRole('dialog')).toHaveTextContent('Delete this comment?')
    expect(onConfirm).not.toHaveBeenCalled()

    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }))
    expect(onConfirm).toHaveBeenCalledOnce()
  })

  it('calls onOpenChange(false) without calling onConfirm when Cancel is clicked', async () => {
    const onConfirm = vi.fn()
    const onOpenChange = vi.fn()
    renderWithProviders(
      <ConfirmDialog open onOpenChange={onOpenChange} title="Delete this post?" onConfirm={onConfirm} />
    )

    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onConfirm).not.toHaveBeenCalled()
    expect(onOpenChange).toHaveBeenCalledWith(false)
  })

  it('renders nothing visible when closed', () => {
    renderWithProviders(
      <ConfirmDialog open={false} onOpenChange={() => {}} title="Delete this post?" onConfirm={() => {}} />
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
