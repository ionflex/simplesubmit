@description('Region for all resources.')
param location string

@description('Shared suffix used in all resource names (orgPrefix-workload-env-region).')
param suffix string

@description('Short org/owner prefix (used for the run-together storage name).')
param orgPrefix string

@description('Workload name (used for the run-together storage name).')
param workload string

@description('Environment abbreviation (used for the run-together storage name).')
param env string

var storageName = toLower('st${orgPrefix}${workload}${env}we')

resource log 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${suffix}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appi 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: log.id
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'stapp-${suffix}'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

resource swaAppSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: swa
  name: 'appsettings'
  properties: {
    APPLICATIONINSIGHTS_CONNECTION_STRING: appi.properties.ConnectionString
  }
}

output staticWebAppName string = swa.name
output staticWebAppDefaultHostname string = swa.properties.defaultHostname
output storageAccountName string = storage.name
