import { useMemo, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  FormDescription,
} from '@/components/ui/form'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { usernameSchema } from '@/lib/validation'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { avatarDataUri, randomAvatarSeed } from '@/lib/dicebear'
import { useCompleteProfile, useUsernameAvailability } from '@/features/users/hooks'

type FormValues = {
  username: string
  displayName: string
  avatarSeed: string
}

export function CompleteProfilePage() {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const [seed, setSeed] = useState(randomAvatarSeed())
  const completeProfile = useCompleteProfile()

  const schema = useMemo(
    () =>
      z.object({
        username: usernameSchema(tCommon),
        displayName: z.string().min(1, tCommon('validation.displayNameRequired')).max(50),
        avatarSeed: z.string().min(1),
      }),
    [tCommon]
  )

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { username: '', displayName: '', avatarSeed: seed },
  })

  const username = form.watch('username')
  const usernameFormatValid = usernameSchema(tCommon).safeParse(username).success
  const availability = useUsernameAvailability(username, usernameFormatValid)

  function regenerateAvatar() {
    const next = randomAvatarSeed()
    setSeed(next)
    form.setValue('avatarSeed', next)
  }

  async function onSubmit(values: FormValues) {
    try {
      await completeProfile.mutateAsync({ ...values, avatarSeed: seed })
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('completeProfile.errorGeneric'))
      }
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">{t('completeProfile.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('completeProfile.intro')}</p>
      </div>

      <div className="flex items-center gap-4">
        <UserAvatar seed={seed} username={username || 'you'} className="size-16" />
        <Button type="button" variant="outline" size="sm" onClick={regenerateAvatar}>
          {t('completeProfile.shuffleAvatar')}
        </Button>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <FormField
            control={form.control}
            name="username"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('completeProfile.usernameLabel')}</FormLabel>
                <FormControl>
                  <Input placeholder={t('completeProfile.usernamePlaceholder')} {...field} />
                </FormControl>
                {usernameFormatValid && availability.data && (
                  <FormDescription
                    className={
                      availability.data.isAvailable ? 'text-green-600' : 'text-destructive'
                    }
                  >
                    {availability.data.isAvailable
                      ? t('completeProfile.usernameAvailable')
                      : t('completeProfile.usernameTaken')}
                  </FormDescription>
                )}
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="displayName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('completeProfile.displayNameLabel')}</FormLabel>
                <FormControl>
                  <Input placeholder={t('completeProfile.displayNamePlaceholder')} {...field} />
                </FormControl>
                <FormDescription>{t('completeProfile.displayNameHint')}</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />

          <Button
            type="submit"
            disabled={
              completeProfile.isPending ||
              (usernameFormatValid && availability.data?.isAvailable === false)
            }
          >
            {completeProfile.isPending ? t('completeProfile.creating') : t('completeProfile.continue')}
          </Button>
        </form>
      </Form>
      {/* eager-load once so the avatar preview above never flashes blank on first paint */}
      <img src={avatarDataUri(seed)} alt="" className="hidden" aria-hidden />
    </div>
  )
}
