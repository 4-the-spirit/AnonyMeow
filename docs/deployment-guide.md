# Deploying AnonyMeow to Azure — step-by-step guide

This is a first-deployment walkthrough. It assumes you've never done this before and
explains not just the commands but *why* each step exists. Everything code/config-side
(Bicep templates, CI/CD workflows, the backend health check, the frontend's SWA config)
already exists in the repo — this guide is about the parts only you can do: Azure Portal
actions, credentials, and running the Azure CLI from a machine that has access to it (this
AI assistant does not have Azure CLI/portal access, so none of the commands below can be
run on your behalf — you or a connected Azure MCP tool need to run them).

**Where things live in the repo**, so you can jump straight to source-of-truth files:
- `infra/` — Bicep templates + `infra/README.md` (terse command reference)
- [`azure-ciam-setup.md`](azure-ciam-setup.md) — the authoritative CIAM tenant checklist (Step 1 below summarizes it)
- `.github/workflows/` — CI/CD (`backend-ci.yml`, `frontend-ci.yml`, `backend-deploy.yml`)
- `Execution Log/2026-07-27-azure-deployment-configuration.md` — what was built and verified so far

## What gets created

One Azure resource group containing: an App Service (runs the backend API), a Static Web
App (hosts the built React frontend), a PostgreSQL Flexible Server (the database), a
Storage Account (post images), a Key Vault (holds secrets), and optionally Application
Insights (telemetry). Roughly: App Service Basic tier (~$13/mo), PostgreSQL Burstable
B1ms (~$15/mo), Storage/Key Vault/App Insights are pennies at low usage, Static Web Apps
Free tier is $0. Ballpark **$25-35/month** total, before any real traffic.

## Prerequisites

1. **An Azure subscription** with permission to create resources (Owner or Contributor).
2. **A GitHub account** with admin access to this repo (to add secrets, link Static Web
   Apps, and configure environments).
3. **Azure CLI installed**, on whatever machine you'll run these commands from:
   - Windows: `winget install Microsoft.AzureCLI` (or download from
     https://learn.microsoft.com/cli/azure/install-azure-cli-windows)
   - Confirm it worked: `az version`
4. **Sign in**:
   ```bash
   az login
   ```
   This opens a browser to sign in. If you have more than one subscription, pick the right one:
   ```bash
   az account list --output table
   az account set --subscription "<subscription name or id>"
   ```

Everything below assumes you're running commands from the repo root, in a shell logged
into `az` as above.

---

## Step 1 — Provision the Microsoft Entra External ID (CIAM) tenant (authentication)

This is the identity provider — without it, nobody can sign in on the deployed site. Full
detail is in [`azure-ciam-setup.md`](azure-ciam-setup.md); here's the condensed version:

1. In the Microsoft Entra admin center, create a new **external tenant** (separate from any
   regular Entra ID/Azure AD tenant you might have) and link it to your subscription. Each
   external tenant gets its own dedicated `<subdomain>.ciamlogin.com` hostname — that's what
   the app authenticates against, not a shared endpoint.
2. Inside that tenant, go to **App registrations** → **New registration**. Create
   **one** registration used for both the frontend and the backend, then under
   **Authentication**:
   - Enable **"Allow public client flows"**.
   - Enable **"Native authentication"**.

   No SPA platform or redirect URI is needed — sign-in is a custom form built into this
   app's own frontend (`EmailAuthPage`/`ForgotPasswordPage`), proxied through the backend,
   not a browser popup/redirect, so there's no redirect URI to register.
3. Create a **sign-up and sign-in** user flow (email with password) and add this app
   registration to it. Under **User attributes / claims**, leave "Display Name" unchecked —
   this app collects it in its own "complete your profile" step after sign-up, not via the
   CIAM user flow. Password reset is included automatically in the combined flow — no
   separate policy to manage.
4. **Write down these 2 values** — you'll need them twice (once for the Bicep deploy in
   Step 3, once for GitHub secrets in Step 8):

   | Value | Example | Where to find it |
   |---|---|---|
   | Tenant subdomain | `anonymeow` | Your tenant's name, before `.onmicrosoft.com` |
   | Client ID | `a1b2c3d4-...` | The app registration's "Application (client) ID" |

Both are backend-only config — the frontend needs no CIAM values at all (see
[`azure-ciam-setup.md`](azure-ciam-setup.md)). You can do this step in parallel with Step 2-3 below.

---

## Step 2 — Create the resource group

```bash
az group create --name rg-anonymeow-prod --location eastus2
```

`eastus2` is used because it's one of the regions that supports Azure Static Web Apps —
keep this region choice for the rest of the guide unless you have a reason to change it
(if you do, see the `location` vs `staticWebAppLocation` note in Step 3).

---

## Step 3 — Deploy the infrastructure (Bicep)

This one step provisions everything: App Service, PostgreSQL, Storage, Key Vault, and a
bare Static Web App. **This is the step that touches real, billable, live Azure
resources** — always review with `what-if` before applying.

1. **Pick a PostgreSQL admin password.** Generate a strong one and save it somewhere safe
   (a password manager) — you won't be able to read it back from Azure later.

2. **Review what will be created** (nothing is created yet, this is a dry run):
   ```bash
   az deployment group what-if \
     --resource-group rg-anonymeow-prod \
     --template-file infra/main.bicep \
     --parameters infra/main.parameters.prod.json \
     --parameters postgresAdminPassword='<your-postgres-password>' \
     --parameters azureAdB2CInstance='https://<tenant-subdomain>.ciamlogin.com' \
     --parameters azureAdB2CClientId='<client-id-from-step-1>'
   ```
   (On Windows PowerShell, replace the trailing `\` line-continuations with a backtick
   `` ` ``, or just put the whole command on one line.)

   Read the output — it lists every resource that will be created. Nothing exists with
   these names yet, so everything should show as "Create", not "Modify"/"Delete".

3. **Apply it** (same command, `create` instead of `what-if`):
   ```bash
   az deployment group create \
     --resource-group rg-anonymeow-prod \
     --template-file infra/main.bicep \
     --parameters infra/main.parameters.prod.json \
     --parameters postgresAdminPassword='<your-postgres-password>' \
     --parameters azureAdB2CInstance='https://<tenant-subdomain>.ciamlogin.com' \
     --parameters azureAdB2CClientId='<client-id-from-step-1>'
   ```
   This takes a few minutes (PostgreSQL server creation is the slow part).

---

## Step 4 — Read the deployment outputs

```bash
az deployment group show \
  --resource-group rg-anonymeow-prod \
  --name main \
  --query properties.outputs
```

You'll get back JSON with `appServiceName`, `appServiceHostName`,
`staticWebAppName`, `staticWebAppHostName`, `keyVaultName`, `postgresServerFqdn`,
`storageAccountName`. Write down `appServiceHostName` and `staticWebAppHostName` — the
next two steps need them (they look like `anonymeow-prod-api.azurewebsites.net` and
`anonymeow-prod-swa.azurestaticapps.net`).

---

## Step 5 — Link the Static Web App to GitHub

The Static Web App resource exists but has no deployment pipeline yet. Linking it to
GitHub makes Azure auto-generate and commit a deploy workflow for you:

```bash
az staticwebapp create \
  --name <staticWebAppName-from-step-4> \
  --resource-group rg-anonymeow-prod \
  --source https://github.com/<your-org-or-username>/<your-repo> \
  --branch master \
  --app-location frontend \
  --output-location dist \
  --login-with-github
```

`--login-with-github` opens a browser prompt to authorize Azure to push a workflow file
to your repo. When it's done, check your repo — you should see a new file at
`.github/workflows/azure-static-web-apps-<random-id>.yml`, and a new repo secret named
something like `AZURE_STATIC_WEB_APPS_API_TOKEN_<RANDOM>` will have been added
automatically. Pull that new workflow file locally (`git pull`) before continuing.

---

## Step 6 — Edit the generated frontend workflow

The workflow Azure just committed builds the frontend, but it doesn't know about the two
`VITE_*` environment variables the build needs (API base URL + the real-auth switch) — Vite
bakes these into the bundle at build time, so they must be present as env vars on the
build step.

Open the new `.github/workflows/azure-static-web-apps-*.yml` file. Find the step that
looks like this (the exact name may vary slightly by generated version):

```yaml
      - name: Build And Deploy
        id: builddeploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_XXXXXXX }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "frontend"
          output_location: "dist"
```

Add an `env:` block to that same step:

```yaml
      - name: Build And Deploy
        id: builddeploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_XXXXXXX }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "frontend"
          output_location: "dist"
        env:
          VITE_API_BASE_URL: ${{ secrets.VITE_API_BASE_URL }}
          VITE_REAL_AUTH_ENABLED: ${{ secrets.VITE_REAL_AUTH_ENABLED }}
```

Commit and push this edit. **Remember this edit exists** — if you ever re-run
`az staticwebapp create --source` against the same resource, Azure may regenerate this
file and silently drop it; check the diff before committing if that happens.

---

## Step 7 — Set up passwordless Azure login for the backend deploy (OIDC)

The backend's deploy workflow (`backend-deploy.yml`) needs permission to deploy to your
App Service and to run database migrations. Rather than a long-lived password, it uses
short-lived OIDC tokens. This needs a one-time App Registration + federated credential:

```bash
# 1. Create an app registration for GitHub Actions to authenticate as
az ad app create --display-name "anonymeow-github-deploy"
# note the "appId" from the output — this is your AZURE_CLIENT_ID

# 2. Create a service principal for it
az ad sp create --id <appId-from-above>

# 3. Tell Azure AD to trust GitHub Actions tokens from your repo's "production" environment
az ad app federated-credential create --id <appId-from-above> --parameters '{
  "name": "github-production-environment",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<your-org-or-username>/<your-repo>:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'

# 4. Grant it permission to deploy into the resource group
az role assignment create \
  --assignee <appId-from-above> \
  --role Contributor \
  --scope /subscriptions/<your-subscription-id>/resourceGroups/rg-anonymeow-prod
```

Get your subscription ID and tenant ID if you don't have them handy:
```bash
az account show --query "{subscriptionId:id, tenantId:tenantId}"
```

You now have three values for the next step: `appId` (→ `AZURE_CLIENT_ID`), `tenantId`
(→ `AZURE_TENANT_ID`), `id`/subscription id (→ `AZURE_SUBSCRIPTION_ID`).

---

## Step 8 — Add GitHub repository secrets and variables

In GitHub: repo → **Settings** → **Secrets and variables** → **Actions**.

**Secrets** (Repository secrets tab — or add them to the `production` Environment
specifically, see Step 9):

| Secret name | Value |
|---|---|
| `PROD_DB_CONNECTION_STRING` | `Host=<postgresServerFqdn-from-step-4>;Port=5432;Database=anonymeow;Username=anonymeowadmin;Password=<your-postgres-password-from-step-3>;Ssl Mode=Require;Trust Server Certificate=true` |
| `AZURE_CLIENT_ID` | the `appId` from Step 7 |
| `AZURE_TENANT_ID` | the tenant ID from Step 7 |
| `AZURE_SUBSCRIPTION_ID` | the subscription ID from Step 7 |
| `VITE_API_BASE_URL` | `https://<appServiceHostName-from-step-4>` |
| `VITE_REAL_AUTH_ENABLED` | `true` |

**Variables** (Variables tab, not secret — this one is just a name, not sensitive):

| Variable name | Value |
|---|---|
| `AZURE_APP_SERVICE_NAME` | the `appServiceName` from Step 4 |

---

## Step 9 — Create the `production` GitHub Environment (the approval gate)

Repo → **Settings** → **Environments** → **New environment** → name it exactly
`production` (the workflow file references this name).

Turn on **Required reviewers** and add yourself (or whoever should approve production
deploys). This is what makes `backend-deploy.yml` pause before running database
migrations against the live database — you'll get a notification to approve each deploy
run before it touches the DB, which is the human review step this project's conventions
require for schema changes.

---

## Step 10 — Trigger the deploys

Everything is wired up. Push to `master` (or merge a PR into it) that touches `backend/`
or `infra/` — this triggers `backend-deploy.yml`: it builds and tests, then **pauses for
your approval** (check the repo's **Actions** tab, click the waiting run, click
**Review deployments** → **Approve**), then runs migrations and deploys.

The frontend deploy workflow (from Step 5) triggers independently on pushes touching
`frontend/`, with no approval gate (it doesn't touch a live database, just static files).

If nothing changed in `backend/` or `frontend/` since your last push (e.g. you're doing
this on a repo that already had the code committed before you set up CI/CD), you can
trigger a run manually from the **Actions** tab, or make a trivial change and push it.

---

## Step 11 — Verify it actually works

1. **Health check**:
   ```bash
   curl https://<appServiceHostName>/health
   ```
   Expect `{"status":"ok","database":"ok"}`.
2. **Sign-up flow**: open `https://<staticWebAppHostName>` in a browser, sign up with a
   real email/password, confirm you land on "complete your profile", sign out, sign back
   in, then try "Forgot your password?" and confirm it works.
3. **A real authenticated action**: after signing in, do something that hits the backend
   (view the feed, create a post) — confirms the deployed frontend and backend can talk to
   each other and that JWT validation against the real CIAM tenant works (this specific
   path can't be tested locally, since local dev uses a fake-token bypass instead of real
   CIAM).
4. **Image upload**: create a post with an image — this exercises the Storage Account CORS
   rule specifically; if it fails while everything else works, that's the first place to
   look.
5. **Notifications**: open two browser sessions (or one incognito), sign in as two
   different users, have one reply to the other's post/comment, confirm a live
   notification arrives (validates the SignalR hub connection).

---

## If something goes wrong

- **`what-if` shows unexpected deletes/replacements**: stop, don't apply — re-check your
  parameters against `infra/main.parameters.prod.json` first; a changed SKU or name can
  force a resource replacement.
- **App Service shows "Application Error" / health check fails**: check
  **App Service → Log stream** in the Portal, or `az webapp log tail --name
  <appServiceName> --resource-group rg-anonymeow-prod`. Common cause: a Key Vault
  reference app setting not resolving — check **App Service → Configuration → Application
  settings**, values referencing Key Vault show a green checkmark when resolved, red X
  when not (usually a role-assignment propagation delay; wait a few minutes and restart
  the App Service).
- **Sign-up/sign-in fails immediately with a generic error**: double check the CIAM authority —
  `AzureAdB2COptions:Instance` must be the bare `https://<subdomain>.ciamlogin.com/` (no domain or
  policy path segment, no `/v2.0` suffix); appending those is a leftover classic-B2C URL shape and
  breaks OIDC discovery.
- **Sign-up/sign-in returns a 503 "Sign-In Temporarily Unavailable"**: the app registration
  doesn't have **"Allow public client flows"** and **"Native authentication"** enabled under
  **Authentication** — see [`azure-ciam-setup.md`](azure-ciam-setup.md) Step 2. The backend logs the raw Microsoft
  error (`invalid_client` / `nativeauthapi_disabled`) server-side; check **App Service → Log
  stream** to confirm.
- **Sign-up fails with a 500 "Sign-Up Configuration Error"**: the CIAM user flow's **User
  attributes / claims** still has "Display Name" (or another attribute) checked — this app's
  sign-up form doesn't collect it. Uncheck it in the user flow (see [`azure-ciam-setup.md`](azure-ciam-setup.md)
  Step 3); `CompleteProfilePage` collects the display name separately, after sign-up.
- **Migration step fails in `backend-deploy.yml`**: check the Action's log output — it
  runs `dotnet ef database update` directly, so EF's own error message will point at the
  actual problem (usually a connection string typo or a firewall rule not yet propagated).
- **Frontend build succeeds but API calls fail with a CORS error**: confirm the App
  Service's `Cors__FrontendOrigin` setting exactly matches `https://<staticWebAppHostName>`
  (Portal → App Service → Configuration).

## Redeploying after this initial setup

Once everything above is done once, ongoing deploys are just: push to `master`, approve
the production environment gate when it appears in the Actions tab. Re-running the Bicep
deployment (`az deployment group create` again) is only needed if you change
`infra/*.bicep` — always `what-if` first, same as Step 3.
