import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { queryClient } from './queryClient'
import { router } from './router'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { LanguageProvider } from '@/components/LanguageProvider/LanguageProvider'
import { TooltipProvider } from '@/components/ui/tooltip'
import { Toaster } from '@/components/ui/sonner'

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <LanguageProvider>
        <AuthProvider>
          <TooltipProvider>
            <RouterProvider router={router} />
            {/* Mounted above the router (not inside AppLayout) so toasts still render on
             * routes outside it, e.g. /signin and /signup. */}
            <Toaster />
          </TooltipProvider>
        </AuthProvider>
      </LanguageProvider>
    </QueryClientProvider>
  )
}
