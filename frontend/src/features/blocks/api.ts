import { apiFetch } from '@/lib/api/client'

export interface BlockResponse {
  blockedUsername: string
  createdAtUtc: string
}

export function blockUser(username: string) {
  return apiFetch<BlockResponse>(`/api/users/${encodeURIComponent(username)}/block`, {
    method: 'POST',
  })
}

export function unblockUser(username: string) {
  return apiFetch<void>(`/api/users/${encodeURIComponent(username)}/block`, {
    method: 'DELETE',
  })
}
