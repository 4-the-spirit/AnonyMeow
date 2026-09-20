import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { useCreateBan } from '../hooks'

export function CreateBanDialog({ communityName }: { communityName: string }) {
  const { t } = useTranslation('moderation')
  const { t: tCommon } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const createBan = useCreateBan(communityName)
  const schema = useMemo(
    () =>
      z.object({
        username: z.string().min(1, tCommon('validation.usernameRequired')),
        reason: z.string().min(1, tCommon('validation.reasonRequired')),
      }),
    [tCommon]
  )
  const form = useForm({
    resolver: zodResolver(schema),
    defaultValues: { username: '', reason: '' },
  })

  async function onSubmit(values: z.infer<typeof schema>) {
    try {
      await createBan.mutateAsync(values)
      toast.success(t('bans.banSuccessToast', { username: `u/${values.username}` }))
      setOpen(false)
      form.reset()
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('bans.banErrorToast'))
      }
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="destructive">{t('bans.banUser')}</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('bans.dialogTitle', { name: communityName })}</DialogTitle>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <FormField
              control={form.control}
              name="username"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('bans.usernameLabel')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('bans.usernamePlaceholder')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('bans.reasonLabel')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('bans.reasonPlaceholder')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button type="submit" variant="destructive" disabled={createBan.isPending}>
                {t('bans.ban')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
