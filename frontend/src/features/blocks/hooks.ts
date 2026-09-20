import { useMutation } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import * as api from './api'

const BLOCKED_CACHE_KEY_PREFIX = 'anonymeow.blockedUsers.'

/**
 * Non-authoritative, localStorage-backed cache of who the current user has blocked — there's no
 * "am I blocking this user" or "list my blocks" endpoint, so this only exists to keep the
 * Block/Unblock button label sane across reloads (same shape as
 * features/communities/hooks.ts's useJoinedCommunitiesCache for Join/Leave). Real enforcement
 * always happens server-side (403 at conversation-create/message-send) regardless of what this
 * cache says.
 */
export function useBlockedUsersCache() {
  const { user } = useAuth()
  const storageKey = user ? `${BLOCKED_CACHE_KEY_PREFIX}${user.username}` : null

  function readSet(): Set<string> {
    if (!storageKey) return new Set()
    try {
      const raw = localStorage.getItem(storageKey)
      return new Set(raw ? (JSON.parse(raw) as string[]) : [])
    } catch {
      return new Set()
    }
  }

  function isBlocked(username: string) {
    return readSet().has(username)
  }

  function markBlocked(username: string, blocked: boolean) {
    if (!storageKey) return
    const set = readSet()
    if (blocked) {
      set.add(username)
    } else {
      set.delete(username)
    }
    localStorage.setItem(storageKey, JSON.stringify([...set]))
  }

  return { isBlocked, markBlocked }
}

export function useBlockUser(username: string) {
  const { markBlocked } = useBlockedUsersCache()
  return useMutation({
    mutationFn: () => api.blockUser(username),
    onSuccess: () => markBlocked(username, true),
  })
}

export function useUnblockUser(username: string) {
  const { markBlocked } = useBlockedUsersCache()
  return useMutation({
    mutationFn: () => api.unblockUser(username),
    onSuccess: () => markBlocked(username, false),
  })
}
