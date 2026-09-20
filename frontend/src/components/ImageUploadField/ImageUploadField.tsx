import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { ApiError } from '@/lib/api/problemDetails'
import { ImageDropzone } from './ImageDropzone'
import { useUploadImageFile } from './hooks'
import { MAX_IMAGE_SIZE_BYTES } from './imageLimits'

interface ImageUploadFieldProps {
  value: string | undefined
  onChange: (imageUrl: string) => void
  maxSizeBytes?: number
}

/**
 * The SAS endpoint returns 503 until Azure Blob Storage is provisioned, so today only the
 * graceful-failure path is exercisable against the real backend.
 */
export function ImageUploadField({ value, onChange, maxSizeBytes = MAX_IMAGE_SIZE_BYTES }: ImageUploadFieldProps) {
  const { t } = useTranslation('common')
  const { t: tPosts } = useTranslation('posts')
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<unknown>(null)
  const { uploadFile } = useUploadImageFile()

  async function handleFile(file: File) {
    if (file.size > maxSizeBytes) {
      toast.error(tPosts('imageFields.imageTooLargeError', { count: Math.floor(maxSizeBytes / (1024 * 1024)) }))
      return
    }

    setUploadError(null)
    setIsUploading(true)
    try {
      const blobUrl = await uploadFile(file)
      onChange(blobUrl)
      toast.success(t('imageUpload.successToast'))
    } catch (error) {
      setUploadError(error)
      if (error instanceof ApiError && error.status === 503) {
        toast.error(t('imageUpload.errorToastUnavailable'))
      } else {
        toast.error(t('imageUpload.errorToastFailed'))
      }
    } finally {
      setIsUploading(false)
    }
  }

  return (
    <div className="flex flex-col gap-2">
      {value && !isUploading && (
        <img src={value} alt={t('imageUpload.previewAlt')} className="max-h-48 rounded-lg border" />
      )}
      <ImageDropzone
        onFileSelected={handleFile}
        disabled={isUploading}
        label={isUploading ? t('imageUpload.uploading') : t(value ? 'imageUpload.replace' : 'imageUpload.dropzoneCta')}
        hint={value ? undefined : t('imageUpload.dropzoneHint')}
        compact={!!value}
      />
      {uploadError != null && (
        <ErrorState
          title={t('imageUpload.unavailableTitle')}
          description={t('imageUpload.unavailableDescription')}
          error={uploadError}
        />
      )}
    </div>
  )
}
