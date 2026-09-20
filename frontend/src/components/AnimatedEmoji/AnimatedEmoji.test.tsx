import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AnimatedEmoji } from './AnimatedEmoji'

describe('AnimatedEmoji', () => {
  it('renders a known emoji as its animated PNG', () => {
    render(<AnimatedEmoji emoji="🔥" size={32} />)

    const img = screen.getByRole('img', { name: 'fire' })
    expect(img).toHaveAttribute('src', '/emoji/fire.png')
    expect(img).toHaveAttribute('width', '32')
    expect(img).toHaveAttribute('height', '32')
  })

  it('falls back to the plain character for an unmapped emoji', () => {
    render(<AnimatedEmoji emoji="🎉" />)

    expect(screen.getByText('🎉')).toBeInTheDocument()
    expect(screen.queryByRole('img')).not.toBeInTheDocument()
  })
})
