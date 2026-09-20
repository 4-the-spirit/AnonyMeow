import { useEffect, useMemo } from 'react'
import { X } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Form, FormControl, FormField, FormItem, FormMessage } from '@/components/ui/form'
import { describePiiBlock, getPiiCategories } from '@/lib/api/problemDetails'
import { useSendMessage } from '../hooks'
import type { MessageResponse } from '../types'

interface MessageComposerProps {
  conversationId: string
  /** Display label (display name, falling back to username) of the other participant, shown in
   * the "replying to X" banner. */
  otherDisplayName: string
  /** Whether the current user has blocked the other participant — owned by the parent
   * (ConversationPage) so it updates immediately when toggled via BlockActionButton, rather
   * than only picking up the change on this component's next unrelated re-render. */
  blockedByMe?: boolean
  replyTo?: MessageResponse | null
  onCancelReply?: () => void
  onSent?: () => void
}

export function MessageComposer({
  conversationId,
  otherDisplayName,
  blockedByMe = false,
  replyTo,
  onCancelReply,
  onSent,
}: MessageComposerProps) {
  const { t } = useTranslation('conversations')
  const { t: tCommon } = useTranslation('common')
  const sendMessage = useSendMessage(conversationId)
  const schema = useMemo(
    () => z.object({ body: z.string().min(1, tCommon('validation.messageRequired')).max(2000) }),
    [tCommon]
  )
  const form = useForm({ resolver: zodResolver(schema), defaultValues: { body: '' } })

  useEffect(() => {
    if (replyTo) form.setFocus('body')
  }, [replyTo, form])

  async function onSubmit(values: { body: string }) {
    try {
      await sendMessage.mutateAsync({ ...values, replyToMessageId: replyTo?.id })
      form.reset()
      onCancelReply?.()
      onSent?.()
    } catch (error) {
      const piiCategories = getPiiCategories(error)
      if (piiCategories) {
        toast.error(describePiiBlock(piiCategories))
      } else {
        toast.error(t('composer.errorToast'))
      }
    }
  }

  if (blockedByMe) {
    return <p className="text-muted-foreground border-input rounded-lg border border-dashed px-3 py-2 text-sm">{t('block.blockedNotice')}</p>
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-1.5">
        {replyTo && (
          <div className="bg-muted flex items-center justify-between gap-2 rounded-lg px-3 py-1.5 text-sm">
            <span className="truncate">
              <span className="text-muted-foreground">{t('reply.replyingTo', { username: otherDisplayName })}: </span>
              {replyTo.body}
            </span>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={t('reply.cancel')}
              onClick={onCancelReply}
            >
              <X className="size-3.5" />
            </Button>
          </div>
        )}
        <div className="flex items-end gap-2">
          <FormField
            control={form.control}
            name="body"
            render={({ field }) => (
              <FormItem className="flex-1">
                <FormControl>
                  <Textarea placeholder={t('composer.placeholder')} rows={2} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={sendMessage.isPending}>
            {t('composer.send')}
          </Button>
        </div>
      </form>
    </Form>
  )
}
