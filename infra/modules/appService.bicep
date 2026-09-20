@description('Azure region for the resources')
param location string

@description('Resource name prefix, e.g. anonymeow-prod')
param namePrefix string

param tags object = {}

@description('Resource ID of the App Service Plan to host this app on')
param appServicePlanId string

@description('Key Vault URI (e.g. https://<vault>.vault.azure.net/), used to build Key Vault reference app settings')
param keyVaultUri string

@description('Allowed CORS origin for the backend, i.e. the deployed frontend origin')
param corsFrontendOrigin string

@description('Microsoft Entra External ID (CIAM) instance, e.g. https://<tenant>.ciamlogin.com')
param azureAdB2CInstance string

@description('Microsoft Entra External ID (CIAM) app registration Client ID')
param azureAdB2CClientId string

@description('Blob container name for post images')
param blobContainerName string

@description('Name of the Key Vault secret holding the database connection string')
param dbConnectionStringSecretName string

@description('Name of the Key Vault secret holding the blob storage connection string')
param blobConnectionStringSecretName string

@description('Application Insights connection string; leave empty to skip auto-instrumentation')
param appInsightsConnectionString string = ''

// Key Vault reference syntax: app resolves the actual secret value at runtime via its
// managed identity — see keyVaultRoleAssignment.bicep for the access grant this depends on.
var dbConnectionStringRef = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/${dbConnectionStringSecretName}/)'
var blobConnectionStringRef = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/${blobConnectionStringSecretName}/)'

var baseAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'ConnectionStrings__Default'
    value: dbConnectionStringRef
  }
  {
    name: 'AzureAdB2C__Instance'
    value: azureAdB2CInstance
  }
  {
    name: 'AzureAdB2C__ClientId'
    value: azureAdB2CClientId
  }
  {
    name: 'BlobStorage__ConnectionString'
    value: blobConnectionStringRef
  }
  {
    name: 'BlobStorage__ContainerName'
    value: blobContainerName
  }
  {
    name: 'Cors__FrontendOrigin'
    value: corsFrontendOrigin
  }
]

var appInsightsSettings = empty(appInsightsConnectionString) ? [] : [
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  {
    name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
    value: '~3'
  }
]

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: '${namePrefix}-api'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      healthCheckPath: '/health'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: concat(baseAppSettings, appInsightsSettings)
    }
  }
}

output principalId string = appService.identity.principalId
output name string = appService.name
output defaultHostName string = appService.properties.defaultHostName
