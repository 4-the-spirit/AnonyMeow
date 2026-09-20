import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import * as api from './api'
import { userKeys } from './queryKeys'
import type { CompleteProfileRequest, UpdateProfileRequest } from './types'

export function useCompleteProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CompleteProfileRequest) => api.completeProfile(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.me })
    },
  })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateProfileRequest) => api.updateMe(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.me })
    },
  })
}

/** Debounced live-availability check, used while a username is still being typed. */
export function useUsernameAvailability(value: string, enabled: boolean) {
  const debounced = useDebouncedValue(value, 400)
  return useQuery({
    queryKey: userKeys.usernameAvailability(debounced),
    queryFn: () => api.checkUsernameAvailability(debounced),
    enabled: enabled && debounced.length >= 3,
    staleTime: 10_000,
  })
}

export function useUserProfile(username: string) {
  return useQuery({
    queryKey: userKeys.detail(username),
    queryFn: () => api.getUserByUsername(username),
    enabled: !!username,
  })
}

export function useUserPosts(username: string, page: number) {
  return useQuery({
    queryKey: userKeys.posts(username, page),
    queryFn: () => api.getUserPosts(username, page),
    enabled: !!username,
  })
}

export function useUserComments(username: string, page: number) {
  return useQuery({
    queryKey: userKeys.comments(username, page),
    queryFn: () => api.getUserComments(username, page),
    enabled: !!username,
  })
}

export function useUserCommunities(username: string, page: number) {
  return useQuery({
    queryKey: userKeys.communities(username, page),
    queryFn: () => api.getUserCommunities(username, page),
    enabled: !!username,
  })
}
