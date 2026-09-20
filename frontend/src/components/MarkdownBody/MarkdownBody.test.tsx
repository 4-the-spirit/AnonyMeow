import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { MarkdownBody } from './MarkdownBody'

describe('MarkdownBody', () => {
  it('renders basic markdown formatting', () => {
    render(<MarkdownBody>{'**bold** and _italic_ and [a link](https://example.com)'}</MarkdownBody>)

    expect(screen.getByText('bold').tagName).toBe('STRONG')
    expect(screen.getByText('italic').tagName).toBe('EM')
    // External markdown links no longer navigate directly — see the dedicated test below.
    expect(screen.getByRole('button', { name: 'a link' })).toBeInTheDocument()
  })

  it('shows the "leaving AnonyMeow" confirmation before navigating an author-written link', async () => {
    render(<MarkdownBody>{'[a link](https://example.com)'}</MarkdownBody>)

    await userEvent.click(screen.getByRole('button', { name: 'a link' }))

    const dialog = await screen.findByRole('dialog')
    expect(dialog).toHaveTextContent('example.com')

    const continueLink = screen.getByRole('link', { name: 'Continue' })
    expect(continueLink).toHaveAttribute('href', 'https://example.com')
    expect(continueLink).toHaveAttribute('target', '_blank')
  })

  it('never renders embedded raw HTML or executes event handlers (XSS guard)', () => {
    const malicious = 'before <img src=x onerror=alert(1)> after'
    const { container } = render(<MarkdownBody>{malicious}</MarkdownBody>)

    // react-markdown without rehype-raw renders the tag literally as escaped text, not as
    // a live DOM element — no <img> is created, so its onerror handler can never fire.
    expect(container.querySelector('img')).toBeNull()
    expect(container.querySelector('[onerror]')).toBeNull()
    expect(container.textContent).toContain('before')
    expect(container.textContent).toContain('<img src=x onerror=alert(1)>')
    expect(container.textContent).toContain('after')
  })

  it('turns @mentions into profile links, leaving surrounding text and external links intact', () => {
    render(
      <MemoryRouter>
        <MarkdownBody>{'hey @alice_99 check [this](https://example.com) out @b'}</MarkdownBody>
      </MemoryRouter>
    )

    expect(screen.getByRole('link', { name: '@alice_99' })).toHaveAttribute(
      'href',
      '/u/alice_99'
    )
    // External links render as a button that opens the "leaving AnonyMeow" confirmation
    // (see the dedicated test above) rather than a plain <a>.
    expect(screen.getByRole('button', { name: 'this' })).toBeInTheDocument()
    // Below the backend's 3-character minimum username length — left as plain text, not a link.
    expect(screen.queryByRole('link', { name: '@b' })).toBeNull()
    expect(screen.getByText(/out @b/)).toBeInTheDocument()
  })
})
