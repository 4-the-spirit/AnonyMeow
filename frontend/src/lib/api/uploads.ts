import { apiFetch } from './client'

export interface ImageUploadSasResponse {
  uploadUrl: string
  blobUrl: string
}

export function createImageUploadSas() {
  return apiFetch<ImageUploadSasResponse>('/api/uploads/images/sas', { method: 'POST' })
}
