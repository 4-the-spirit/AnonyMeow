import { describe, expect, it } from 'vitest'
import { screen } from '@testing-library/react'
import { renderWithProviders } from '@/test/test-utils'
import { AuthLayout } from './AuthLayout'

describe('AuthLayout', () => {
  it('renders the wordmark in the logo font, the tagline, and the given children', () => {
    renderWithProviders(
      <AuthLayout>
        <p>form content</p>
      </AuthLayout>
    )

    const wordmark = screen.getByText('AnonyMeow')
    expect(wordmark.className).toContain('font-logo')
    expect(screen.getByText('Anonymous, no-pressure community discussion.')).toBeInTheDocument()
    expect(screen.getByText('form content')).toBeInTheDocument()
  })

  it('renders the mascot logo', () => {
    renderWithProviders(
      <AuthLayout>
        <p>form content</p>
      </AuthLayout>
    )

    expect(screen.getByLabelText('AnonyMeow').tagName).toBe('VIDEO')
  })
})
