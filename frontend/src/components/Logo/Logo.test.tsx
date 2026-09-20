import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Logo } from './Logo'

describe('Logo', () => {
  it('renders the mascot video with the given size', () => {
    render(<Logo size={32} />)

    const video = screen.getByLabelText('AnonyMeow')
    expect(video.tagName).toBe('VIDEO')
    expect(video).toHaveAttribute('src', '/logo/mascot-waving.mp4')
    expect(video).toHaveAttribute('width', '32')
    expect(video).toHaveAttribute('height', '32')
  })
})
