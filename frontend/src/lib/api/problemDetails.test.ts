import { describe, expect, it } from 'vitest'
import { ApiError, describePiiBlock, getPiiCategories } from './problemDetails'

describe('getPiiCategories', () => {
  it('returns the categories for a PII-blocked 422', () => {
    const error = new ApiError(422, {
      title: 'Content Blocked',
      detectedCategories: ['Email', 'Phone Number'],
    })

    expect(getPiiCategories(error)).toEqual(['Email', 'Phone Number'])
  })

  it('returns null for a 422 with no detectedCategories (e.g. an unrelated validation error)', () => {
    const error = new ApiError(422, { title: 'Invalid emoji', errors: { emoji: ['not allowed'] } })

    expect(getPiiCategories(error)).toBeNull()
  })

  it('returns null for a non-422 ApiError', () => {
    const error = new ApiError(403, { detectedCategories: ['Email'] })

    expect(getPiiCategories(error)).toBeNull()
  })

  it('returns null for a non-ApiError', () => {
    expect(getPiiCategories(new Error('boom'))).toBeNull()
  })
})

describe('describePiiBlock', () => {
  it('joins multiple categories into a readable message', () => {
    expect(describePiiBlock(['Email', 'Phone Number'])).toContain('Email, Phone Number')
  })
})
