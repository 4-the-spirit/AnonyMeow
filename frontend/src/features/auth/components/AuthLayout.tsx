import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Card, CardContent } from '@/components/ui/card'
import { MascotVideo } from '@/components/MascotVideo/MascotVideo'

// White-background mascot videos shown on the sign-in/sign-up/forgot-password auth pages —
// sourced from backend/Resources/videos/white, copied into public/white/. Add new filenames here
// as more white-background videos land in that folder.
const AUTH_MASCOT_VIDEOS = [
  'mascot-coffee-chat.mp4',
  'mascot-detective.mp4',
  'mascot-laptop.mp4',
  'mascot-running.mp4',
  'mascot-scholar.mp4',
  'mascot-shrug.mp4',
  'mascot-skater.mp4',
  'mascot-thinking.mp4',
  'mascot-waving.mp4',
  'mascot-winter.mp4',
  'mascot-wizard.mp4',
]

function pickRandomAuthMascotVideo() {
  return AUTH_MASCOT_VIDEOS[Math.floor(Math.random() * AUTH_MASCOT_VIDEOS.length)]
}

/** Shared branding chrome for the sign-in/sign-up entry points (the local-dev AuthPage, the real
 * EmailAuthPage, and ForgotPasswordPage) — a full-viewport gradient backdrop framing a centered
 * card, so every auth page gets identical, more inviting branding around whatever form content
 * it renders inside. */
export function AuthLayout({ children }: { children: ReactNode }) {
  const { t: tCommon } = useTranslation('common')
  const { t: tAuth } = useTranslation('auth')
  // Picked once per mount, so each visit to a sign-in/sign-up/forgot-password page shows a
  // different mascot video.
  const [mascotVideo] = useState(pickRandomAuthMascotVideo)

  return (
    <div className="via-background flex min-h-screen items-center justify-center bg-gradient-to-br from-primary/15 to-accent/25 p-6">
      <Card className="w-full max-w-md shadow-lg">
        <CardContent className="flex flex-col gap-6 pt-2">
          <div className="flex flex-col items-center gap-2 text-center">
            <MascotVideo
              key={mascotVideo}
              src={`/white/${mascotVideo}`}
              aria-label={tCommon('appName')}
              className="h-68 w-68 max-w-full object-cover"
            />
            <span className="font-logo text-4xl font-extrabold tracking-tight text-black">
              {tCommon('appName')}
            </span>
            <p className="text-muted-foreground text-sm">{tAuth('tagline')}</p>
          </div>
          {children}
        </CardContent>
      </Card>
    </div>
  )
}
