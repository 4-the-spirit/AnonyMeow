import { useTranslation } from 'react-i18next'
import { useCommunityPosts } from '../hooks'
import { PostResultsList } from './PostResultsList'
import type { SortOrder } from '../types'

interface PostListProps {
  communityName: string
  sort: SortOrder
  page: number
  onPageChange: (page: number) => void
}

export function PostList({ communityName, sort, page, onPageChange }: PostListProps) {
  const { t } = useTranslation('posts')
  const query = useCommunityPosts(communityName, sort, page)

  return (
    <PostResultsList
      query={query}
      onPageChange={onPageChange}
      emptyTitle={t('list.noPostsYetTitle')}
      emptyDescription={t('list.noPostsYetDescription')}
      currentCommunityName={communityName}
    />
  )
}
