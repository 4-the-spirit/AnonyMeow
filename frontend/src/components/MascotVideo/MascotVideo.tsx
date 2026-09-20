import type { CSSProperties, MouseEvent } from 'react'

interface MascotVideoProps {
  src: string
  className?: string
  width?: number
  height?: number
  style?: CSSProperties
  'aria-label'?: string
  'aria-hidden'?: boolean
}

function blockContextMenu(event: MouseEvent<HTMLVideoElement>) {
  event.preventDefault()
}

/** Autoplaying, muted, looping mascot video shared by the sidebar, auth pages, and logo.
 * Right-click, drag, download-button, and picture-in-picture are disabled as a casual
 * deterrent against saving the clip — this is a UI speed bump, not real protection, since
 * the file is still served as a plain video that devtools/screen-recording can capture. */
export function MascotVideo({
  src,
  className,
  width,
  height,
  style,
  'aria-label': ariaLabel,
  'aria-hidden': ariaHidden,
}: MascotVideoProps) {
  return (
    <video
      src={src}
      width={width}
      height={height}
      style={style}
      aria-label={ariaLabel}
      aria-hidden={ariaHidden}
      autoPlay
      loop
      muted
      playsInline
      draggable={false}
      controlsList="nodownload noremoteplayback nofullscreen"
      disablePictureInPicture
      onContextMenu={blockContextMenu}
      className={className}
    />
  )
}
