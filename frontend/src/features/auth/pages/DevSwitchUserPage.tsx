import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { useAuth } from '@/features/auth/useAuth'

/**
 * Dev-only escape hatch: Phase 1 has no real login UI, and moderation testing requires
 * acting as two different users (a regular member and a community moderator) in the
 * same browser session. Mints a token for an arbitrary oid via POST /api/dev/token.
 */
export function DevSwitchUserPage() {
  const { user, devLoginAs } = useAuth()
  const [oid, setOid] = useState('')
  const [isSwitching, setIsSwitching] = useState(false)
  const navigate = useNavigate()

  async function handleSwitch(targetOid?: string) {
    setIsSwitching(true)
    try {
      await devLoginAs(targetOid || undefined)
      toast.success('Switched user')
      navigate('/')
    } catch {
      toast.error('Could not switch user')
    } finally {
      setIsSwitching(false)
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <Card>
        <CardHeader>
          <CardTitle>Switch dev user</CardTitle>
          <CardDescription>
            Dev-only tool. Mints a new local token for the given oid (or a brand new
            random user if left blank) via the backend's <code>/api/dev/token</code>{' '}
            bypass.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <p className="text-muted-foreground text-sm">
            Currently signed in as <strong>{user?.username ?? '—'}</strong>
          </p>
          <div className="flex flex-col gap-2">
            <Label htmlFor="oid">oid (leave blank for a brand new user)</Label>
            <Input
              id="oid"
              value={oid}
              onChange={(e) => setOid(e.target.value)}
              placeholder="e.g. moderator-1"
            />
          </div>
          <div className="flex gap-2">
            <Button onClick={() => handleSwitch(oid)} disabled={isSwitching}>
              Switch to this oid
            </Button>
            <Button variant="outline" onClick={() => handleSwitch()} disabled={isSwitching}>
              New random user
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
