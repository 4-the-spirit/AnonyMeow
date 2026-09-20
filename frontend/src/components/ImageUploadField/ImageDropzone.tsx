import { useId, useState } from 'react'
import { ImagePlus } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ImageDropzoneProps {
  onFileSelected: (file: File) => void
  disabled?: boolean
  label: string
  hint?: string
  compact?: boolean
  className?: string
}

/**
 * Styled stand-in for a bare `<input type="file">` — click-to-browse (via the wrapping
 * label) and drag-and-drop both funnel through the same onFileSelected callback so callers
 * don't need to handle native file events.
 */
export function ImageDropzone({
  onFileSelected,
  disabled,
  label,
  hint,
  compact,
  className,
}: ImageDropzoneProps) {
  const [isDragging, setIsDragging] = useState(false)
  const inputId = useId()

  function handleDrop(e: React.DragEvent<HTMLLabelElement>) {
    e.preventDefault()
    setIsDragging(false)
    if (disabled) return
    const file = e.dataTransfer.files?.[0]
    if (file) onFileSelected(file)
  }

  return (
    <label
      htmlFor={inputId}
      onDragOver={(e) => {
        e.preventDefault()
        if (!disabled) setIsDragging(true)
      }}
      onDragLeave={() => setIsDragging(false)}
      onDrop={handleDrop}
      className={cn(
        'border-input flex flex-col items-center justify-center gap-1 rounded-lg border-2 border-dashed text-center transition-colors',
        compact ? 'gap-0.5 p-3' : 'p-6',
        disabled
          ? 'pointer-events-none opacity-50'
          : 'hover:border-primary/50 hover:bg-muted/50 cursor-pointer',
        isDragging && 'border-primary bg-primary/5',
        className
      )}
    >
      <ImagePlus className={cn('text-muted-foreground', compact ? 'size-5' : 'size-8')} />
      <span className={cn('font-medium', compact ? 'text-xs' : 'text-sm')}>{label}</span>
      {hint && <span className="text-muted-foreground text-xs">{hint}</span>}
      <input
        id={inputId}
        type="file"
        accept="image/*"
        className="sr-only"
        disabled={disabled}
        onChange={(e) => {
          const file = e.target.files?.[0]
          e.target.value = ''
          if (file) onFileSelected(file)
        }}
      />
    </label>
  )
}
