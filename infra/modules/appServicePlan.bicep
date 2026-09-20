@description('Azure region for the resources')
param location string

@description('Resource name prefix, e.g. anonymeow-prod')
param namePrefix string

param tags object = {}

param skuName string = 'B1'
param skuTier string = 'Basic'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${namePrefix}-plan'
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

output id string = appServicePlan.id
