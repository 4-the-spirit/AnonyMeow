@description('Azure region for the Static Web App (only available in a subset of regions, e.g. eastus2, centralus, westus2, westeurope, eastasia)')
param location string

@description('Resource name prefix, e.g. anonymeow-prod')
param namePrefix string

param tags object = {}

@description('SWA SKU: Free or Standard')
param skuName string = 'Free'

// Provisioned "bare" (no linked repo) — GitHub linking is a one-time manual step
// (az staticwebapp create --source, or the Portal) that also auto-generates and commits
// the frontend's own GitHub Actions deploy workflow. See infra/README.md.
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${namePrefix}-swa'
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    provider: 'None'
  }
}

output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
