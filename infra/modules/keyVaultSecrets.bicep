@description('Name of an existing Key Vault to write secrets into')
param keyVaultName string

@secure()
@description('Map of secret name to secret value to create. Values never appear in deployment logs or committed files — only the deployment engine handles them.')
param secrets object

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource kvSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [
  for secret in items(secrets): {
    parent: keyVault
    name: secret.key
    properties: {
      value: secret.value
    }
  }
]
