import { useState } from 'react'
import { X } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api/problemDetails'
import { ImageDropzone } from './ImageDropzone'
import { useUploadImageFile } from './hooks'

interface MultiImageUploadFieldProps {
  value: string[]
  onChange: (imageUrls: string[]) => void
  maxCount: number
  maxSizeBytes: number
}

// The SAS endpoint returns 503 until Azure Blob Storage is provisioned — same graceful-failure
// caveat as ImageUploadField, which this shares upload mechanics with via useUploadImageFile.
export function MultiImageUploadField({ value, onChange, maxCount, maxSizeBytes }: MultiImageUploadFieldProps) {
  const { t } = useTranslation('common')
  const { t: tPosts } = useTranslation('posts')
  const [isUploading, setIsUploading] = useState(false)
  const { uploadFile } = useUploadImageFile()

  async function handleFile(file: File) {
    if (value.length >= maxCount) {
      toast.error(tPosts('imageFields.tooManyImagesError', { count: maxCount }))
      return
    }
    if (file.size > maxSizeBytes) {
      toast.error(tPosts('imageFields.imageTooLargeError', { count: Math.floor(maxSizeBytes / (1024 * 1024)) }))
      return
    }

    setIsUploading(true)
    try {
      const blobUrl = await uploadFile(file)
      onChange([...value, blobUrl])
      toast.success(t('imageUpload.successToast'))
    } catch (error) {
      if (error instanceof ApiError && error.status === 503) {
        toast.error(t('imageUpload.errorToastUnavailable'))
      } else {
        toast.error(t('imageUpload.errorToastFailed'))
      }
    } finally {
      setIsUploading(false)
    }
  }

  function removeImage(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  return (
    <div className="flex flex-col gap-2">
      {value.length > 0 && (
        <div className="grid grid-cols-3 gap-2">
          {value.map((url, index) => (
            <div key={url} className="relative">
              <img
                src={url}
                alt={t('imageUpload.previewAlt')}
                className="h-24 w-full rounded-lg border object-cover"
              />
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                className="absolute top-1 end-1"
                onClick={() => removeImage(index)}
                aria-label={tPosts('imageFields.removeImage', { number: index + 1 })}
              >
                <X className="size-4" />
              </Button>
            </div>
          ))}
        </div>
      )}
      {value.length < maxCount && (
        <ImageDropzone
          onFileSelected={handleFile}
          disabled={isUploading}
          label={
            isUploading
              ? t('imageUpload.uploading')
              : t('imageUpload.dropzoneCtaMulti', { count: maxCount - value.length })
          }
          hint={t('imageUpload.dropzoneHint')}
          compact
        />
      )}
    </div>
  )
}
