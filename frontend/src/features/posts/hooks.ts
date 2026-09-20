import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from './api'
import { postKeys } from './queryKeys'
import type { CastPollVoteRequest, CreatePostRequest, SortOrder, UpdatePostRequest } from './types'

export function useCommunityPosts(communityName: string, sort: SortOrder, page: number) {
  return useQuery({
    queryKey: postKeys.community(communityName, sort, page),
    queryFn: () => api.listCommunityPosts(communityName, sort, page),
    enabled: !!communityName,
  })
}

export function usePost(id: string) {
  return useQuery({
    queryKey: postKeys.detail(id),
    queryFn: () => api.getPost(id),
    enabled: !!id,
  })
}

export function useCreatePost(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreatePostRequest) => api.createPost(communityName, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['posts', 'community', communityName] })
    },
  })
}

export function useUpdatePost(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdatePostRequest) => api.updatePost(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: postKeys.detail(id) })
    },
  })
}

export function useUpdatePostFlair(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (flairId: string) => api.updatePostFlair(id, flairId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: postKeys.detail(id) })
    },
  })
}

export function useDeletePost(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.deletePost(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: postKeys.detail(id) })
    },
  })
}

export function useCastPollVote(postId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CastPollVoteRequest) => api.castPollVote(postId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: postKeys.detail(postId) })
    },
  })
}
