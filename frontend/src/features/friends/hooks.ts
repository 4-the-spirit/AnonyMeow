import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import * as api from './api'
import { friendKeys } from './queryKeys'

export function useFriends(username: string) {
  return useQuery({
    queryKey: friendKeys.list(username),
    queryFn: () => api.listFriends(username),
    enabled: !!username,
  })
}

export function useMyFriendRequests() {
  const { status } = useAuth()
  return useQuery({
    queryKey: friendKeys.myRequests,
    queryFn: api.listMyFriendRequests,
    enabled: status === 'ready',
  })
}

/**
 * A request/accept/decline/remove call changes both the other user's friend graph and (for
 * accept/remove) the current user's own — invalidate both plus the shared pending-requests list
 * rather than trying to patch the cache by hand.
 */
function useInvalidateFriendState(otherUsername: string) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  return () => {
    queryClient.invalidateQueries({ queryKey: friendKeys.myRequests })
    queryClient.invalidateQueries({ queryKey: friendKeys.list(otherUsername) })
    if (user) {
      queryClient.invalidateQueries({ queryKey: friendKeys.list(user.username) })
    }
  }
}

export function useSendFriendRequest(username: string) {
  const invalidate = useInvalidateFriendState(username)
  return useMutation({
    mutationFn: () => api.sendFriendRequest(username),
    onSuccess: invalidate,
  })
}

export function useCancelFriendRequest(username: string) {
  const invalidate = useInvalidateFriendState(username)
  return useMutation({
    mutationFn: () => api.cancelFriendRequest(username),
    onSuccess: invalidate,
  })
}

export function useAcceptFriendRequest(username: string) {
  const invalidate = useInvalidateFriendState(username)
  return useMutation({
    mutationFn: () => api.acceptFriendRequest(username),
    onSuccess: invalidate,
  })
}

export function useDeclineFriendRequest(username: string) {
  const invalidate = useInvalidateFriendState(username)
  return useMutation({
    mutationFn: () => api.declineFriendRequest(username),
    onSuccess: invalidate,
  })
}

export function useRemoveFriend(username: string) {
  const invalidate = useInvalidateFriendState(username)
  return useMutation({
    mutationFn: () => api.removeFriend(username),
    onSuccess: invalidate,
  })
}
