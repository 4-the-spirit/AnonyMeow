# AnonyMeow — Azure infrastructure (production)

Bicep templates for the production environment: App Service (backend), Static Web App
(frontend), PostgreSQL Flexible Server, Storage Account (blob images), Key Vault, and
optional Application Insights. See `Execution Log/` and the root `README.md`/`CLAUDE.md`
for broader context.

**This is the one step in the deployment that touches live/shared Azure resources — run it
yourself, review the `what-if` output first, and confirm before applying, per CLAUDE.md's
infra-change policy. Claude has no Azure CLI/portal access in this environment and does not
run any of the commands below.**

## Prerequisites

- Azure CLI (`az`), logged in (`az login`) with a subscription selected (`az account set --subscription <id>`).
- A resource group already created, e.g.:
  ```bash
  az group create --name rg-anonymeow-prod --location eastus2
  ```
- The Microsoft Entra External ID (CIAM) values from `docs/azure-ciam-setup.md` (tenant
  provisioning is a separate manual step — do that first or in parallel; the App Service will
  simply reject real tokens until real values are wired in, but that doesn't block infra
  provisioning).
- A PostgreSQL administrator password you'll supply at deploy time (never commit it).

## Secrets: how to supply them

Two values must never be committed:

- `postgresAdminPassword` (truly secret)
- optionally, the two `azureAdB2C*` values if you'd rather not have them on your shell
  history / CI logs (they aren't secret in the security sense — `ClientId` etc. are public
  identifiers — but keeping them out of the committed parameters file avoids editing a
  tracked file per environment change)

Pass them at deploy time via `--parameters key=value`, or create a **gitignored** file
`infra/main.parameters.prod.secrets.json` (add it to `.gitignore` before creating it) with
the same shape as `main.parameters.prod.json` and reference both files together.

## Deploy

Always review with `what-if` before applying:

```bash
az deployment group what-if \
  --resource-group rg-anonymeow-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.prod.json \
  --parameters postgresAdminPassword='<your-password>' \
  --parameters azureAdB2CInstance='https://<tenant>.ciamlogin.com' \
  --parameters azureAdB2CClientId='<client-id>'
```

Once reviewed and confirmed:

```bash
az deployment group create \
  --resource-group rg-anonymeow-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.prod.json \
  --parameters postgresAdminPassword='<your-password>' \
  --parameters azureAdB2CInstance='https://<tenant>.ciamlogin.com' \
  --parameters azureAdB2CClientId='<client-id>'
```

This provisions everything in one pass: App Service Plan + App Service, PostgreSQL
Flexible Server + database + firewall rule, Storage Account + container + CORS, Key
Vault + secrets + the App Service's read access to them, a bare Static Web App, and
(unless `deployApplicationInsights=false`) Log Analytics + Application Insights.

## After the first deploy

1. **Note the outputs** — `appServiceHostName`, `staticWebAppHostName`, etc.
   ```bash
   az deployment group show -g rg-anonymeow-prod -n main --query properties.outputs
   ```
2. **Link the Static Web App to GitHub** (one-time, not managed by this Bicep — SWA/GitHub
   linking and Bicep don't combine cleanly):
   ```bash
   az staticwebapp create \
     --name <staticWebAppName-from-outputs> \
     --resource-group rg-anonymeow-prod \
     --source https://github.com/<org>/<repo> \
     --branch master \
     --app-location frontend \
     --output-location dist \
     --login-with-github
   ```
   This auto-generates and commits `.github/workflows/azure-static-web-apps-<id>.yml` and
   an `AZURE_STATIC_WEB_APPS_API_TOKEN_*` GitHub secret. **Edit that generated workflow
   once** to inject the two `VITE_*` build-time env vars (see the root
   `.github/workflows/` — there is no hand-written frontend deploy workflow in this repo by
   design). Re-running this command later (e.g. to relink) can regenerate the workflow file
   and silently drop that edit — check the diff before committing if you ever re-run it.
3. **Populate GitHub Actions secrets** used by `backend-deploy.yml`:
   `PROD_DB_CONNECTION_STRING`, `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` /
   `AZURE_SUBSCRIPTION_ID` (OIDC federated credential — see below), plus the SWA workflow's
   two `VITE_*` secrets.
4. **Configure OIDC federation** for GitHub Actions → Azure (avoids a long-lived publish
   profile secret): create an App Registration, add a federated credential scoped to
   `repo:<org>/<repo>:environment:production`, and grant it `Contributor` (or a narrower
   custom role) on the resource group.
5. **Create the `production` GitHub Environment** (repo Settings → Environments) with
   required reviewers — this is the human approval gate for `backend-deploy.yml`'s
   migration step.

## Custom domain (apex, no www)

Production is meant to live at the bare apex domain `anonymeow.com` — **not**
`www.anonymeow.com`. Apex domains need different DNS records than a `www` subdomain would,
so this is its own step (same policy as everything else in this file: manual, `az`-CLI-driven,
run by you, not Claude):

1. Add the apex hostname to the Static Web App:
   ```bash
   az staticwebapp hostname set \
     --name <staticWebAppName-from-outputs> \
     --resource-group rg-anonymeow-prod \
     --hostname anonymeow.com
   ```
2. At your DNS registrar, add the records Azure's validation step asks for:
   - A **TXT** record (`asuid.anonymeow.com`) proving ownership — Azure shows the exact value
     in the Portal/CLI output when you add the hostname.
   - An **ALIAS/ANAME** record (or your provider's CNAME-flattening feature) at the zone apex
     pointing to the Static Web App's default hostname. A plain **CNAME cannot be used at a
     bare apex** per DNS spec — that's the whole reason `www` is normally the easy path and the
     apex needs this extra step. Most modern DNS providers (Cloudflare, Route 53, etc.) support
     one of these.
3. Wait for DNS propagation and Azure's domain validation; a managed TLS certificate is
   auto-issued once it passes.
4. If `www.anonymeow.com` was already added as a hostname from an earlier setup, remove it only
   after confirming the apex is live and serving correctly:
   ```bash
   az staticwebapp hostname delete \
     --name <staticWebAppName-from-outputs> \
     --resource-group rg-anonymeow-prod \
     --hostname www.anonymeow.com
   ```
5. Re-run the main deployment with the CORS override pointed at the apex domain, so the
   backend allows the new origin:
   ```bash
   az deployment group create \
     --resource-group rg-anonymeow-prod \
     --template-file infra/main.bicep \
     --parameters infra/main.parameters.prod.json \
     --parameters postgresAdminPassword='<your-password>' \
     --parameters azureAdB2CInstance='https://<tenant>.ciamlogin.com' \
     --parameters azureAdB2CClientId='<client-id>' \
     --parameters corsFrontendOriginOverride='https://anonymeow.com'
   ```
   (`corsFrontendOriginOverride` flows through `modules/appService.bicep`'s
   `corsFrontendOrigin` param into the `Cors:FrontendOrigin` setting read in
   `backend/Program.cs`.)

## Notes

- `linuxFxVersion: 'DOTNETCORE|10.0'` in `modules/appService.bicep` assumes that exact
  runtime stack name is available on App Service; verify with
  `az webapp list-runtimes --os linux` and adjust if .NET 10's stack identifier differs.
- No staging slot in this v1 — rollback means redeploying a previous commit via
  `backend-deploy.yml`, not a slot swap.
- Re-running `az deployment group create` is safe (idempotent) for config changes; always
  `what-if` first, especially before changing SKUs or anything that could trigger a
  resource replacement.
