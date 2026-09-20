import { describe, expect, it } from 'vitest'
import { newPasswordSchema, passwordSchema } from './validation'

const t = (key: string) => key

describe('passwordSchema', () => {
  const schema = passwordSchema(t)

  it('accepts any 8-256 character password regardless of complexity', () => {
    expect(schema.safeParse('all-lowercase').success).toBe(true)
  })

  it('rejects passwords shorter than 8 characters', () => {
    expect(schema.safeParse('short1').success).toBe(false)
  })
})

describe('newPasswordSchema', () => {
  const schema = newPasswordSchema(t)

  it('accepts a password combining 3 of 4 character classes', () => {
    expect(schema.safeParse('Passw0rd').success).toBe(true)
  })

  it('accepts a password combining all 4 character classes', () => {
    expect(schema.safeParse('P@ssw0rd123').success).toBe(true)
  })

  it('rejects a password with only lowercase letters', () => {
    expect(schema.safeParse('wrongpassword').success).toBe(false)
  })

  it('rejects a password combining only 2 of 4 character classes', () => {
    expect(schema.safeParse('lowercase123').success).toBe(false)
  })

  it('rejects passwords shorter than 8 characters even if complex', () => {
    expect(schema.safeParse('P@ss1').success).toBe(false)
  })
})
