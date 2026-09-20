import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { MessageComposer } from './MessageComposer'
import type { MessageResponse } from '../types'

const BASE_URL = 'https://test.local'

const original: MessageResponse = {
  id: 'msg-1',
  conversationId: 'conv-1',
  senderId: 'other-user-id',
  body: 'what time works for you?',
  isRead: false,
  createdAtUtc: '2026-01-01T00:00:00Z',
  replyToMessageId: null,
  replyToSenderId: null,
  replyToBodyPreview: null,
}

describe('MessageComposer', () => {
  it('sends a plain message with no reply target', async () => {
    let posted: unknown = null
    server.use(
      http.post(`${BASE_URL}/api/conversations/conv-1/messages`, async ({ request }) => {
        posted = await request.json()
        return HttpResponse.json({ ...original, id: 'msg-2', body: 'hi' }, { status: 201 })
      })
    )

    renderWithProviders(<MessageComposer conversationId="conv-1" otherDisplayName="alice" />)

    await userEvent.type(screen.getByPlaceholderText('Write a message…'), 'hi')
    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    await waitFor(() => expect(posted).toMatchObject({ body: 'hi' }))
  })

  it('shows a reply banner and includes replyToMessageId when replying', async () => {
    let posted: unknown = null
    server.use(
      http.post(`${BASE_URL}/api/conversations/conv-1/messages`, async ({ request }) => {
        posted = await request.json()
        return HttpResponse.json({ ...original, id: 'msg-2', body: '3pm works' }, { status: 201 })
      })
    )

    renderWithProviders(
      <MessageComposer conversationId="conv-1" otherDisplayName="alice" replyTo={original} />
    )

    expect(screen.getByText(/Replying to alice/)).toBeInTheDocument()
    expect(screen.getByText(/what time works for you\?/)).toBeInTheDocument()

    await userEvent.type(screen.getByPlaceholderText('Write a message…'), '3pm works')
    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    await waitFor(() => expect(posted).toMatchObject({ body: '3pm works', replyToMessageId: 'msg-1' }))
  })

  it('cancels the reply when the cancel button is clicked', async () => {
    let onCancelCalled = false
    renderWithProviders(
      <MessageComposer
        conversationId="conv-1"
        otherDisplayName="alice"
        replyTo={original}
        onCancelReply={() => {
          onCancelCalled = true
        }}
      />
    )

    await userEvent.click(screen.getByRole('button', { name: 'Cancel reply' }))

    expect(onCancelCalled).toBe(true)
  })

  it('shows a blocked notice instead of the composer when blockedByMe is true', () => {
    renderWithProviders(
      <MessageComposer conversationId="conv-1" otherDisplayName="alice" blockedByMe />
    )

    expect(
      screen.getByText("You've blocked this user. Unblock them to send messages.")
    ).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('Write a message…')).not.toBeInTheDocument()
  })
})
