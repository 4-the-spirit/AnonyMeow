import { useMemo, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { emailSchema, passwordSchema, newPasswordSchema } from '@/lib/validation'
import { ApiError, applyServerErrors } from '@/lib/api/problemDetails'
import { useAuth } from '@/features/auth/useAuth'
import { nativeAuthApi, type OtpChallenge } from '@/features/auth/nativeAuthApi'
import { AuthLayout } from '../components/AuthLayout'

interface EmailAuthPageProps {
  defaultTab?: 'signIn' | 'signUp'
}

/**
 * Real email/password entry point (replaces the old MSAL popup, B2CAuthPage) — talks to the
 * backend's native-auth proxy endpoints (Endpoints/NativeAuthEndpoints.cs), which in turn call
 * Microsoft Entra External ID's native authentication API server-to-server (that API doesn't
 * support CORS, so it can't be called directly from here — see docs/azure-ciam-setup.md).
 */
export function EmailAuthPage({ defaultTab = 'signIn' }: EmailAuthPageProps) {
  const { t } = useTranslation('auth')

  return (
    <AuthLayout>
      <Tabs defaultValue={defaultTab}>
        <TabsList className="w-full">
          <TabsTrigger value="signIn">{t('emailAuthPage.signInTab')}</TabsTrigger>
          <TabsTrigger value="signUp">{t('emailAuthPage.signUpTab')}</TabsTrigger>
        </TabsList>
        <SignInTabContent />
        <SignUpTabContent />
      </Tabs>
    </AuthLayout>
  )
}

type CredentialsValues = { email: string; password: string }

/**
 * `passwordRule: 'new'` (sign-up) enforces Microsoft Entra's complexity rule client-side, not
 * just length: sign-in must stay length-only since an existing password may predate that rule.
 */
function useCredentialsForm(passwordRule: 'existing' | 'new' = 'existing') {
  const { t: tCommon } = useTranslation('common')
  const schema = useMemo(
    () =>
      z.object({
        email: emailSchema(tCommon),
        password: passwordRule === 'new' ? newPasswordSchema(tCommon) : passwordSchema(tCommon),
      }),
    [tCommon, passwordRule]
  )
  return useForm<CredentialsValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })
}

function SignInTabContent() {
  const { t } = useTranslation('auth')
  const { completeSession } = useAuth()
  const navigate = useNavigate()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const form = useCredentialsForm()

  async function onSubmit(values: CredentialsValues) {
    setIsSubmitting(true)
    try {
      const tokens = await nativeAuthApi.signInStart(values)
      await completeSession({ token: tokens.idToken, oid: tokens.oid, refreshToken: tokens.refreshToken ?? undefined })
      navigate('/')
    } catch (error) {
      if (!applyServerErrors(form.setError, error)) {
        toast.error(t('emailAuthPage.signIn.errorGeneric'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <TabsContent value="signIn" className="flex flex-col gap-4 pt-2">
      <div>
        <h1 className="text-xl font-semibold">{t('emailAuthPage.signIn.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('emailAuthPage.signIn.intro')}</p>
      </div>
      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <FormField
            control={form.control}
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
          <FormField
            control={form.control}
            name="password"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('emailAuthPage.passwordLabel')}</FormLabel>
                <FormControl>
                  <Input type="password" autoComplete="current-password" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t('emailAuthPage.signIn.submitting') : t('emailAuthPage.signIn.submit')}
          </Button>
          <Link to="/forgot-password" className="text-muted-foreground text-center text-sm hover:underline">
            {t('emailAuthPage.forgotPasswordLink')}
          </Link>
        </form>
      </Form>
    </TabsContent>
  )
}

type SignUpStep = { step: 'credentials' } | { step: 'verifyEmail'; challenge: OtpChallenge }

function SignUpTabContent() {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const { completeSession } = useAuth()
  const navigate = useNavigate()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [state, setState] = useState<SignUpStep>({ step: 'credentials' })
  const [emailConflict, setEmailConflict] = useState(false)
  const credentialsForm = useCredentialsForm('new')
  const codeForm = useForm<{ code: string }>({ defaultValues: { code: '' } })

  async function onSubmitCredentials(values: CredentialsValues) {
    setIsSubmitting(true)
    setEmailConflict(false)
    try {
      const result = await nativeAuthApi.signUpStart(values)
      if (result.nextStep === 'completed' && result.tokens) {
        await completeSession({
          token: result.tokens.idToken,
          oid: result.tokens.oid,
          refreshToken: result.tokens.refreshToken ?? undefined,
        })
        navigate('/')
        return
      }

      if (result.nextStep === 'verifyEmail' && result.otpChallenge) {
        setState({ step: 'verifyEmail', challenge: result.otpChallenge })
      }
    } catch (error) {
      // The backend already retries a matching sign-up as a sign-in server-side (an email can be
      // "claimed" by an incomplete prior attempt even without password match), so a 409 here means
      // that fallback didn't apply — most commonly a wrong password for an existing account. Point
      // at password reset rather than leaving it a dead end.
      if (error instanceof ApiError && error.status === 409) {
        setEmailConflict(true)
      }
      if (!applyServerErrors(credentialsForm.setError, error)) {
        toast.error(t('emailAuthPage.signUp.errorGeneric'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function onSubmitCode(values: { code: string }) {
    if (state.step !== 'verifyEmail') {
      return
    }

    setIsSubmitting(true)
    try {
      const tokens = await nativeAuthApi.signUpVerifyEmail({
        continuationToken: state.challenge.continuationToken,
        code: values.code,
      })
      await completeSession({ token: tokens.idToken, oid: tokens.oid, refreshToken: tokens.refreshToken ?? undefined })
      navigate('/')
    } catch (error) {
      if (!applyServerErrors(codeForm.setError, error)) {
        toast.error(t('emailAuthPage.signUp.errorGeneric'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (state.step === 'verifyEmail') {
    return (
      // key forces a fresh mount instead of React reconciling this subtree in place against the
      // credentials-step form below — without it, react-hook-form's Controller for "code" reused
      // the previous render's Controller instance (still wired to credentialsForm's "email" field
      // at the same tree position) instead of registering against codeForm, so onChange fired but
      // never reached any live form state and the input silently refused to accept typed input.
      <TabsContent key="verifyEmail" value="signUp" className="flex flex-col gap-4 pt-2">
        <div>
          <h1 className="text-xl font-semibold">{t('emailAuthPage.verifyEmail.title')}</h1>
          <p className="text-muted-foreground text-sm">
            {state.challenge.maskedEmail
              ? t('emailAuthPage.verifyEmail.introWithEmail', { email: state.challenge.maskedEmail })
              : t('emailAuthPage.verifyEmail.intro')}
          </p>
        </div>
        <Form {...codeForm}>
          <form onSubmit={codeForm.handleSubmit(onSubmitCode)} className="flex flex-col gap-4">
            <FormField
              control={codeForm.control}
              name="code"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('emailAuthPage.verifyEmail.codeLabel')}</FormLabel>
                  <FormControl>
                    <Input inputMode="numeric" autoComplete="one-time-code" autoFocus {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? t('emailAuthPage.verifyEmail.submitting') : t('emailAuthPage.verifyEmail.submit')}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setState({ step: 'credentials' })}>
              {t('emailAuthPage.verifyEmail.back')}
            </Button>
          </form>
        </Form>
      </TabsContent>
    )
  }

  return (
    <TabsContent key="credentials" value="signUp" className="flex flex-col gap-4 pt-2">
      <div>
        <h1 className="text-xl font-semibold">{t('emailAuthPage.signUp.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('emailAuthPage.signUp.intro')}</p>
      </div>
      <Form {...credentialsForm}>
        <form onSubmit={credentialsForm.handleSubmit(onSubmitCredentials)} className="flex flex-col gap-4">
          <FormField
            control={credentialsForm.control}
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
          <FormField
            control={credentialsForm.control}
            name="password"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('emailAuthPage.passwordLabel')}</FormLabel>
                <FormControl>
                  <Input type="password" autoComplete="new-password" {...field} />
                </FormControl>
                <FormDescription>{tCommon('validation.passwordRequirements')}</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t('emailAuthPage.signUp.submitting') : t('emailAuthPage.signUp.submit')}
          </Button>
          {emailConflict && (
            <Link to="/forgot-password" className="text-muted-foreground text-center text-sm hover:underline">
              {t('emailAuthPage.signUp.emailConflictHint')}
            </Link>
          )}
        </form>
      </Form>
    </TabsContent>
  )
}
