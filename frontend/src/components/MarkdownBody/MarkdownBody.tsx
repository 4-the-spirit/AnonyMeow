import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { cn } from '@/lib/utils'
import { remarkMentions } from '@/lib/markdown/remarkMentions'
import { LinkWarningDialog } from '@/components/LinkWarningDialog/LinkWarningDialog'

interface MarkdownBodyProps {
  children: string
  className?: string
  /** Whether `![alt](src)` markdown image syntax renders as an actual image. Defaults to true;
   * comments render with this off since there's no comment image feature. */
  allowImages?: boolean
}

/** Renders in place of an `<img>` when images are disallowed — keeps the alt text (if any)
 * visible as plain text instead of silently dropping content the author wrote. */
function DisabledImage({ alt }: { alt?: string }) {
  return alt ? <span>{alt}</span> : null
}

/** `@mention` links (produced by remarkMentions) navigate client-side; every other
 * (author-written) link goes through the same "leaving AnonyMeow" confirmation as a post's
 * standalone submitted URL, rather than navigating straight out. Both get explicit link styling —
 * the `prose` typography plugin renders `<a>` in the same color as body text by default, which
 * otherwise makes links visually indistinguishable from plain text. */
const linkClassName = 'text-primary font-medium no-underline hover:underline'

function Anchor({ href, children }: { href?: string; children?: ReactNode }) {
  if (href?.startsWith('/u/')) {
    return (
      <Link to={href} className={linkClassName}>
        {children}
      </Link>
    )
  }
  if (!href) {
    return <span>{children}</span>
  }
  return (
    <LinkWarningDialog href={href} className={linkClassName}>
      {children}
    </LinkWarningDialog>
  )
}

/**
 * The only place post/comment markdown is rendered. Deliberately never adds rehype-raw or
 * dangerouslySetInnerHTML — react-markdown renders to React elements and never interprets
 * embedded HTML unless that plugin is added, which is the XSS guard for user content.
 */
export function MarkdownBody({ children, className, allowImages = true }: MarkdownBodyProps) {
  return (
    <div
      className={cn(
        'prose prose-sm dark:prose-invert max-w-none break-words',
        '[&_p]:my-2 [&_ul]:my-2 [&_ol]:my-2 [&_blockquote]:my-2',
        className
      )}
    >
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkMentions]}
        components={{ a: Anchor, ...(allowImages ? {} : { img: DisabledImage }) }}
      >
        {children}
      </ReactMarkdown>
    </div>
  )
}
