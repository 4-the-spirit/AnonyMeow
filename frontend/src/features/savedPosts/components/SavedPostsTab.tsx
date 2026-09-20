import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PostResultsList } from '@/features/posts/components/PostResultsList'
import { useSavedPosts, useSavedPostsCache } from '../hooks'

export function SavedPostsTab() {
  const { t } = useTranslation('savedPosts')
  const [page, setPage] = useState(1)
  const savedPosts = useSavedPosts(page)
  const { markSaved } = useSavedPostsCache()

  // GET /api/users/me/saved-posts is authoritative for the posts it returns, so seed the
  // non-authoritative local cache from it — otherwise a saved post's SaveButton would render
  // as "unsaved" here until the user re-toggles it once in this browser.
  useEffect(() => {
    savedPosts.data?.items.forEach((post) => markSaved(post.id, true))
  }, [savedPosts.data])

  return (
    <PostResultsList
      query={savedPosts}
      onPageChange={setPage}
      emptyTitle={t('emptyTitle')}
      emptyDescription={t('emptyDescription')}
    />
  )
}
