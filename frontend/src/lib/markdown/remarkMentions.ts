import { visit } from 'unist-util-visit'
import type { Link, PhrasingContent, Root, Text } from 'mdast'
import type { Plugin } from 'unified'

/** Matches the backend's MentionParsingService regex exactly (Services/MentionParsingService.cs). */
const MENTION_PATTERN = /@([a-zA-Z0-9_]{3,20})/g

/**
 * Turns `@username` text into a link to that user's profile. No existence check per mention —
 * an unknown username just 404s on UserProfilePage like any other bad profile link, same as the
 * backend never validating a mention resolves before letting the comment through un-mentioned.
 */
export const remarkMentions: Plugin<[], Root> = () => (tree) => {
  visit(tree, 'text', (node: Text, index, parent) => {
    if (index === undefined || index === null || !parent) return

    MENTION_PATTERN.lastIndex = 0
    if (!MENTION_PATTERN.test(node.value)) return

    const children: PhrasingContent[] = []
    let lastIndex = 0
    MENTION_PATTERN.lastIndex = 0
    let match: RegExpExecArray | null
    while ((match = MENTION_PATTERN.exec(node.value)) !== null) {
      if (match.index > lastIndex) {
        children.push({ type: 'text', value: node.value.slice(lastIndex, match.index) })
      }
      const username = match[1]
      const link: Link = {
        type: 'link',
        url: `/u/${username}`,
        children: [{ type: 'text', value: `@${username}` }],
      }
      children.push(link)
      lastIndex = match.index + match[0].length
    }
    if (lastIndex < node.value.length) {
      children.push({ type: 'text', value: node.value.slice(lastIndex) })
    }

    parent.children.splice(index, 1, ...children)
    return index + children.length
  })
}
