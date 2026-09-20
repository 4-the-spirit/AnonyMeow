import { apiFetch } from '@/lib/api/client'
import type { FriendRequestResponse, FriendRequestsResponse, FriendResponse } from './types'

export function sendFriendRequest(username: string) {
  return apiFetch<FriendRequestResponse>(
    `/api/users/${encodeURIComponent(username)}/friend-requests`,
    { method: 'POST' }
  )
}

export function cancelFriendRequest(username: string) {
  return apiFetch<void>(
    `/api/users/${encodeURIComponent(username)}/friend-requests`,
    { method: 'DELETE' }
  )
}

export function acceptFriendRequest(username: string) {
  return apiFetch<FriendRequestResponse>(
    `/api/users/${encodeURIComponent(username)}/friend-requests/accept`,
    { method: 'POST' }
  )
}

export function declineFriendRequest(username: string) {
  return apiFetch<FriendRequestResponse>(
    `/api/users/${encodeURIComponent(username)}/friend-requests/decline`,
    { method: 'POST' }
  )
}

export function removeFriend(username: string) {
  return apiFetch<void>(`/api/friends/${encodeURIComponent(username)}`, { method: 'DELETE' })
}

export function listFriends(username: string) {
  return apiFetch<FriendResponse[]>(`/api/users/${encodeURIComponent(username)}/friends`)
}

export function listMyFriendRequests() {
  return apiFetch<FriendRequestsResponse>('/api/users/me/friend-requests')
}
