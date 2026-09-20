import { useEffect, useMemo, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { UserAvatar } from '@/components/UserAvatar/UserAvatar'
import { LoadingSpinner } from '@/components/LoadingSpinner/LoadingSpinner'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { randomAvatarSeed } from '@/lib/dicebear'
import { useAuth } from '@/features/auth/useAuth'
import { useUpdateProfile } from '../hooks'

type FormValues = {
  displayName: string
  friendListVisibility: 'Everyone' | 'FriendsOnly' | 'NoOne'
}

export function EditProfilePage() {
  const { t } = useTranslation('profile')
  const { t: tCommon } = useTranslation('common')
  const { user, refetchUser } = useAuth()
  const navigate = useNavigate()
  const updateProfile = useUpdateProfile()
  const [seed, setSeed] = useState(user?.avatarSeed ?? randomAvatarSeed())

  const schema = useMemo(
    () =>
      z.object({
        displayName: z.string().min(1, tCommon('validation.displayNameRequired')).max(50),
        friendListVisibility: z.enum(['Everyone', 'FriendsOnly', 'NoOne']),
      }),
    [tCommon]
  )

  const friendListVisibilityOptions: { value: 'Everyone' | 'FriendsOnly' | 'NoOne'; label: string }[] = [
    { value: 'Everyone', label: t('edit.visibilityEveryone') },
    { value: 'FriendsOnly', label: t('edit.visibilityFriendsOnly') },
    { value: 'NoOne', label: t('edit.visibilityNoOne') },
  ]

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      displayName: user?.displayName ?? '',
      friendListVisibility: user?.friendListVisibility ?? 'Everyone',
    },
  })

  useEffect(() => {
    if (user) {
      form.reset({
        displayName: user.displayName ?? '',
        friendListVisibility: user.friendListVisibility,
      })
      setSeed(user.avatarSeed ?? randomAvatarSeed())
    }
  }, [user, form])

  if (!user) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    )
  }

  async function onSubmit(values: FormValues) {
    try {
      await updateProfile.mutateAsync({
        displayName: values.displayName,
        avatarSeed: seed,
        friendListVisibility: values.friendListVisibility,
      })
      await refetchUser()
      toast.success(t('edit.successToast'))
      navigate(`/u/${user!.username}`)
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('edit.errorToast'))
      }
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <h1 className="mb-1 text-xl font-semibold">{t('edit.title')}</h1>
      <p className="text-muted-foreground mb-6 text-sm">
        {t('edit.usernamePermanent', { username: `u/${user.username}` })}
      </p>

      <div className="mb-6 flex items-center gap-4">
        <UserAvatar seed={seed} username={user.username} className="size-16" />
        <Button type="button" variant="outline" size="sm" onClick={() => setSeed(randomAvatarSeed())}>
          {t('edit.shuffleAvatar')}
        </Button>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <FormField
            control={form.control}
            name="displayName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('edit.displayNameLabel')}</FormLabel>
                <FormControl>
                  <Input {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="friendListVisibility"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('edit.friendListVisibilityLabel')}</FormLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {friendListVisibilityOptions.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormDescription>{t('edit.friendListVisibilityHint')}</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={updateProfile.isPending}>
            {updateProfile.isPending ? t('edit.saving') : t('edit.saveChanges')}
          </Button>
        </form>
      </Form>
    </div>
  )
}
