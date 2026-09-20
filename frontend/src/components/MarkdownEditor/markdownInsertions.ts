export type MarkdownAction =
  | 'bold'
  | 'italic'
  | 'code'
  | 'link'
  | 'bulletList'
  | 'numberedList'
  | 'blockquote'

export interface MarkdownInsertionResult {
  text: string
  selectionStart: number
  selectionEnd: number
}

const WRAP_MARKERS: Record<'bold' | 'italic' | 'code', { marker: string; placeholder: string }> = {
  bold: { marker: '**', placeholder: 'bold text' },
  italic: { marker: '_', placeholder: 'italic text' },
  code: { marker: '`', placeholder: 'code' },
}

function applyWrap(
  text: string,
  selectionStart: number,
  selectionEnd: number,
  marker: string,
  placeholder: string
): MarkdownInsertionResult {
  const before = text.slice(0, selectionStart)
  const selected = text.slice(selectionStart, selectionEnd)
  const after = text.slice(selectionEnd)

  if (selected.length > 0 && before.endsWith(marker) && after.startsWith(marker)) {
    const newBefore = before.slice(0, before.length - marker.length)
    const newAfter = after.slice(marker.length)
    return {
      text: newBefore + selected + newAfter,
      selectionStart: newBefore.length,
      selectionEnd: newBefore.length + selected.length,
    }
  }

  const content = selected || placeholder
  const newSelectionStart = before.length + marker.length
  return {
    text: before + marker + content + marker + after,
    selectionStart: newSelectionStart,
    selectionEnd: newSelectionStart + content.length,
  }
}

/** Prefixes every non-empty line touched by the selection; `prefixForLine` receives a
 * zero-based counter over non-empty lines only, so numbered lists count 1, 2, 3… correctly
 * even when the selection includes blank lines. */
function applyLinePrefix(
  text: string,
  selectionStart: number,
  selectionEnd: number,
  prefixForLine: (lineIndex: number) => string
): MarkdownInsertionResult {
  const lineStart = text.lastIndexOf('\n', selectionStart - 1) + 1
  const nextNewline = text.indexOf('\n', selectionEnd)
  const lineEnd = nextNewline === -1 ? text.length : nextNewline

  const before = text.slice(0, lineStart)
  const after = text.slice(lineEnd)
  const lines = text.slice(lineStart, lineEnd).split('\n')

  let lineIndex = 0
  const prefixedLines = lines.map((line) => {
    if (line.length === 0) return line
    const prefix = prefixForLine(lineIndex)
    lineIndex += 1
    return prefix + line
  })
  const newSelected = prefixedLines.join('\n')

  return {
    text: before + newSelected + after,
    selectionStart: before.length,
    selectionEnd: before.length + newSelected.length,
  }
}

/** Replaces the current selection (or inserts at the cursor) with `insertion`, placing the
 * cursor right after it — used for emoji picks, which don't need applyWrap's toggle-off
 * behavior or a placeholder when nothing is selected. */
export function insertText(
  text: string,
  selectionStart: number,
  selectionEnd: number,
  insertion: string
): MarkdownInsertionResult {
  const before = text.slice(0, selectionStart)
  const after = text.slice(selectionEnd)
  const cursor = before.length + insertion.length
  return { text: before + insertion + after, selectionStart: cursor, selectionEnd: cursor }
}

function applyLink(text: string, selectionStart: number, selectionEnd: number): MarkdownInsertionResult {
  const before = text.slice(0, selectionStart)
  const selected = text.slice(selectionStart, selectionEnd)
  const after = text.slice(selectionEnd)
  const linkText = selected || 'link text'
  const url = 'https://'

  const urlStart = before.length + 1 + linkText.length + 2
  return {
    text: `${before}[${linkText}](${url})${after}`,
    selectionStart: urlStart,
    selectionEnd: urlStart + url.length,
  }
}

/** Builds a well-formed `[text](url)` from user-supplied fields (the link popover) instead of
 * leaving raw markdown syntax for the user to fill in by hand — `linkText` falls back to the
 * URL itself when the user didn't select/type any link text. */
export function applyLinkWithUrl(
  text: string,
  selectionStart: number,
  selectionEnd: number,
  linkText: string,
  url: string
): MarkdownInsertionResult {
  const before = text.slice(0, selectionStart)
  const after = text.slice(selectionEnd)
  const displayText = linkText.trim() || url
  const insertion = `[${displayText}](${url})`
  const cursor = before.length + insertion.length
  return { text: before + insertion + after, selectionStart: cursor, selectionEnd: cursor }
}

export function applyMarkdownAction(
  text: string,
  selectionStart: number,
  selectionEnd: number,
  action: MarkdownAction
): MarkdownInsertionResult {
  switch (action) {
    case 'bold':
    case 'italic':
    case 'code': {
      const { marker, placeholder } = WRAP_MARKERS[action]
      return applyWrap(text, selectionStart, selectionEnd, marker, placeholder)
    }
    case 'link':
      return applyLink(text, selectionStart, selectionEnd)
    case 'bulletList':
      return applyLinePrefix(text, selectionStart, selectionEnd, () => '- ')
    case 'numberedList':
      return applyLinePrefix(text, selectionStart, selectionEnd, (i) => `${i + 1}. `)
    case 'blockquote':
      return applyLinePrefix(text, selectionStart, selectionEnd, () => '> ')
  }
}
