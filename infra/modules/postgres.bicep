@description('Azure region for the resources')
param location string

@description('Resource name prefix, e.g. anonymeow-prod')
param namePrefix string

param tags object = {}

@description('PostgreSQL Flexible Server administrator username')
param administratorLogin string

@secure()
@description('PostgreSQL Flexible Server administrator password')
param administratorPassword string

param skuName string = 'Standard_B1ms'
param skuTier string = 'Burstable'
param storageSizeGB int = 32
param postgresVersion string = '16'
param databaseName string = 'anonymeow'

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: '${namePrefix}-psql'
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresServer
  name: databaseName
}

// App Service on Linux has no static outbound IPs by default, so the server-level
// "allow all Azure services" firewall rule (0.0.0.0-0.0.0.0 is the documented special
// case Azure recognizes for this) is used instead of per-IP rules. Tighten this with
// VNet integration + a private endpoint if that becomes a requirement later.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: postgresServer
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output fqdn string = postgresServer.properties.fullyQualifiedDomainName
output serverName string = postgresServer.name

@secure()
output connectionString string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Port=5432;Database=${databaseName};Username=${administratorLogin};Password=${administratorPassword};Ssl Mode=Require;Trust Server Certificate=true'
