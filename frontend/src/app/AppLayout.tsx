import { useEffect, useState } from 'react'
import { Bell, Bookmark, Home, MessageSquare, Newspaper, Plus, User } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarSeparator,
  SidebarTrigger,
} from '@/components/ui/sidebar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { MascotVideo } from '@/components/MascotVideo/MascotVideo'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { useAuth } from '@/features/auth/useAuth'
import { useMyFriendRequests } from '@/features/friends/hooks'
import { useUnreadNotificationCount } from '@/features/notifications/hooks'
import { useNotificationHubConnection } from '@/features/notifications/useNotificationHubConnection'
import { useRecommendedCommunities } from '@/features/discovery/hooks'
import { useUserCommunities } from '@/features/users/hooks'
import { isRtl } from '@/lib/i18n'
import { SearchBar } from '@/features/search/components/SearchBar'
import { getDisplayName } from '@/lib/userDisplay'

// Gray-background (`_fafafa`) mascot videos shown to anonymous visitors above the sign-in/
// sign-up buttons — sourced from backend/Resources/videos/gray, copied into public/mascot/. Add
// new filenames here as more gray-background videos land in that folder.
const MASCOT_VIDEOS = ['mascot-shrug.mp4']

function pickRandomMascotVideo() {
  return MASCOT_VIDEOS[Math.floor(Math.random() * MASCOT_VIDEOS.length)]
}

export function AppLayout() {
  const { t, i18n } = useTranslation('common')
  const { user, signOut } = useAuth()
  // RootGate lets anonymous visitors reach AppLayout too — `user` is null until status is
  // "ready", so every user-dependent bit below (sidebar footer, badge counts) accounts for that.
  const location = useLocation()
  // Re-rolled on every mount (page refresh) and whenever the route changes, so the sidebar
  // mascot animation varies as visitors browse.
  const [mascotVideo, setMascotVideo] = useState(pickRandomMascotVideo)
  useEffect(() => {
    setMascotVideo(pickRandomMascotVideo())
  }, [location.pathname])
  const myRequests = useMyFriendRequests()
  const incomingCount = myRequests.data?.incoming.length ?? 0
  const unreadNotifications = useUnreadNotificationCount()
  const recommendedCommunities = useRecommendedCommunities(1)
  const joinedCommunities = useUserCommunities(user?.username ?? '', 1)
  const hasJoinedCommunities = !!user && (joinedCommunities.data?.items.length ?? 0) > 0
  useNotificationHubConnection()

  const navItems = [
    { to: '/', label: t('nav.home'), icon: Home },
    { to: '/feed', label: t('nav.feed'), icon: Newspaper },
    { to: '/messages', label: t('nav.messages'), icon: MessageSquare },
    {
      to: '/notifications',
      label: t('nav.notifications'),
      icon: Bell,
      badge: unreadNotifications.data,
    },
    { to: '/saved', label: t('nav.saved'), icon: Bookmark },
    ...(user ? [{ to: `/u/${user.username}`, label: t('nav.myProfile'), icon: User }] : []),
  ]

  return (
    <SidebarProvider>
      <Sidebar side={isRtl(i18n.language) ? 'right' : 'left'} collapsible="offcanvas">
        <SidebarHeader>
          <div className="flex items-center gap-2 px-1 py-1">
            <Link to="/" className="flex items-center gap-2 text-xl font-extrabold">
              <span className="font-logo">{t('appName')}</span>
            </Link>
          </div>
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupContent>
              <SidebarMenu>
                {navItems.map((item) => (
                  <SidebarMenuItem key={item.to}>
                    <SidebarMenuButton
                      asChild
                      isActive={location.pathname === item.to}
                    >
                      <Link to={item.to}>
                        <item.icon />
                        <span>{item.label}</span>
                      </Link>
                    </SidebarMenuButton>
                    {!!item.badge && item.badge > 0 && (
                      <SidebarMenuBadge>{item.badge}</SidebarMenuBadge>
                    )}
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>

          <SidebarSeparator />

          {hasJoinedCommunities && (
            <SidebarGroup>
              <SidebarGroupLabel>{t('nav.myCommunities')}</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {joinedCommunities.data!.items.map((community) => (
                    <SidebarMenuItem key={community.name}>
                      <SidebarMenuButton asChild>
                        <Link to={`/c/${community.name}`}>
                          {community.iconImageUrl ? (
                            <img
                              src={community.iconImageUrl}
                              alt=""
                              className="size-4 shrink-0 rounded-full object-cover"
                            />
                          ) : (
                            <span className="bg-muted flex size-4 shrink-0 items-center justify-center rounded-full text-[10px]">
                              {community.name.charAt(0)}
                            </span>
                          )}
                          <span>c/{community.name}</span>
                        </Link>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          )}

          <SidebarGroup>
            <SidebarGroupLabel>
              {hasJoinedCommunities ? t('nav.suggestedCommunities') : t('nav.communities')}
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                {recommendedCommunities.data?.items.map((community) => (
                  <SidebarMenuItem key={community.name}>
                    <SidebarMenuButton asChild>
                      <Link to={`/c/${community.name}`}>
                        {community.iconImageUrl ? (
                          <img
                            src={community.iconImageUrl}
                            alt=""
                            className="size-4 shrink-0 rounded-full object-cover"
                          />
                        ) : (
                          <span className="bg-muted flex size-4 shrink-0 items-center justify-center rounded-full text-[10px]">
                            {community.name.charAt(0)}
                          </span>
                        )}
                        <span>c/{community.name}</span>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
                <SidebarMenuItem>
                  <SidebarMenuButton asChild>
                    <Link to="/communities/new">
                      <Plus />
                      <span>{t('nav.newCommunity')}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild className="text-muted-foreground">
                    <Link to="/">{t('nav.browseAll')}</Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        </SidebarContent>
        <SidebarFooter>
          {user ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  className="hover:bg-sidebar-accent flex items-center gap-2 rounded-lg px-2 py-1.5"
                >
                  <UserAvatar seed={user.avatarSeed} username={user.username} />
                  <span className="truncate text-sm font-medium">{getDisplayName(user)}</span>
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" side="top">
                <DropdownMenuItem asChild>
                  <Link to={`/u/${user.username}`}>{t('nav.myProfile')}</Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link to="/settings/profile">{t('nav.editProfile')}</Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link to="/friends/requests" className="justify-between">
                    {t('nav.friendRequests')}
                    {incomingCount > 0 && <Badge>{incomingCount}</Badge>}
                  </Link>
                </DropdownMenuItem>
                {user.isPlatformAdmin && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link to="/admin">{t('nav.platformAdmin')}</Link>
                    </DropdownMenuItem>
                  </>
                )}
                {import.meta.env.DEV && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link to="/dev/switch-user">{t('nav.switchUserDev')}</Link>
                    </DropdownMenuItem>
                  </>
                )}
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={signOut}>{t('nav.signOut')}</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : (
            <div className="flex flex-col items-center gap-2 px-2 py-1.5">
              <MascotVideo
                key={mascotVideo}
                src={`/mascot/${mascotVideo}`}
                aria-hidden
                className="h-48 w-48 max-w-full object-cover"
              />
              <div className="flex w-full items-center gap-2">
                <Button asChild size="sm" variant="outline" className="flex-1">
                  <Link to="/signin">{t('nav.signIn')}</Link>
                </Button>
                <Button asChild size="sm" className="flex-1">
                  <Link to="/signup">{t('nav.signUp')}</Link>
                </Button>
              </div>
            </div>
          )}
        </SidebarFooter>
      </Sidebar>
      <SidebarInset>
        <header className="border-border bg-background sticky top-0 z-10 flex items-center gap-3 border-b px-4 py-3">
          <SidebarTrigger className="md:hidden" />
          <SearchBar />
        </header>
        <main className="mx-auto w-full max-w-4xl px-4 py-6">
          <Outlet />
        </main>
      </SidebarInset>
    </SidebarProvider>
  )
}
