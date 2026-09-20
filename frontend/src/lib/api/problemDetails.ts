import type { UseFormSetError, FieldValues, Path } from 'react-hook-form'

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

export interface ValidationProblemDetails extends ProblemDetails {
  errors?: Record<string, string[]>
  /** Present only on the PII-detection 422 (Common/Exceptions/PiiDetectedException). */
  detectedCategories?: string[]
}

export class ApiError extends Error {
  status: number
  title?: string
  detail?: string
  errors?: Record<string, string[]>
  detectedCategories?: string[]

  constructor(status: number, body: ValidationProblemDetails | null) {
    super(body?.detail ?? body?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.title = body?.title
    this.detail = body?.detail
    this.errors = body?.errors
    this.detectedCategories = body?.detectedCategories
  }
}

/**
 * Non-null only for the PII-detection block (422 with a populated detectedCategories list) —
 * the same 422 status is reused by unrelated validation failures elsewhere, so status alone
 * isn't a safe signal.
 */
export function getPiiCategories(error: unknown): string[] | null {
  if (!(error instanceof ApiError) || error.status !== 422 || !error.detectedCategories?.length) {
    return null
  }
  return error.detectedCategories
}

export function describePiiBlock(categories: string[]): string {
  return `Your submission looks like it contains personal information (${categories.join(', ')}) and wasn't published. Please remove it and try again.`
}

/**
 * Maps the backend's per-field validation errors onto react-hook-form fields, so
 * server- and client-side validation render identically.
 */
export function applyServerErrors<TFieldValues extends FieldValues>(
  setError: UseFormSetError<TFieldValues>,
  error: unknown
) {
  if (!(error instanceof ApiError) || !error.errors) {
    return false
  }

  for (const [field, messages] of Object.entries(error.errors)) {
    setError(field as Path<TFieldValues>, {
      type: 'server',
      message: messages[0],
    })
  }

  return true
}
