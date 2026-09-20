/** Preferred label for showing a user: their display name, falling back to their (permanent,
 * unique) username when they haven't set one. Never use this for routing/lookups — keep
 * using the username for those, since it's the stable identifier. */
export function getDisplayName(user: { username: string; displayName?: string | null }): string {
  return user.displayName ?? user.username
}
