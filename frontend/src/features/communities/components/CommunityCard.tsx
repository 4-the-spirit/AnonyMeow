import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Card, CardContent } from '@/components/ui/card'
import type { CommunityResponse } from '../types'

export function CommunityCard({ community }: { community: CommunityResponse }) {
  const { t } = useTranslation('communities')
  return (
    <Link to={`/c/${community.name}`} className="block">
      <Card className="gap-0 overflow-hidden p-0 transition-shadow hover:shadow-md">
        <div className="relative">
          {community.bannerImageUrl ? (
            <img
              src={community.bannerImageUrl}
              alt=""
              className="h-20 w-full object-cover"
            />
          ) : (
            <div className="from-primary/25 via-accent/40 to-secondary h-20 w-full bg-linear-to-br" />
          )}
          <div className="bg-card ring-card absolute -bottom-6 start-4 flex size-14 items-center justify-center overflow-hidden rounded-full ring-4">
            {community.iconImageUrl ? (
              <img
                src={community.iconImageUrl}
                alt=""
                className="size-full rounded-full object-cover"
              />
            ) : (
              <div className="bg-muted text-muted-foreground flex size-full items-center justify-center rounded-full text-lg font-semibold">
                {community.name.charAt(0)}
              </div>
            )}
          </div>
        </div>
        <CardContent className="flex flex-col gap-1 pt-8 pb-4">
          <h3 className="font-heading truncate text-base font-medium">c/{community.name}</h3>
          {community.description && (
            <p className="text-muted-foreground line-clamp-2 text-sm">{community.description}</p>
          )}
          <p className="text-muted-foreground text-xs">
            {t('detail.memberCount', { count: community.memberCount })}
          </p>
        </CardContent>
      </Card>
    </Link>
  )
}
