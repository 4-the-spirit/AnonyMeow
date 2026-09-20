import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ApiError } from '@/lib/api/problemDetails'
import { useAuth } from '@/features/auth/useAuth'
import { AuthLayout } from '../components/AuthLayout'

interface AuthPageProps {
  defaultTab?: 'signIn' | 'signUp'
}

export function AuthPage({ defaultTab = 'signIn' }: AuthPageProps) {
  const { t } = useTranslation('auth')
  const { signInWithUsername, signUp } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [isSigningIn, setIsSigningIn] = useState(false)
  const [isSigningUp, setIsSigningUp] = useState(false)

  async function handleSignIn(e: React.FormEvent) {
    e.preventDefault()
    if (!username.trim()) return
    setIsSigningIn(true)
    try {
      await signInWithUsername(username.trim())
      navigate('/')
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        toast.error(t('authPage.signIn.notFound'))
      } else {
        toast.error(t('authPage.signIn.errorGeneric'))
      }
    } finally {
      setIsSigningIn(false)
    }
  }

  async function handleSignUp() {
    setIsSigningUp(true)
    try {
      await signUp()
      navigate('/')
    } catch {
      toast.error(t('authPage.signUp.errorGeneric'))
    } finally {
      setIsSigningUp(false)
    }
  }

  return (
    <AuthLayout>
      <Tabs defaultValue={defaultTab}>
        <TabsList className="w-full">
          <TabsTrigger value="signIn">{t('authPage.signInTab')}</TabsTrigger>
          <TabsTrigger value="signUp">{t('authPage.signUpTab')}</TabsTrigger>
        </TabsList>

        <TabsContent value="signIn" className="flex flex-col gap-4 pt-2">
          <div>
            <h1 className="text-xl font-semibold">{t('authPage.signIn.title')}</h1>
            <p className="text-muted-foreground text-sm">{t('authPage.signIn.intro')}</p>
          </div>
          <form onSubmit={handleSignIn} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="signin-username">{t('authPage.signIn.usernameLabel')}</Label>
              <Input
                id="signin-username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder={t('authPage.signIn.usernamePlaceholder')}
                autoFocus
              />
            </div>
            <Button type="submit" disabled={isSigningIn || !username.trim()}>
              {isSigningIn ? t('authPage.signIn.submitting') : t('authPage.signIn.submit')}
            </Button>
          </form>
        </TabsContent>

        <TabsContent value="signUp" className="flex flex-col gap-4 pt-2">
          <div>
            <h1 className="text-xl font-semibold">{t('authPage.signUp.title')}</h1>
            <p className="text-muted-foreground text-sm">{t('authPage.signUp.intro')}</p>
          </div>
          <Button onClick={handleSignUp} disabled={isSigningUp}>
            {isSigningUp ? t('authPage.signUp.submitting') : t('authPage.signUp.submit')}
          </Button>
        </TabsContent>
      </Tabs>
    </AuthLayout>
  )
}
