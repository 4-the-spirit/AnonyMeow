import { Input } from '@/components/ui/input'

const HEX_COLOR_REGEX = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/
const FULL_HEX_COLOR_REGEX = /^#[0-9a-fA-F]{6}$/

/** Hex text input + a native color picker kept in sync with it — no picker library needed since
 * <input type="color"> already gives a full OS/browser color-picker UI. */
export function FlairColorField({
  value,
  onChange,
  placeholder,
}: {
  value: string
  onChange: (value: string) => void
  placeholder?: string
}) {
  return (
    <div className="flex items-center gap-2">
      <Input value={value} placeholder={placeholder} className="w-28" onChange={(e) => onChange(e.target.value)} />
      <input
        type="color"
        aria-label={placeholder}
        className="size-8 shrink-0 cursor-pointer rounded border bg-transparent p-0"
        value={FULL_HEX_COLOR_REGEX.test(value) ? value : '#000000'}
        onChange={(e) => onChange(e.target.value)}
      />
      {!FULL_HEX_COLOR_REGEX.test(value) && (
        <span
          aria-hidden
          className="size-6 shrink-0 rounded-full border"
          style={{ backgroundColor: HEX_COLOR_REGEX.test(value) ? value : undefined }}
        />
      )}
    </div>
  )
}
