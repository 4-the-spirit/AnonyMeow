import type { TFunction } from 'i18next'

/** Matches the backend's heartbeat throttle (CurrentUserAccessor) plus a little slack for the
 * frontend's own 30s presence poll interval. */
const ONLINE_THRESHOLD_MS = 2 * 60 * 1000

export function isOnline(lastSeenAt: string | null): boolean {
  if (!lastSeenAt) return false
  return Date.now() - new Date(lastSeenAt).getTime() < ONLINE_THRESHOLD_MS
}

/** Relative "Last seen X ago" text via the built-in Intl API — no date library needed, and it
 * already localizes correctly for Hebrew/RTL. */
export function formatLastSeen(lastSeenAt: string | null, locale: string, t: TFunction): string {
  if (!lastSeenAt) return t('presence.neverActive', { ns: 'conversations' })

  const diffMs = Date.now() - new Date(lastSeenAt).getTime()
  const diffMinutes = Math.round(diffMs / 60_000)
  if (diffMinutes < 1) {
    return t('presence.lastSeen', { ns: 'conversations', time: t('presence.justNow', { ns: 'conversations' }) })
  }

  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })
  let relative: string
  if (diffMinutes < 60) {
    relative = rtf.format(-diffMinutes, 'minute')
  } else {
    const diffHours = Math.round(diffMinutes / 60)
    relative = diffHours < 24 ? rtf.format(-diffHours, 'hour') : rtf.format(-Math.round(diffHours / 24), 'day')
  }

  return t('presence.lastSeen', { ns: 'conversations', time: relative })
}
