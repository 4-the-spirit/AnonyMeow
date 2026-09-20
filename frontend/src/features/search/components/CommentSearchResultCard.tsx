import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { MarkdownBody } from '@/components/MarkdownBody/MarkdownBody'
import { getDisplayName } from '@/lib/userDisplay'
import type { CommentResponse } from '@/features/comments/types'

/** Search's CommentResponse never carries a postId (only the profile "Comments" tab populates
 * it — see backend `CommentResponseAssembly`), so unlike other comment renderings this card
 * can't deep-link to the comment's post. Author + body + score only. */
export function CommentSearchResultCard({ comment }: { comment: CommentResponse }) {
  const { t } = useTranslation('search')
  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between gap-2 space-y-0">
        <Link
          to={`/u/${comment.authorUsername}`}
          className="text-muted-foreground text-xs hover:underline"
        >
          {getDisplayName({ username: comment.authorUsername, displayName: comment.authorDisplayName })}
        </Link>
        <span className="text-muted-foreground text-xs">
          {t('results.commentScore', { score: comment.score })}
        </span>
      </CardHeader>
      <CardContent>
        <MarkdownBody className="text-sm" allowImages={false}>
          {comment.bodyMarkdown}
        </MarkdownBody>
      </CardContent>
    </Card>
  )
}
