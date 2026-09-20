import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useLocation } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { ForgotPasswordPage } from './ForgotPasswordPage'

const BASE_URL = 'https://test.local'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

function renderPage() {
  return renderWithProviders(
    <>
      <ForgotPasswordPage />
      <LocationDisplay />
    </>,
    { route: '/forgot-password' }
  )
}

describe('ForgotPasswordPage', () => {
  it('walks email -> code -> new password to a finished session', async () => {
    server.use(
      http.post(`${BASE_URL}/api/auth/native/password-reset/start`, () =>
        HttpResponse.json({ continuationToken: 'start-token', codeLength: 8, maskedEmail: 'j***@example.com' })
      ),
      http.post(`${BASE_URL}/api/auth/native/password-reset/verify`, async ({ request }) => {
        const body = (await request.json()) as { continuationToken: string; code: string }
        expect(body.continuationToken).toBe('start-token')
        expect(body.code).toBe('12345678')
        return HttpResponse.json({ continuationToken: 'verified-token' })
      }),
      http.post(`${BASE_URL}/api/auth/native/password-reset/complete`, async ({ request }) => {
        const body = (await request.json()) as { continuationToken: string; newPassword: string }
        expect(body.continuationToken).toBe('verified-token')
        expect(body.newPassword).toBe('NewP@ssw0rd123')
        return HttpResponse.json({
          accessToken: 'access-1',
          idToken: 'id-1',
          refreshToken: 'refresh-1',
          oid: 'oid-1',
          expiresIn: 3600,
        })
      })
    )

    renderPage()

    await userEvent.type(screen.getByLabelText(/email/i), 'jane@example.com')
    await userEvent.click(screen.getByRole('button', { name: /send code/i }))

    await screen.findByText(/j\*\*\*@example\.com/)
    await userEvent.type(screen.getByLabelText(/verification code/i), '12345678')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await screen.findByLabelText(/new password/i)
    await userEvent.type(screen.getByLabelText(/new password/i), 'NewP@ssw0rd123')
    await userEvent.click(screen.getByRole('button', { name: /reset password/i }))

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/'))
  })

  it('shows an error and stays on the email step for an unknown account', async () => {
    server.use(
      http.post(`${BASE_URL}/api/auth/native/password-reset/start`, () =>
        HttpResponse.json({ title: 'Account Not Found', detail: 'No account found.' }, { status: 404 })
      )
    )

    renderPage()

    await userEvent.type(screen.getByLabelText(/email/i), 'ghost@example.com')
    await userEvent.click(screen.getByRole('button', { name: /send code/i }))

    await waitFor(() => expect(screen.getByRole('button', { name: /send code/i })).not.toBeDisabled())
    expect(screen.getByTestId('location')).toHaveTextContent('/forgot-password')
  })
})
