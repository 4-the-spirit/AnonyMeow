import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from './api'
import { flairKeys } from './queryKeys'
import type { CreateFlairRequest } from './types'

export function useFlairs(communityName: string) {
  return useQuery({
    queryKey: flairKeys.list(communityName),
    queryFn: () => api.listFlairs(communityName),
    enabled: !!communityName,
  })
}

export function useCreateFlair(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateFlairRequest) => api.createFlair(communityName, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: flairKeys.list(communityName) })
    },
  })
}

export function useUpdateFlair(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: CreateFlairRequest }) => api.updateFlair(communityName, id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: flairKeys.list(communityName) })
    },
  })
}

export function useDeleteFlair(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.deleteFlair(communityName, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: flairKeys.list(communityName) })
    },
  })
}
