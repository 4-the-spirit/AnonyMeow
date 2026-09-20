import { describe, expect, it } from 'vitest'
import { createEvent, fireEvent, render, screen } from '@testing-library/react'
import { MascotVideo } from './MascotVideo'

describe('MascotVideo', () => {
  it('renders an autoplaying, muted, looping video with the given source', () => {
    render(<MascotVideo src="/mascot/mascot-waving.mp4" aria-label="AnonyMeow" />)

    const video = screen.getByLabelText('AnonyMeow')
    expect(video.tagName).toBe('VIDEO')
    expect(video).toHaveAttribute('src', '/mascot/mascot-waving.mp4')
    expect(video).toHaveAttribute('autoplay')
    expect(video).toHaveAttribute('loop')
    // React reflects `muted` as a DOM property rather than an HTML attribute.
    expect((video as HTMLVideoElement).muted).toBe(true)
  })

  it('blocks the right-click context menu to deter casual video saving', () => {
    render(<MascotVideo src="/mascot/mascot-waving.mp4" aria-label="AnonyMeow" />)

    const video = screen.getByLabelText('AnonyMeow')
    const contextMenuEvent = createEvent.contextMenu(video)
    fireEvent(video, contextMenuEvent)

    expect(contextMenuEvent.defaultPrevented).toBe(true)
  })

  it('disables dragging, download, and picture-in-picture affordances', () => {
    render(<MascotVideo src="/mascot/mascot-waving.mp4" aria-label="AnonyMeow" />)

    const video = screen.getByLabelText('AnonyMeow')
    expect(video).toHaveAttribute('draggable', 'false')
    expect(video).toHaveAttribute('controlslist', 'nodownload noremoteplayback nofullscreen')
    expect(video).toHaveAttribute('disablepictureinpicture')
  })
})
