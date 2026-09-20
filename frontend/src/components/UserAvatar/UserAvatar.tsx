import { useMemo } from 'react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { avatarDataUri } from '@/lib/dicebear'
import { cn } from '@/lib/utils'

interface UserAvatarProps {
  seed: string | null
  username: string
  className?: string
}

export function UserAvatar({ seed, username, className }: UserAvatarProps) {
  const dataUri = useMemo(() => (seed ? avatarDataUri(seed) : null), [seed])

  return (
    <Avatar className={cn('size-8', className)}>
      {dataUri && <AvatarImage src={dataUri} alt={`${username}'s avatar`} />}
      <AvatarFallback>{username.slice(0, 2).toUpperCase()}</AvatarFallback>
    </Avatar>
  )
}
