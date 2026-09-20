import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { postKeys } from '@/features/posts/queryKeys'
import * as api from './api'
import { moderationKeys } from './queryKeys'
import type {
  CreateBanRequest,
  CreateReportRequest,
  ReportStatus,
  ResolveReportRequest,
} from './types'

export function useReportPost(postId: string) {
  return useMutation({
    mutationFn: (body: CreateReportRequest) => api.reportPost(postId, body),
  })
}

export function useReportComment(commentId: string) {
  return useMutation({
    mutationFn: (body: CreateReportRequest) => api.reportComment(commentId, body),
  })
}

export function useReportMessage(messageId: string) {
  return useMutation({
    mutationFn: (body: CreateReportRequest) => api.reportMessage(messageId, body),
  })
}

export function useReports(communityName: string, status: ReportStatus | 'all', page: number) {
  return useQuery({
    queryKey: moderationKeys.reports(communityName, status, page),
    queryFn: () => api.listReports(communityName, status, page),
    enabled: !!communityName,
  })
}

export function useResolveReport(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ reportId, body }: { reportId: string; body: ResolveReportRequest }) =>
      api.resolveReport(communityName, reportId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['moderation', communityName, 'reports'] })
    },
  })
}

/** Pin/lock/remove all invalidate the post detail so PostDetailPage reflects the new state. */
export function usePostModActions(postId: string) {
  const queryClient = useQueryClient()
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: postKeys.detail(postId) })

  const pin = useMutation({ mutationFn: () => api.pinPost(postId), onSuccess: invalidate })
  const unpin = useMutation({ mutationFn: () => api.unpinPost(postId), onSuccess: invalidate })
  const lock = useMutation({ mutationFn: () => api.lockPost(postId), onSuccess: invalidate })
  const unlock = useMutation({ mutationFn: () => api.unlockPost(postId), onSuccess: invalidate })
  const remove = useMutation({
    mutationFn: (reason?: string) => api.removePost(postId, reason),
    onSuccess: invalidate,
  })

  return { pin, unpin, lock, unlock, remove }
}

/** Mirrors usePostModActions' remove — invalidates every comments query the same way
 * useDeleteComment does, since a moderator-removed comment needs to disappear/update the same way
 * an author-deleted one does. */
export function useCommentModActions(commentId: string) {
  const queryClient = useQueryClient()
  const remove = useMutation({
    mutationFn: (reason?: string) => api.removeComment(commentId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments'] })
    },
  })

  return { remove }
}

export function useBans(communityName: string) {
  return useQuery({
    queryKey: moderationKeys.bans(communityName),
    queryFn: () => api.listBans(communityName),
    enabled: !!communityName,
  })
}

export function useCreateBan(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateBanRequest) => api.createBan(communityName, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: moderationKeys.bans(communityName) })
    },
  })
}

export function useRevokeBan(communityName: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (username: string) => api.revokeBan(communityName, username),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: moderationKeys.bans(communityName) })
    },
  })
}
