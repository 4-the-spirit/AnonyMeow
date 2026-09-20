import { useMemo, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { emailSchema, newPasswordSchema } from '@/lib/validation'
import { applyServerErrors } from '@/lib/api/problemDetails'
import { useAuth } from '@/features/auth/useAuth'
import { nativeAuthApi } from '@/features/auth/nativeAuthApi'
import { AuthLayout } from '../components/AuthLayout'

type Step =
  | { step: 'email' }
  | { step: 'code'; continuationToken: string; maskedEmail: string | null }
  | { step: 'newPassword'; continuationToken: string }

export function ForgotPasswordPage() {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const { completeSession } = useAuth()
  const navigate = useNavigate()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [state, setState] = useState<Step>({ step: 'email' })

  const emailFormSchema = useMemo(() => z.object({ email: emailSchema(tCommon) }), [tCommon])
  const emailForm = useForm<{ email: string }>({
    resolver: zodResolver(emailFormSchema),
    defaultValues: { email: '' },
  })
  const codeForm = useForm<{ code: string }>({ defaultValues: { code: '' } })
  const passwordFormSchema = useMemo(() => z.object({ newPassword: newPasswordSchema(tCommon) }), [tCommon])
  const passwordForm = useForm<{ newPassword: string }>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: { newPassword: '' },
  })

  async function onSubmitEmail(values: { email: string }) {
    setIsSubmitting(true)
    try {
      const challenge = await nativeAuthApi.passwordResetStart(values)
      setState({ step: 'code', continuationToken: challenge.continuationToken, maskedEmail: challenge.maskedEmail })
    } catch (error) {
      if (!applyServerErrors(emailForm.setError, error)) {
        toast.error(t('forgotPassword.errorGeneric'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function onSubmitCode(values: { code: string }) {
    if (state.step !== 'code') {
      return
    }

    setIsSubmitting(true)
    try {
      const result = await nativeAuthApi.passwordResetVerify({
        continuationToken: state.continuationToken,
        code: values.code,
      })
      setState({ step: 'newPassword', continuationToken: result.continuationToken })
    } catch (error) {
      if (!applyServerErrors(codeForm.setError, error)) {
        toast.error(t('forgotPassword.errorGeneric'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function onSubmitNewPassword(values: { newPassword: string }) {
    if (state.step !== 'newPassword') {
      return
    }

    setIsSubmitting(true)
    try {
      const tokens = await nativeAuthApi.passwordResetComplete({
        continuationToken: state.continuationToken,
        newPassword: values.newPassword,
      })
      await completeSession({ token: tokens.idToken, oid: tokens.oid, refreshToken: tokens.refreshToken ?? undefined })
      navigate('/')
    } catch (error) {
      if (!applyServerErrors(passwordForm.setError, error)) {
        toast.error(t('forgotPassword.errorGeneric'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout>
      {state.step === 'email' && (
        <div className="flex flex-col gap-4">
          <div>
            <h1 className="text-xl font-semibold">{t('forgotPassword.emailStep.title')}</h1>
            <p className="text-muted-foreground text-sm">{t('forgotPassword.emailStep.intro')}</p>
          </div>
          <Form {...emailForm}>
            <form onSubmit={emailForm.handleSubmit(onSubmitEmail)} className="flex flex-col gap-4">
              <FormField
                control={emailForm.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('emailAuthPage.emailLabel')}</FormLabel>
                    <FormControl>
                      <Input type="email" autoComplete="email" autoFocus {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('forgotPassword.emailStep.submitting') : t('forgotPassword.emailStep.submit')}
              </Button>
            </form>
          </Form>
        </div>
      )}

      {state.step === 'code' && (
        <div className="flex flex-col gap-4">
          <div>
            <h1 className="text-xl font-semibold">{t('forgotPassword.codeStep.title')}</h1>
            <p className="text-muted-foreground text-sm">
              {state.maskedEmail
                ? t('forgotPassword.codeStep.introWithEmail', { email: state.maskedEmail })
                : t('forgotPassword.codeStep.intro')}
            </p>
          </div>
          <Form {...codeForm}>
            <form onSubmit={codeForm.handleSubmit(onSubmitCode)} className="flex flex-col gap-4">
              <FormField
                control={codeForm.control}
                name="code"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('forgotPassword.codeStep.codeLabel')}</FormLabel>
                    <FormControl>
                      <Input inputMode="numeric" autoComplete="one-time-code" autoFocus {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('forgotPassword.codeStep.submitting') : t('forgotPassword.codeStep.submit')}
              </Button>
            </form>
          </Form>
        </div>
      )}

      {state.step === 'newPassword' && (
        <div className="flex flex-col gap-4">
          <div>
            <h1 className="text-xl font-semibold">{t('forgotPassword.passwordStep.title')}</h1>
            <p className="text-muted-foreground text-sm">{t('forgotPassword.passwordStep.intro')}</p>
          </div>
          <Form {...passwordForm}>
            <form onSubmit={passwordForm.handleSubmit(onSubmitNewPassword)} className="flex flex-col gap-4">
              <FormField
                control={passwordForm.control}
                name="newPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('forgotPassword.passwordStep.newPasswordLabel')}</FormLabel>
                    <FormControl>
                      <Input type="password" autoComplete="new-password" autoFocus {...field} />
                    </FormControl>
                    <FormDescription>{tCommon('validation.passwordRequirements')}</FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('forgotPassword.passwordStep.submitting') : t('forgotPassword.passwordStep.submit')}
              </Button>
            </form>
          </Form>
        </div>
      )}
    </AuthLayout>
  )
}
