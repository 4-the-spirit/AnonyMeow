import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import * as api from './api'
import { savedCommentKeys } from './queryKeys'

const SAVED_CACHE_KEY_PREFIX = 'anonymeow.savedComments.'

/**
 * CommentResponse never exposes whether the viewer has saved a given comment — same documented
 * gap as SaveButton's post cache — so this is a non-authoritative, localStorage-backed cache that
 * keeps the Save/Unsave button label sane across reloads.
 */
export function useSavedCommentsCache() {
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

  function isSaved(commentId: string) {
    return readSet().has(commentId)
  }

  function markSaved(commentId: string, saved: boolean) {
    if (!storageKey) return
    const set = readSet()
    if (saved) {
      set.add(commentId)
    } else {
      set.delete(commentId)
    }
    localStorage.setItem(storageKey, JSON.stringify([...set]))
  }

  return { isSaved, markSaved }
}

export function useSaveComment(commentId: string) {
  const { markSaved } = useSavedCommentsCache()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.saveComment(commentId),
    onSuccess: () => {
      markSaved(commentId, true)
      queryClient.invalidateQueries({ queryKey: savedCommentKeys.all })
    },
  })
}

export function useUnsaveComment(commentId: string) {
  const { markSaved } = useSavedCommentsCache()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.unsaveComment(commentId),
    onSuccess: () => {
      markSaved(commentId, false)
      queryClient.invalidateQueries({ queryKey: savedCommentKeys.all })
    },
  })
}

export function useSavedComments(page: number) {
  return useQuery({
    queryKey: savedCommentKeys.list(page),
    queryFn: () => api.listSavedComments(page),
  })
}
