import { useTranslation } from 'react-i18next'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { SavedPostsTab } from '@/features/savedPosts/components/SavedPostsTab'
import { SavedCommentsTab } from '@/features/savedComments/components/SavedCommentsTab'

export function SavedPage() {
  const { t } = useTranslation('saved')

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t('pageTitle')}</h1>
      <Tabs defaultValue="posts">
        <TabsList>
          <TabsTrigger value="posts">{t('tabs.posts')}</TabsTrigger>
          <TabsTrigger value="comments">{t('tabs.comments')}</TabsTrigger>
        </TabsList>
        <TabsContent value="posts">
          <SavedPostsTab />
        </TabsContent>
        <TabsContent value="comments">
          <SavedCommentsTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}
