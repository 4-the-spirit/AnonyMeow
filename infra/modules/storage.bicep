@description('Azure region for the resources')
param location string

@description('Resource name prefix, e.g. anonymeow-prod')
param namePrefix string

param tags object = {}

@description('Blob container used for post images')
param containerName string = 'post-images'

@description('Origin (scheme + host, no trailing slash) allowed to PUT/GET blobs directly, e.g. https://<swa>.azurestaticapps.net')
param allowedOrigin string

// Storage account names must be 3-24 lowercase alphanumeric characters and globally unique.
var storageAccountName = toLower(take(replace('${namePrefix}st${uniqueString(resourceGroup().id)}', '-', ''), 24))

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

// CORS here (not the backend's CORS policy) governs the browser's direct PUT of image
// bytes to a SAS URL minted by ImageUploadService — the backend origin is irrelevant to
// this specific request, only the frontend origin doing the upload matters.
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: [
            allowedOrigin
          ]
          allowedMethods: [
            'GET'
            'HEAD'
            'PUT'
            'OPTIONS'
          ]
          allowedHeaders: [
            '*'
          ]
          exposedHeaders: [
            '*'
          ]
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}

output storageAccountName string = storageAccount.name
output containerName string = container.name

@secure()
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
