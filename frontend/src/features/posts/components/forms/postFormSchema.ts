import { z } from 'zod'
import { MAX_IMAGE_COUNT } from '@/components/ImageUploadField/imageLimits'

export interface PostFormValues {
  title: string
  bodyMarkdown?: string
  imageUrls?: string[]
  hasPoll: boolean
  pollOptions?: string[]
  flairId: string
}

/**
 * Every post can independently and optionally carry a body, images, and a poll — this mirrors
 * PostRequestValidator server-side: >=2 non-empty poll options if hasPoll, and "must have body
 * OR images OR poll" overall. A link can simply be pasted into the body as markdown (rendered
 * clickable by MarkdownBody), so there's no separate URL field/attachment on creation. A tag
 * (flairId) is always required, also mirroring PostRequestValidator.
 */
export function buildPostFormSchema(t: (key: string, options?: Record<string, unknown>) => string) {
  return z
    .object({
      title: z.string().min(1, t('validation.titleRequired')).max(300),
      bodyMarkdown: z.string().optional(),
      imageUrls: z.array(z.string()).max(MAX_IMAGE_COUNT, t('validation.tooManyImages', { count: MAX_IMAGE_COUNT })).optional(),
      hasPoll: z.boolean(),
      pollOptions: z.array(z.string()).optional(),
      flairId: z.string().min(1, t('validation.flairRequired')),
    })
    .superRefine((values, ctx) => {
      if (values.hasPoll) {
        const options = (values.pollOptions ?? []).map((o) => o.trim()).filter(Boolean)
        if (options.length < 2) {
          ctx.addIssue({
            code: 'custom',
            path: ['pollOptions'],
            message: t('validation.pollMinOptions'),
          })
        }
      }

      const hasBody = !!values.bodyMarkdown?.trim()
      const hasImages = (values.imageUrls?.length ?? 0) > 0
      if (!hasBody && !hasImages && !values.hasPoll) {
        ctx.addIssue({
          code: 'custom',
          path: ['bodyMarkdown'],
          message: t('validation.contentRequired'),
        })
      }
    })
}
