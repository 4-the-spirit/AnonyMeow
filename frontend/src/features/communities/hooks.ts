import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import * as api from './api'
import { communityKeys } from './queryKeys'
import type { CreateCommunityRequest, UpdateCommunityRequest } from './types'

export function useCommunities(search: string, page: number) {
  return useQuery({
    queryKey: communityKeys.list(search, page),
    queryFn: () => api.listCommunities(search, page),
  })
}

export function useCommunity(name: string) {
  return useQuery({
    queryKey: communityKeys.detail(name),
    queryFn: () => api.getCommunity(name),
    enabled: !!name,
  })
}

export function useCreateCommunity() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateCommunityRequest) => api.createCommunity(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['communities', 'list'] })
    },
  })
}

export function useUpdateCommunity(name: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateCommunityRequest) => api.updateCommunity(name, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: communityKeys.detail(name) })
    },
  })
}

export function useModerators(name: string) {
  return useQuery({
    queryKey: communityKeys.moderators(name),
    queryFn: () => api.listModerators(name),
    enabled: !!name,
  })
}

export function useMembers(name: string, search: string, page: number) {
  const debouncedSearch = useDebouncedValue(search, 300)
  return useQuery({
    queryKey: communityKeys.members(name, debouncedSearch, page),
    queryFn: () => api.listMembers(name, debouncedSearch, page),
    enabled: !!name,
  })
}

/** Promote/demote both invalidate the moderators list and every members-list page/search so
 * CommunityMembersPage's role badges and CommunityModeratorsPage stay in sync. */
export function useCommunityModActions(name: string) {
  const queryClient = useQueryClient()
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: communityKeys.moderators(name) })
    queryClient.invalidateQueries({ queryKey: ['communities', name, 'members'] })
  }

  const promote = useMutation({
    mutationFn: (username: string) => api.promoteModerator(name, username),
    onSuccess: invalidate,
  })
  const demote = useMutation({
    mutationFn: (username: string) => api.demoteModerator(name, username),
    onSuccess: invalidate,
  })

  return { promote, demote }
}

/**
 * No `isModerator` field exists on CommunityResponse and there's no "my roles" endpoint —
 * moderator status is derived by checking the current username against the moderators list.
 */
export function useIsModerator(name: string) {
  const { user } = useAuth()
  const moderators = useModerators(name)
  const isModerator =
    !!user && !!moderators.data?.some((mod) => mod.username === user.username)
  return { isModerator, isLoading: moderators.isLoading }
}

export function useJoinCommunity(name: string) {
  const queryClient = useQueryClient()
  const { markJoined } = useJoinedCommunitiesCache()
  return useMutation({
    mutationFn: () => api.joinCommunity(name),
    onSuccess: () => {
      markJoined(name, true)
      queryClient.invalidateQueries({ queryKey: communityKeys.detail(name) })
    },
  })
}

export function useLeaveCommunity(name: string) {
  const queryClient = useQueryClient()
  const { markJoined } = useJoinedCommunitiesCache()
  return useMutation({
    mutationFn: () => api.leaveCommunity(name),
    onSuccess: () => {
      markJoined(name, false)
      queryClient.invalidateQueries({ queryKey: communityKeys.detail(name) })
    },
  })
}

const JOINED_CACHE_KEY_PREFIX = 'anonymeow.joinedCommunities.'

/**
 * Non-authoritative, localStorage-backed cache of which communities the current user has
 * joined — CommunityResponse has no isMember field and there's no "my communities" endpoint,
 * so this only exists to keep the Join/Leave button label sane across reloads. It is never
 * treated as a source of truth for anything else.
 */
export function useJoinedCommunitiesCache() {
  const { user } = useAuth()
  const storageKey = user ? `${JOINED_CACHE_KEY_PREFIX}${user.username}` : null

  function readSet(): Set<string> {
    if (!storageKey) return new Set()
    try {
      const raw = localStorage.getItem(storageKey)
      return new Set(raw ? (JSON.parse(raw) as string[]) : [])
    } catch {
      return new Set()
    }
  }

  function isJoined(communityName: string) {
    return readSet().has(communityName)
  }

  function markJoined(communityName: string, joined: boolean) {
    if (!storageKey) return
    const set = readSet()
    if (joined) {
      set.add(communityName)
    } else {
      set.delete(communityName)
    }
    localStorage.setItem(storageKey, JSON.stringify([...set]))
  }

  return { isJoined, markJoined }
}
