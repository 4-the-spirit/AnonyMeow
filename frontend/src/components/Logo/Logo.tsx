import { useTranslation } from 'react-i18next'
import { MascotVideo } from '@/components/MascotVideo/MascotVideo'
import { cn } from '@/lib/utils'

interface LogoProps {
  size?: number
  className?: string
}

/** Site logo: the waving mascot video, autoplaying muted and on loop. */
export function Logo({ size = 40, className }: LogoProps) {
  const { t } = useTranslation('common')

  return (
    <MascotVideo
      src="/logo/mascot-waving.mp4"
      aria-label={t('appName')}
      width={size}
      height={size}
      className={cn('inline-block shrink-0 object-contain', className)}
      style={{ width: size, height: size }}
    />
  )
}
