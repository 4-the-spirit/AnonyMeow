targetScope = 'resourceGroup'

@description('Short environment name, used in resource naming')
param environmentName string = 'prod'

@description('Base name for the project, used to build resource names')
param projectName string = 'anonymeow'

@description('Primary Azure region for most resources')
param location string = resourceGroup().location

@description('Azure region for the Static Web App (SWA is only available in a subset of regions)')
param staticWebAppLocation string = 'eastus2'

@description('Azure region for the App Service Plan/App Service (separate from `location` since App Service VM quota availability varies by region/subscription)')
param appServicePlanLocation string = location

@description('PostgreSQL Flexible Server administrator username')
param postgresAdminUsername string = 'anonymeowadmin'

@secure()
@description('PostgreSQL Flexible Server administrator password. Supply at deploy time, never commit.')
param postgresAdminPassword string

@description('Microsoft Entra External ID (CIAM) instance, e.g. https://<tenant>.ciamlogin.com (see docs/azure-ciam-setup.md)')
param azureAdB2CInstance string

@description('Microsoft Entra External ID (CIAM) app registration Client ID')
param azureAdB2CClientId string

@description('Frontend origin allowed by backend CORS. Leave empty to default to the Static Web App default hostname (fine for v1, no custom domain).')
param corsFrontendOriginOverride string = ''

param appServicePlanSkuName string = 'B1'
param appServicePlanSkuTier string = 'Basic'
param postgresSkuName string = 'Standard_B1ms'
param postgresSkuTier string = 'Burstable'
param blobContainerName string = 'post-images'
param deployApplicationInsights bool = true

var namePrefix = '${projectName}-${environmentName}'
var tags = {
  project: projectName
  environment: environmentName
}

module monitoring 'modules/monitoring.bicep' = if (deployApplicationInsights) {
  name: 'monitoring'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

module staticWebApp 'modules/staticWebApp.bicep' = {
  name: 'staticWebApp'
  params: {
    location: staticWebAppLocation
    namePrefix: namePrefix
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    containerName: blobContainerName
    allowedOrigin: 'https://${staticWebApp.outputs.defaultHostname}'
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    administratorLogin: postgresAdminUsername
    administratorPassword: postgresAdminPassword
    skuName: postgresSkuName
    skuTier: postgresSkuTier
  }
}

module secrets 'modules/keyVaultSecrets.bicep' = {
  name: 'secrets'
  params: {
    keyVaultName: keyVault.outputs.name
    secrets: {
      'db-connection-string': postgres.outputs.connectionString
      'blob-storage-connection-string': storage.outputs.connectionString
    }
  }
}

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    location: appServicePlanLocation
    namePrefix: namePrefix
    tags: tags
    skuName: appServicePlanSkuName
    skuTier: appServicePlanSkuTier
  }
}

module appService 'modules/appService.bicep' = {
  name: 'appService'
  params: {
    location: appServicePlanLocation
    namePrefix: namePrefix
    tags: tags
    appServicePlanId: appServicePlan.outputs.id
    keyVaultUri: keyVault.outputs.vaultUri
    corsFrontendOrigin: empty(corsFrontendOriginOverride) ? 'https://${staticWebApp.outputs.defaultHostname}' : corsFrontendOriginOverride
    azureAdB2CInstance: azureAdB2CInstance
    azureAdB2CClientId: azureAdB2CClientId
    blobContainerName: blobContainerName
    dbConnectionStringSecretName: 'db-connection-string'
    blobConnectionStringSecretName: 'blob-storage-connection-string'
    appInsightsConnectionString: deployApplicationInsights ? (monitoring.?outputs.connectionString ?? '') : ''
  }
  dependsOn: [
    secrets
  ]
}

// Granted after the App Service exists so its managed identity's principal ID is known —
// avoids a circular module dependency between Key Vault and App Service.
module appServiceKeyVaultAccess 'modules/keyVaultRoleAssignment.bicep' = {
  name: 'appServiceKeyVaultAccess'
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: appService.outputs.principalId
  }
}

output appServiceName string = appService.outputs.name
output appServiceHostName string = appService.outputs.defaultHostName
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppHostName string = staticWebApp.outputs.defaultHostname
output keyVaultName string = keyVault.outputs.name
output postgresServerFqdn string = postgres.outputs.fqdn
output storageAccountName string = storage.outputs.storageAccountName
