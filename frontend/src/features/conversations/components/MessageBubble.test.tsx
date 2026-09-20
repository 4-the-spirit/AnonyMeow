import { describe, expect, it, vi } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/test-utils'
import { MessageBubble } from './MessageBubble'
import type { MessageResponse } from '../types'

const message: MessageResponse = {
  id: 'msg-1',
  conversationId: 'conv-1',
  senderId: 'user-a',
  body: 'hello there',
  isRead: false,
  createdAtUtc: '2026-01-01T00:00:00Z',
  replyToMessageId: null,
  replyToSenderId: null,
  replyToBodyPreview: null,
}

describe('MessageBubble', () => {
  it('aligns the current user\'s own messages to the end, with no report option', () => {
    renderWithProviders(<MessageBubble message={message} isMine />)

    expect(screen.getByText('hello there').closest('.items-end')).not.toBeNull()
    expect(screen.queryByRole('button', { name: 'Report' })).toBeNull()
  })

  it("aligns the other participant's messages to the start, with a report option", () => {
    renderWithProviders(<MessageBubble message={message} isMine={false} />)

    expect(screen.getByText('hello there').closest('.items-start')).not.toBeNull()
    expect(screen.getByRole('button', { name: 'Report' })).toBeInTheDocument()
  })

  it('shows the quoted original message when the message is a reply', () => {
    const reply: MessageResponse = {
      ...message,
      id: 'msg-2',
      body: 'sounds good',
      replyToMessageId: 'msg-1',
      replyToSenderId: 'user-a',
      replyToBodyPreview: 'hello there',
    }

    renderWithProviders(<MessageBubble message={reply} isMine={false} />)

    expect(screen.getByText('hello there')).toBeInTheDocument()
    expect(screen.getByText('sounds good')).toBeInTheDocument()
  })

  it('falls back to a placeholder when the replied-to message is unavailable', () => {
    const reply: MessageResponse = {
      ...message,
      id: 'msg-2',
      body: 'sounds good',
      replyToMessageId: 'msg-1',
      replyToSenderId: null,
      replyToBodyPreview: null,
    }

    renderWithProviders(<MessageBubble message={reply} isMine={false} />)

    expect(screen.getByText('Original message unavailable')).toBeInTheDocument()
  })

  it('calls onReply with the message when the reply button is clicked', async () => {
    const onReply = vi.fn()
    renderWithProviders(<MessageBubble message={message} isMine={false} onReply={onReply} />)

    await userEvent.click(screen.getByRole('button', { name: 'Reply' }))

    expect(onReply).toHaveBeenCalledWith(message)
  })
})
