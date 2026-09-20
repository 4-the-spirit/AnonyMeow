@description('Azure region for the resources')
param location string

@description('Resource name prefix, e.g. anonymeow-prod')
param namePrefix string

param tags object = {}

// RBAC authorization (not access policies) so access can be granted via standard role
// assignments to the App Service's managed identity — see keyVaultRoleAssignment.bicep.
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${namePrefix}-kv'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

output vaultUri string = keyVault.properties.vaultUri
output name string = keyVault.name
output id string = keyVault.id
