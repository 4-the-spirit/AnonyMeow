import { describe, expect, it } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/test-utils'
import { LinkWarningDialog } from './LinkWarningDialog'

describe('LinkWarningDialog', () => {
  it('opens a confirmation naming the destination host before navigating away', async () => {
    renderWithProviders(
      <LinkWarningDialog href="https://example.com/some/path">https://example.com/some/path</LinkWarningDialog>
    )

    await userEvent.click(screen.getByRole('button', { name: 'https://example.com/some/path' }))

    const dialog = await screen.findByRole('dialog')
    expect(dialog).toHaveTextContent('example.com')
  })

  it('renders Continue as a real link to the destination, and closes the dialog on click', async () => {
    renderWithProviders(<LinkWarningDialog href="https://example.com">https://example.com</LinkWarningDialog>)
    await userEvent.click(screen.getByRole('button', { name: 'https://example.com' }))

    const continueLink = await screen.findByRole('link', { name: 'Continue' })
    expect(continueLink).toHaveAttribute('href', 'https://example.com')
    expect(continueLink).toHaveAttribute('target', '_blank')
    expect(continueLink).toHaveAttribute('rel', 'noopener noreferrer')

    await userEvent.click(continueLink)
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('does not render a link when Cancel is clicked', async () => {
    renderWithProviders(<LinkWarningDialog href="https://example.com">https://example.com</LinkWarningDialog>)
    await userEvent.click(screen.getByRole('button', { name: 'https://example.com' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('link', { name: 'Continue' })).not.toBeInTheDocument()
  })
})
