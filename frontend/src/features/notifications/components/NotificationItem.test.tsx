import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { useLocation } from 'react-router-dom'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { NotificationItem } from './NotificationItem'
import type { NotificationResponse } from '../types'

const BASE_URL = 'https://test.local'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname + location.search}</div>
}

function makeNotification(overrides: Partial<NotificationResponse>): NotificationResponse {
  return {
    id: 'notif-1',
    type: 'Reply',
    sourceType: 'Comment',
    sourceId: 'comment-1',
    isRead: false,
    previewText: 'someone replied',
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

describe('NotificationItem', () => {
  it('resolves the comment via GET /api/comments/{id} and navigates to the highlighted comment', async () => {
    server.use(
      http.get(`${BASE_URL}/api/comments/comment-1`, () =>
        HttpResponse.json({
          id: 'comment-1',
          authorUsername: 'alice',
          bodyMarkdown: 'hi',
          score: 0,
          replyCount: 0,
          createdAtUtc: '2026-01-01T00:00:00Z',
          editedAtUtc: null,
          postId: 'post-1',
          ancestorCommentIds: ['root-1'],
        })
      ),
      http.post(
        `${BASE_URL}/api/notifications/notif-1/read`,
        () => new HttpResponse(null, { status: 200 })
      )
    )

    renderWithProviders(
      <>
        <NotificationItem notification={makeNotification({})} />
        <LocationDisplay />
      </>
    )

    await userEvent.click(screen.getByRole('button'))

    await waitFor(() =>
      expect(screen.getByTestId('location').textContent).toBe(
        '/posts/post-1?highlightComment=comment-1&ancestors=root-1'
      )
    )
  })

  it('navigates to the friend requests page for a FriendRequest notification', async () => {
    server.use(
      http.post(
        `${BASE_URL}/api/notifications/notif-2/read`,
        () => new HttpResponse(null, { status: 200 })
      )
    )

    renderWithProviders(
      <>
        <NotificationItem
          notification={makeNotification({
            id: 'notif-2',
            type: 'FriendRequest',
            sourceType: 'User',
            sourceId: 'user-1',
          })}
        />
        <LocationDisplay />
      </>
    )

    await userEvent.click(screen.getByRole('button'))

    await waitFor(() =>
      expect(screen.getByTestId('location').textContent).toBe('/friends/requests')
    )
  })

  it('does not navigate anywhere for a ModAction notification', async () => {
    renderWithProviders(
      <>
        <NotificationItem
          notification={makeNotification({
            id: 'notif-3',
            type: 'ModAction',
            sourceType: 'ModerationAction',
            sourceId: 'action-1',
          })}
        />
        <LocationDisplay />
      </>
    )

    await userEvent.click(screen.getByRole('button'))

    expect(screen.getByTestId('location').textContent).toBe('/')
  })
})
