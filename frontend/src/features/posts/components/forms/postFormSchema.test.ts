import { describe, expect, it } from 'vitest'
import { buildPostFormSchema, type PostFormValues } from './postFormSchema'

const t = (key: string) => key

const baseValues: PostFormValues = {
  title: 'A title',
  bodyMarkdown: '',
  imageUrls: [],
  hasPoll: false,
  pollOptions: [],
  flairId: 'flair-1',
}

describe('buildPostFormSchema', () => {
  const schema = buildPostFormSchema(t)

  it('rejects a title-only post with no body, images, or poll', () => {
    const result = schema.safeParse(baseValues)
    expect(result.success).toBe(false)
  })

  it('accepts a body-only post', () => {
    const result = schema.safeParse({ ...baseValues, bodyMarkdown: 'Hello world' })
    expect(result.success).toBe(true)
  })

  it('rejects a post with no tag selected', () => {
    const result = schema.safeParse({ ...baseValues, bodyMarkdown: 'Hello world', flairId: '' })
    expect(result.success).toBe(false)
  })

  it('accepts an images-only post', () => {
    const result = schema.safeParse({ ...baseValues, imageUrls: ['https://blob.example/a.png'] })
    expect(result.success).toBe(true)
  })

  it('rejects hasPoll with fewer than 2 options', () => {
    const result = schema.safeParse({ ...baseValues, hasPoll: true, pollOptions: ['OnlyOne'] })
    expect(result.success).toBe(false)
  })

  it('accepts hasPoll with 2 non-empty options', () => {
    const result = schema.safeParse({ ...baseValues, hasPoll: true, pollOptions: ['A', 'B'] })
    expect(result.success).toBe(true)
  })

  it('accepts all attachments combined', () => {
    const result = schema.safeParse({
      title: 'A title',
      bodyMarkdown: 'Body',
      imageUrls: ['https://blob.example/a.png'],
      hasPoll: true,
      pollOptions: ['A', 'B'],
      flairId: 'flair-1',
    })
    expect(result.success).toBe(true)
  })
})
