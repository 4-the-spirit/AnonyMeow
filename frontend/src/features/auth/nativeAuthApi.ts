import { apiFetch } from '@/lib/api/client'

/** Mirrors the backend's Dtos/NativeAuth/AuthTokensResponse.cs. */
export interface AuthTokens {
  accessToken: string
  idToken: string
  refreshToken: string | null
  oid: string
  expiresIn: number
}

/** Mirrors Dtos/NativeAuth/OtpChallengeResponse.cs. */
export interface OtpChallenge {
  continuationToken: string
  codeLength: number | null
  maskedEmail: string | null
}

/** Mirrors Dtos/NativeAuth/SignUpStartResponse.cs — exactly one of otpChallenge/tokens is set. */
export interface SignUpStartResult {
  nextStep: 'verifyEmail' | 'completed'
  otpChallenge: OtpChallenge | null
  tokens: AuthTokens | null
}

/**
 * Thin wrappers over the backend's native-auth proxy endpoints (Endpoints/NativeAuthEndpoints.cs)
 * — that backend in turn talks to Microsoft's native authentication REST API server-to-server,
 * since that API doesn't support CORS and can't be called from the browser directly (see
 * docs/azure-ciam-setup.md). Every call here is unauthenticated (skipAuth), same as the dev-token
 * bypass endpoints.
 */
export const nativeAuthApi = {
  signUpStart(body: { email: string; password: string }) {
    return apiFetch<SignUpStartResult>('/api/auth/native/signup/start', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },

  signUpVerifyEmail(body: { continuationToken: string; code: string }) {
    return apiFetch<AuthTokens>('/api/auth/native/signup/verify-email', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },

  signInStart(body: { email: string; password: string }) {
    return apiFetch<AuthTokens>('/api/auth/native/signin/start', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },

  refresh(body: { refreshToken: string }) {
    return apiFetch<AuthTokens>('/api/auth/native/refresh', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },

  passwordResetStart(body: { email: string }) {
    return apiFetch<OtpChallenge>('/api/auth/native/password-reset/start', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },

  passwordResetVerify(body: { continuationToken: string; code: string }) {
    return apiFetch<{ continuationToken: string }>('/api/auth/native/password-reset/verify', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },

  passwordResetComplete(body: { continuationToken: string; newPassword: string }) {
    return apiFetch<AuthTokens>('/api/auth/native/password-reset/complete', {
      method: 'POST',
      skipAuth: true,
      body,
    })
  },
}
