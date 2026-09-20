import { z } from 'zod'

/**
 * Factories rather than plain schema constants: the error messages need translated text, and a
 * zod schema built at module scope can't call the `t()` hook — callers build these inside the
 * component (typically via `useMemo`) using the `common` namespace's `validation.*` keys.
 */

/** Mirrors UsernameReservationService.cs: ^[a-z0-9_]{3,20}$ */
export function usernameSchema(t: (key: string) => string) {
  return z
    .string()
    .min(3, t('validation.usernameLength'))
    .max(20, t('validation.usernameLength'))
    .regex(/^[a-z0-9_]+$/, t('validation.usernameFormat'))
}

/** Mirrors CommunityEndpoints.cs: ^[א-ת0-9_ -]{3,30}$ */
export function communityNameSchema(t: (key: string) => string) {
  return z
    .string()
    .min(3, t('validation.communityNameLength'))
    .max(30, t('validation.communityNameLength'))
    .regex(/^[א-ת0-9_ -]+$/, t('validation.communityNameFormat'))
}

/** Format-only check — the backend/CIAM tenant is the actual source of truth for whether an
 * email is valid/registered. Empty input already fails z.email()'s format check, so there's no
 * separate "required" message. */
export function emailSchema(t: (key: string) => string) {
  return z.email(t('validation.emailFormat'))
}

/** Length-only check, for entering an *existing* password (sign-in): the account's real password
 * may predate today's complexity rule or otherwise not match it, so only length is worth
 * rejecting on client-side before even trying the server. */
export function passwordSchema(t: (key: string) => string) {
  return z
    .string()
    .min(8, t('validation.passwordLength'))
    .max(256, t('validation.passwordLength'))
}

const PASSWORD_CHARACTER_CLASSES = [/[a-z]/, /[A-Z]/, /[0-9]/, /[^A-Za-z0-9]/]

/** Mirrors Microsoft Entra External ID's password policy for *new* passwords (sign-up, password
 * reset): 8-256 characters, and at least 3 of {lowercase, uppercase, digit, symbol} — see
 * https://learn.microsoft.com/entra/identity/authentication/concept-sspr-policy. */
export function newPasswordSchema(t: (key: string) => string) {
  return passwordSchema(t).refine(
    (value) => PASSWORD_CHARACTER_CLASSES.filter((charClass) => charClass.test(value)).length >= 3,
    t('validation.passwordComplexity')
  )
}
