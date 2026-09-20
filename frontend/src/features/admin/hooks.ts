import { useMutation, useQuery } from '@tanstack/react-query'
import type { ReportStatus } from '@/features/moderation/types'
import * as api from './api'
import { adminKeys } from './queryKeys'
import type { CreatePlatformRestrictionRequest, SpamFlagStatus } from './types'

export function useAllReports(status: ReportStatus | 'all', page: number) {
  return useQuery({
    queryKey: adminKeys.reports(status, page),
    queryFn: () => api.listAllReports(status, page),
  })
}

export function useSpamFlags(status: SpamFlagStatus | 'all', page: number) {
  return useQuery({
    queryKey: adminKeys.spamFlags(status, page),
    queryFn: () => api.listSpamFlags(status, page),
  })
}

export function useAuditLog(page: number) {
  return useQuery({
    queryKey: adminKeys.auditLog(page),
    queryFn: () => api.listAuditLog(page),
  })
}

export function useRestrictUser() {
  return useMutation({
    mutationFn: ({
      username,
      body,
    }: {
      username: string
      body: CreatePlatformRestrictionRequest
    }) => api.restrictUser(username, body),
  })
}

export function useLiftRestriction() {
  return useMutation({
    mutationFn: (username: string) => api.liftRestriction(username),
  })
}
