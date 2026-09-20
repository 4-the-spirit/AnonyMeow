import type { FlairResponse } from '../types'

export function FlairBadge({ flair }: { flair: FlairResponse }) {
  return (
    <span
      className="inline-flex h-5 shrink-0 items-center rounded-4xl border px-2 text-xs font-medium"
      style={{ borderColor: flair.colorHex, color: flair.colorHex }}
    >
      {flair.name}
    </span>
  )
}
