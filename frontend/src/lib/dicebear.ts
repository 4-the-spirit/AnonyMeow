import { createAvatar } from '@dicebear/core'
import { funEmoji } from '@dicebear/collection'

/** Playful style matching the "AnonyMeow" casual tone. Memoize per-seed at call sites. */
export function avatarDataUri(seed: string): string {
  return createAvatar(funEmoji, { seed }).toDataUri()
}

export function randomAvatarSeed(): string {
  return Math.random().toString(36).slice(2, 10)
}
