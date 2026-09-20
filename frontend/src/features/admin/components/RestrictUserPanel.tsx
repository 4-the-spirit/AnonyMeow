import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { useLiftRestriction, useRestrictUser } from '../hooks'
import type { PlatformRestrictionType } from '../types'

const RESTRICTION_TYPES: PlatformRestrictionType[] = ['PostingRestricted', 'FullSuspension']

export function RestrictUserPanel() {
  const { t } = useTranslation('admin')
  const { t: tCommon } = useTranslation('common')
  const restrictUser = useRestrictUser()
  const liftRestriction = useLiftRestriction()
  const [liftUsername, setLiftUsername] = useState('')

  const schema = useMemo(
    () =>
      z.object({
        username: z.string().min(1, tCommon('validation.usernameRequired')),
        type: z.enum(['PostingRestricted', 'FullSuspension']),
        reason: z.string().min(1, tCommon('validation.reasonRequired')),
        endAtUtc: z.string().optional(),
      }),
    [tCommon]
  )
  const form = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      username: '',
      type: 'PostingRestricted' as PlatformRestrictionType,
      reason: '',
      endAtUtc: '',
    },
  })

  async function onSubmit(values: z.infer<typeof schema>) {
    try {
      await restrictUser.mutateAsync({
        username: values.username,
        body: {
          type: values.type,
          reason: values.reason,
          endAtUtc: values.endAtUtc ? new Date(values.endAtUtc).toISOString() : undefined,
        },
      })
      toast.success(t('restrictions.restrictSuccessToast', { username: `u/${values.username}` }))
      form.reset()
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('restrictions.restrictErrorToast'))
      }
    }
  }

  async function handleLift() {
    if (!liftUsername) return
    try {
      await liftRestriction.mutateAsync(liftUsername)
      toast.success(t('restrictions.liftSuccessToast', { username: `u/${liftUsername}` }))
      setLiftUsername('')
    } catch {
      toast.error(t('restrictions.liftErrorToast'))
    }
  }

  return (
    <div className="flex flex-col gap-6 pt-3">
      <div className="rounded-lg border p-4">
        <h2 className="mb-3 font-medium">{t('restrictions.restrictTitle')}</h2>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <FormField
              control={form.control}
              name="username"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('restrictions.usernameLabel')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('restrictions.usernamePlaceholder')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="type"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('restrictions.typeLabel')}</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {RESTRICTION_TYPES.map((type) => (
                        <SelectItem key={type} value={type}>
                          {t(`restrictions.type${type}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('restrictions.reasonLabel')}</FormLabel>
                  <FormControl>
                    <Textarea placeholder={t('restrictions.reasonPlaceholder')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="endAtUtc"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('restrictions.endAtLabel')}</FormLabel>
                  <FormControl>
                    <Input type="datetime-local" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div>
              <Button type="submit" variant="destructive" disabled={restrictUser.isPending}>
                {t('restrictions.restrictSubmit')}
              </Button>
            </div>
          </form>
        </Form>
      </div>

      <div className="rounded-lg border p-4">
        <h2 className="mb-3 font-medium">{t('restrictions.liftTitle')}</h2>
        <div className="flex gap-2">
          <Input
            placeholder={t('restrictions.usernamePlaceholder')}
            value={liftUsername}
            onChange={(e) => setLiftUsername(e.target.value)}
          />
          <Button
            variant="outline"
            disabled={liftRestriction.isPending || !liftUsername}
            onClick={handleLift}
          >
            {t('restrictions.liftSubmit')}
          </Button>
        </div>
      </div>
    </div>
  )
}
