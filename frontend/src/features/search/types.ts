import type { CommentResponse } from '@/features/comments/types'
import type { CommunityResponse } from '@/features/communities/types'
import type { PostResponse } from '@/features/posts/types'

export type SearchTargetType = 'all' | 'posts' | 'comments' | 'communities'

export interface SearchResponse {
  posts: PostResponse[]
  postsTotalCount: number
  comments: CommentResponse[]
  commentsTotalCount: number
  communities: CommunityResponse[]
  communitiesTotalCount: number
}
