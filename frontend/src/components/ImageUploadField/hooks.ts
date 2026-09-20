import { useMutation } from '@tanstack/react-query'
import { createImageUploadSas } from '@/lib/api/uploads'

export function useCreateImageUploadSas() {
  return useMutation({ mutationFn: createImageUploadSas })
}

// Shared by ImageUploadField (single image) and MultiImageUploadField (posts, multiple images) —
// requests a SAS URL, then PUTs the file directly to Blob Storage (bypasses apiFetch — different
// origin, no bearer token, blob-specific headers) — and returns the resulting blob URL.
export function useUploadImageFile() {
  const createSas = useCreateImageUploadSas()

  async function uploadFile(file: File): Promise<string> {
    const sas = await createSas.mutateAsync()
    await fetch(sas.uploadUrl, {
      method: 'PUT',
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': file.type,
      },
      body: file,
    })
    return sas.blobUrl
  }

  return { uploadFile }
}
