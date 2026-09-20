import type { ReactElement, ReactNode } from 'react'
import { render } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { TooltipProvider } from '@/components/ui/tooltip'
import { tokenStorage } from '@/lib/auth/tokenStorage'

export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

interface RenderOptions {
  route?: string
  withAuth?: boolean
}

export function renderWithProviders(ui: ReactElement, options: RenderOptions = {}) {
  const { route = '/', withAuth = false } = options
  const queryClient = createTestQueryClient()

  // AuthProvider reads any stored session synchronously on mount (no more auto-mint-on-load —
  // real anonymous browsing means a fresh visitor has no session at all), so it's always safe to
  // wrap: with no seeded session it just resolves to "anonymous" with no network calls fired.
  // `withAuth: true` seeds one so status reaches "ready" against the MSW-mocked /api/dev/token +
  // /api/users/me handlers.
  if (withAuth) {
    tokenStorage.set('test-token', 'test-oid')
  } else {
    tokenStorage.clear()
  }

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[route]}>
          <AuthProvider>
            <TooltipProvider>{children}</TooltipProvider>
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    )
  }

  return render(ui, { wrapper: Wrapper })
}
