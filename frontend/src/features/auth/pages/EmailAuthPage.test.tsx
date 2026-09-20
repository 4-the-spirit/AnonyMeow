import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useLocation } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { EmailAuthPage } from './EmailAuthPage'

const BASE_URL = 'https://test.local'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

function renderPage(defaultTab: 'signIn' | 'signUp' = 'signIn') {
  return renderWithProviders(
    <>
      <EmailAuthPage defaultTab={defaultTab} />
      <LocationDisplay />
    </>,
    { route: defaultTab === 'signIn' ? '/signin' : '/signup' }
  )
}

describe('EmailAuthPage', () => {
  it('signs in and navigates home on valid credentials', async () => {
    server.use(
      http.post(`${BASE_URL}/api/auth/native/signin/start`, () =>
        HttpResponse.json({
          accessToken: 'access-1',
          idToken: 'id-1',
          refreshToken: 'refresh-1',
          oid: 'oid-1',
          expiresIn: 3600,
        })
      )
    )

    renderPage('signIn')

    await userEvent.type(screen.getByLabelText(/email/i), 'jane@example.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'P@ssw0rd123')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/'))
  })

  it('shows a field error and stays on the page when the password is wrong', async () => {
    server.use(
      http.post(`${BASE_URL}/api/auth/native/signin/start`, () =>
        HttpResponse.json({ title: 'Invalid Credentials', detail: 'Incorrect password.' }, { status: 401 })
      )
    )

    renderPage('signIn')

    await userEvent.type(screen.getByLabelText(/email/i), 'jane@example.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong-password')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(screen.getByRole('button', { name: /sign in/i })).not.toBeDisabled())
    expect(screen.getByTestId('location')).toHaveTextContent('/signin')
  })

  it('walks sign-up through the email-verification step to a finished session', async () => {
    server.use(
      http.post(`${BASE_URL}/api/auth/native/signup/start`, () =>
        HttpResponse.json({
          nextStep: 'verifyEmail',
          otpChallenge: { continuationToken: 'cont-token', codeLength: 8, maskedEmail: 'j***@example.com' },
          tokens: null,
        })
      ),
      http.post(`${BASE_URL}/api/auth/native/signup/verify-email`, async ({ request }) => {
        const body = (await request.json()) as { continuationToken: string; code: string }
        expect(body.continuationToken).toBe('cont-token')
        expect(body.code).toBe('12345678')
        return HttpResponse.json({
          accessToken: 'access-2',
          idToken: 'id-2',
          refreshToken: 'refresh-2',
          oid: 'oid-2',
          expiresIn: 3600,
        })
      })
    )

    renderPage('signUp')

    await userEvent.type(screen.getByLabelText(/email/i), 'newuser@example.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'P@ssw0rd123')
    await userEvent.click(screen.getByRole('button', { name: /create account/i }))

    await screen.findByText(/j\*\*\*@example\.com/)

    await userEvent.type(screen.getByLabelText(/verification code/i), '12345678')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/'))
  })

  it('shows a reset-password hint when sign-up hits an existing, unmatched account', async () => {
    server.use(
      http.post(`${BASE_URL}/api/auth/native/signup/start`, () =>
        HttpResponse.json(
          {
            title: 'Email Already Registered',
            detail: "An account with the email 'jane@example.com' already exists.",
            errors: { email: ['An account with this email already exists.'] },
          },
          { status: 409 }
        )
      )
    )

    renderPage('signUp')

    await userEvent.type(screen.getByLabelText(/email/i), 'jane@example.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'P@ssw0rd123')
    await userEvent.click(screen.getByRole('button', { name: /create account/i }))

    const resetLink = await screen.findByRole('link', { name: /reset/i })
    expect(resetLink).toHaveAttribute('href', '/forgot-password')
    expect(screen.getByTestId('location')).toHaveTextContent('/signup')
  })
})
