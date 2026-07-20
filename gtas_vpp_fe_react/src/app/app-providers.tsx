import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from 'next-themes'
import type { PropsWithChildren } from 'react'
import { I18nextProvider } from 'react-i18next'

import { configureApiClient } from '@/api/api-client'
import { AppMotionProvider } from '@/app/motion-provider'
import { AuthProvider } from '@/auth/auth-context'
import { Toaster } from '@/components/ui/sonner'
import { TooltipProvider } from '@/components/ui/tooltip'
import { i18n } from '@/lib/i18n'

configureApiClient()

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 30_000,
    },
  },
})

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <ThemeProvider attribute="class" defaultTheme="light" enableSystem>
      <I18nextProvider i18n={i18n}>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <AppMotionProvider>
              <TooltipProvider delayDuration={250}>
                {children}
                <Toaster richColors closeButton />
              </TooltipProvider>
            </AppMotionProvider>
          </AuthProvider>
        </QueryClientProvider>
      </I18nextProvider>
    </ThemeProvider>
  )
}
