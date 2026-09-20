# Microsoft Entra External ID (CIAM) setup for email/password sign-in

This is a manual, one-time checklist for provisioning the Entra External ID (CIAM) tenant this
app's real auth depends on. Claude has no Azure CLI/portal access in this environment and this is
a shared, live resource, so this step can't be automated — do it in the Microsoft Entra admin
center, then hand the resulting values back so they can be wired into config.

This app previously targeted classic Azure AD B2C, but B2C stopped accepting new tenants on
May 1, 2025 — see `Execution Log/2026-07-30-deploy-to-azure-with-entra-external-id.md` for the
migration. It then briefly used a Microsoft-hosted popup page (MSAL, redirect-based) before
switching to Entra's **native authentication** API — a fully custom-branded email/password form
built into this app's own frontend, with the account still genuinely stored in the CIAM tenant.
See `Execution Log/2026-08-08-native-auth-custom-form.md` for that switch and why (native auth's
REST endpoints don't support CORS, so the ASP.NET Core backend proxies them server-to-server —
`Services/NativeAuth/` and `Endpoints/NativeAuthEndpoints.cs`).

## 1. Tenant

If you don't already have one: create an external tenant (separate from any regular Entra ID
tenant) in the Microsoft Entra admin center. Each external tenant gets its own dedicated
`<subdomain>.ciamlogin.com` hostname — that hostname, not a shared endpoint, is what the app
authenticates against.

## 2. App registration

In the external tenant, create one app registration, then under **Authentication**:
- Enable **"Allow public client flows"**.
- Enable **"Native authentication"** (this is what unlocks the `/signup/v1.0/*`,
  `/oauth2/v2.0/initiate`, etc. endpoints `Services/NativeAuth/NativeAuthClient.cs` calls — without
  it those calls fail with `invalid_client` / `nativeauthapi_disabled`).

No SPA platform / redirect URI is needed — there's no browser popup or redirect anymore, so the
app registration doesn't need a registered redirect URI at all for this flow.

## 3. User flow

Create a **sign-up and sign-in** user flow and add this app registration to it:
- Identity providers: **Email with password**.
- User attributes / claims: collect only what's essential — **leave "Display Name" unchecked**.
  `CompleteProfilePage` already collects a display name in-app after sign-up; if the user flow
  also demands it, the sign-up API returns an `attributes_required` step this app doesn't have UI
  for, and the backend surfaces a clear "check the CIAM user flow" error instead of silently
  breaking sign-up.
- Password reset is included automatically in this combined flow — no separate policy to manage.
- Google (or any other federated identity provider) isn't supported by native authentication —
  only **Email with password** and **Email one-time passcode** are. Don't enable Google on this
  user flow; it wouldn't be reachable from this app's UI.

## 4. Values to hand back

| Value | Where it's used |
|---|---|
| Tenant subdomain (e.g. `anonymeow`) | part of `AzureAdB2COptions:Instance` |
| App registration's Client ID | `AzureAdB2COptions:ClientId` |

That's it — only two values, both backend-only. `Services/NativeAuth`'s `NativeAuthOptions` (base
URL + client ID for the native-auth REST calls) is derived from these same two `AzureAdB2COptions`
values at startup (see `Program.cs`), so there's no separate config section, and the **frontend
needs no CIAM values at all** — it only reads one boolean, `VITE_REAL_AUTH_ENABLED`, to decide
whether to show the real email/password UI (`EmailAuthPage`/`ForgotPasswordPage`) instead of the
dev-only bypass (`AuthPage`, `/api/dev/token`).

Where these go:
- **Backend**: `AzureAdB2COptions` in `dotnet user-secrets` (local) or Azure App Configuration /
  App Service settings (staging/prod) — never in `appsettings.json`, per CLAUDE.md.
  `Instance` is `https://{subdomain}.ciamlogin.com` (no domain or policy path segment, no
  `/v2.0` suffix — CIAM's authority is just the tenant's dedicated hostname).
- **Frontend**: set `VITE_REAL_AUTH_ENABLED=true` in the deployed environment's config (GitHub
  Actions secret for the Static Web Apps workflow) — see `.env.example`. Leave it unset locally to
  keep using the dev-only bypass.

## 5. Verification once configured

- Backend: `dotnet build` / `dotnet test` (the new `Services/NativeAuth`/`NativeAuthEndpoints`
  tests fake the outbound HTTP call, so they pass regardless of real tenant values — only true
  end-to-end verification needs the real tenant, below).
- Postman/curl against the deployed backend once the tenant is configured: `POST
  /api/auth/native/signup/start` with a real disposable email + password, then
  `/api/auth/native/signup/verify-email` with the code emailed to that address — confirms the
  whole proxy chain reaches the real tenant.
- Frontend: sign up with a real email in a browser, confirm the account lands on
  "complete your profile", sign out, sign back in, then use "Forgot your password?" and confirm
  it returns to a signed-in session with the new password.
- This end-to-end check against a real tenant is the one piece that couldn't be done while
  implementing the code side of this feature — see the Execution Log entry for exactly what was
  and wasn't verified.
