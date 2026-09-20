import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import * as api from './api'
import { savedPostKeys } from './queryKeys'

const SAVED_CACHE_KEY_PREFIX = 'anonymeow.savedPosts.'

/**
 * PostResponse never exposes whether the viewer has saved a given post — same documented gap as
 * VoteWidget's "did I vote" and communities' "am I joined" — so this is a non-authoritative,
 * localStorage-backed cache (same shape as useJoinedCommunitiesCache/useBlockedUsersCache) that
 * keeps the Save/Unsave button label sane across reloads. GET /api/users/me/saved-posts IS
 * authoritative for the posts it returns, so SavedPostsPage seeds this cache from it.
 */
export function useSavedPostsCache() {
  const { user } = useAuth()
  const storageKey = user ? `${SAVED_CACHE_KEY_PREFIX}${user.username}` : null

  function readSet(): Set<string> {
    if (!storageKey) return new Set()
    try {
      const raw = localStorage.getItem(storageKey)
      return new Set(raw ? (JSON.parse(raw) as string[]) : [])
    } catch {
      return new Set()
    }
  }

  function isSaved(postId: string) {
    return readSet().has(postId)
  }

  function markSaved(postId: string, saved: boolean) {
    if (!storageKey) return
    const set = readSet()
    if (saved) {
      set.add(postId)
    } else {
      set.delete(postId)
    }
    localStorage.setItem(storageKey, JSON.stringify([...set]))
  }

  return { isSaved, markSaved }
}

export function useSavePost(postId: string) {
  const { markSaved } = useSavedPostsCache()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.savePost(postId),
    onSuccess: () => {
      markSaved(postId, true)
      queryClient.invalidateQueries({ queryKey: savedPostKeys.all })
    },
  })
}

export function useUnsavePost(postId: string) {
  const { markSaved } = useSavedPostsCache()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.unsavePost(postId),
    onSuccess: () => {
      markSaved(postId, false)
      queryClient.invalidateQueries({ queryKey: savedPostKeys.all })
    },
  })
}

export function useSavedPosts(page: number) {
  return useQuery({
    queryKey: savedPostKeys.list(page),
    queryFn: () => api.listSavedPosts(page),
  })
}
