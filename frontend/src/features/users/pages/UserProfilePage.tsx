import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { ErrorState } from '@/components/ErrorState/ErrorState'
import { useAuth } from '@/features/auth/useAuth'
import { FriendActionButton } from '@/features/friends/components/FriendActionButton'
import { FriendsListTab } from '@/features/friends/components/FriendsListTab'
import { MessageButton } from '@/features/conversations/components/MessageButton'
import { BlockActionButton } from '@/features/blocks/components/BlockActionButton'
import { useUserProfile } from '../hooks'
import { ProfilePostsTab } from '../components/ProfilePostsTab'
import { ProfileCommentsTab } from '../components/ProfileCommentsTab'
import { ProfileCommunitiesTab } from '../components/ProfileCommunitiesTab'

export function UserProfilePage() {
  const { t, i18n } = useTranslation('profile')
  const { username = '' } = useParams()
  const { user: currentUser } = useAuth()
  const profile = useUserProfile(username)

  if (profile.isPending) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  if (profile.isError) {
    return <ErrorState error={profile.error} onRetry={() => profile.refetch()} />
  }

  const data = profile.data
  const isOwnProfile = currentUser?.username === data.username

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <UserAvatar seed={data.avatarSeed} username={data.username} className="size-16" />
          <div>
            <h1 className="text-xl font-semibold">{data.displayName ?? data.username}</h1>
            <p className="text-muted-foreground text-sm">u/{data.username}</p>
            <p className="text-muted-foreground text-xs">
              {t('karmaAndJoined', {
                karma: data.karma,
                date: new Date(data.createdAtUtc).toLocaleDateString(i18n.language),
              })}
            </p>
          </div>
        </div>
        {isOwnProfile ? (
          <Button asChild variant="outline">
            <Link to="/settings/profile">{t('edit.title')}</Link>
          </Button>
        ) : (
          <div className="flex gap-2">
            <MessageButton username={data.username} />
            <FriendActionButton username={data.username} />
            <BlockActionButton username={data.username} />
          </div>
        )}
      </div>

      <Tabs defaultValue="posts">
        <TabsList>
          <TabsTrigger value="posts">{t('tabs.posts')}</TabsTrigger>
          <TabsTrigger value="comments">{t('tabs.comments')}</TabsTrigger>
          <TabsTrigger value="communities">{t('tabs.communities')}</TabsTrigger>
          <TabsTrigger value="friends">{t('tabs.friends')}</TabsTrigger>
        </TabsList>
        <TabsContent value="posts">
          <ProfilePostsTab username={data.username} />
        </TabsContent>
        <TabsContent value="comments">
          <ProfileCommentsTab username={data.username} />
        </TabsContent>
        <TabsContent value="communities">
          <ProfileCommunitiesTab username={data.username} />
        </TabsContent>
        <TabsContent value="friends">
          <FriendsListTab username={data.username} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
