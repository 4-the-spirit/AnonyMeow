/** Lightweight markdown -> plain text for short previews (post card excerpts). Not a full
 * parser — just strips the syntax that would otherwise show up as literal punctuation. */
export function stripMarkdown(markdown: string): string {
  return markdown
    .replace(/!\[[^\]]*]\([^)]*\)/g, '')
    .replace(/\[([^\]]*)]\([^)]*\)/g, '$1')
    .replace(/[*_~`>#]+/g, '')
    .replace(/\s+/g, ' ')
    .trim()
}
