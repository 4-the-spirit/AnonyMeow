import { createBrowserRouter } from 'react-router-dom'
import { RootGate } from './RootGate'
import { RequireAuth } from './RequireAuth'
import { AppLayout } from './AppLayout'
import { NotFoundPage } from './NotFoundPage'
import { SignInPage } from '@/features/auth/pages/SignInPage'
import { SignUpPage } from '@/features/auth/pages/SignUpPage'
import { ForgotPasswordPage } from '@/features/auth/pages/ForgotPasswordPage'
import { DevSwitchUserPage } from '@/features/auth/pages/DevSwitchUserPage'
import { CommunityBrowsePage } from '@/features/communities/pages/CommunityBrowsePage'
import { CreateCommunityPage } from '@/features/communities/pages/CreateCommunityPage'
import { CommunityDetailPage } from '@/features/communities/pages/CommunityDetailPage'
import { CommunitySettingsPage } from '@/features/communities/pages/CommunitySettingsPage'
import { CommunityModeratorsPage } from '@/features/communities/pages/CommunityModeratorsPage'
import { CommunityMembersPage } from '@/features/communities/pages/CommunityMembersPage'
import { CreatePostPage } from '@/features/posts/pages/CreatePostPage'
import { PostDetailPage } from '@/features/posts/pages/PostDetailPage'
import { CommentThreadPage } from '@/features/comments/pages/CommentThreadPage'
import { UserProfilePage } from '@/features/users/pages/UserProfilePage'
import { EditProfilePage } from '@/features/users/pages/EditProfilePage'
import { ModQueuePage } from '@/features/moderation/pages/ModQueuePage'
import { ModBansPage } from '@/features/moderation/pages/ModBansPage'
import { CommunityFlairsPage } from '@/features/flairs/pages/CommunityFlairsPage'
import { FriendRequestsPage } from '@/features/friends/pages/FriendRequestsPage'
import { NotificationsPage } from '@/features/notifications/pages/NotificationsPage'
import { ConversationsListPage } from '@/features/conversations/pages/ConversationsListPage'
import { ConversationPage } from '@/features/conversations/pages/ConversationPage'
import { FeedPage } from '@/features/feed/pages/FeedPage'
import { SavedPage } from '@/features/saved/pages/SavedPage'
import { AdminPage } from '@/features/admin/pages/AdminPage'
import { TrendingPage } from '@/features/discovery/pages/TrendingPage'
import { SearchPage } from '@/features/search/pages/SearchPage'

export const router = createBrowserRouter([
  {
    element: <RootGate />,
    children: [
      { path: '/signin', element: <SignInPage /> },
      { path: '/signup', element: <SignUpPage /> },
      { path: '/forgot-password', element: <ForgotPasswordPage /> },
      {
        element: <AppLayout />,
        children: [
          // Publicly viewable — no account required.
          { path: '/', element: <CommunityBrowsePage /> },
          { path: '/feed', element: <FeedPage /> },
          { path: '/trending', element: <TrendingPage /> },
          { path: '/search', element: <SearchPage /> },
          { path: '/c/:communityName', element: <CommunityDetailPage /> },
          { path: '/c/:communityName/moderators', element: <CommunityModeratorsPage /> },
          { path: '/c/:communityName/members', element: <CommunityMembersPage /> },
          { path: '/posts/:postId', element: <PostDetailPage /> },
          { path: '/posts/:postId/comments/:commentId', element: <CommentThreadPage /> },
          { path: '/u/:username', element: <UserProfilePage /> },

          // Account-scoped — RequireAuth redirects anonymous visitors to /signin.
          {
            element: <RequireAuth />,
            children: [
              { path: '/saved', element: <SavedPage /> },
              { path: '/admin', element: <AdminPage /> },
              { path: '/communities/new', element: <CreateCommunityPage /> },
              { path: '/c/:communityName/submit', element: <CreatePostPage /> },
              { path: '/c/:communityName/settings', element: <CommunitySettingsPage /> },
              { path: '/c/:communityName/mod/queue', element: <ModQueuePage /> },
              { path: '/c/:communityName/mod/bans', element: <ModBansPage /> },
              { path: '/c/:communityName/flairs', element: <CommunityFlairsPage /> },
              { path: '/settings/profile', element: <EditProfilePage /> },
              { path: '/friends/requests', element: <FriendRequestsPage /> },
              { path: '/notifications', element: <NotificationsPage /> },
              { path: '/messages', element: <ConversationsListPage /> },
              { path: '/messages/:conversationId', element: <ConversationPage /> },
              { path: '/dev/switch-user', element: <DevSwitchUserPage /> },
            ],
          },

          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
])
