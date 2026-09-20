import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { useStartConversation } from '../hooks'

export function MessageButton({ username }: { username: string }) {
  const { t } = useTranslation('conversations')
  const startConversation = useStartConversation()

  async function handleClick() {
    try {
      await startConversation.mutateAsync({ username })
    } catch {
      toast.error(t('startErrorToast'))
    }
  }

  return (
    <Button variant="outline" onClick={handleClick} disabled={startConversation.isPending}>
      {t('message')}
    </Button>
  )
}
