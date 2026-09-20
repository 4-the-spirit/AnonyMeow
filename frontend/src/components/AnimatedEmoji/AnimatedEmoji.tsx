import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

/**
 * Animated PNGs from microsoft/fluentui-emoji-animated, vendored into public/emoji/ (that repo
 * stores them via Git LFS, so they aren't safe to hotlink at runtime). Keyed by the unicode
 * character so callers can keep passing the same emoji they'd otherwise render as plain text.
 */
const ANIMATED_EMOJI: Record<string, { src: string; altKey: string }> = {
  '🐱': { src: '/emoji/cat-face.png', altKey: 'emoji.catFace' },
  '👍': { src: '/emoji/thumbs-up.png', altKey: 'emoji.thumbsUp' },
  '👎': { src: '/emoji/thumbs-down.png', altKey: 'emoji.thumbsDown' },
  '❤️': { src: '/emoji/red-heart.png', altKey: 'emoji.redHeart' },
  '😂': { src: '/emoji/face-with-tears-of-joy.png', altKey: 'emoji.faceWithTearsOfJoy' },
  '😮': { src: '/emoji/face-with-open-mouth.png', altKey: 'emoji.faceWithOpenMouth' },
  '😢': { src: '/emoji/crying-face.png', altKey: 'emoji.cryingFace' },
  '🔥': { src: '/emoji/fire.png', altKey: 'emoji.fire' },
}

interface AnimatedEmojiProps {
  emoji: string
  size?: number
  className?: string
}

/** Renders a known emoji as its animated Fluent Emoji PNG; falls back to the plain character. */
export function AnimatedEmoji({ emoji, size = 40, className }: AnimatedEmojiProps) {
  const { t } = useTranslation('common')
  const animation = ANIMATED_EMOJI[emoji]

  if (!animation) {
    return <span className={className}>{emoji}</span>
  }

  return (
    <img
      src={animation.src}
      alt={t(animation.altKey)}
      width={size}
      height={size}
      className={cn('inline-block shrink-0', className)}
      style={{ width: size, height: size }}
    />
  )
}
