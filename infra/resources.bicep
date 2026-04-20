@description('Region for all resources.')
param location string

@description('Resource name prefix: system-env-region-component (no resource-type suffix).')
param namePrefix string

@description('Storage account name (3-24 lowercase alphanumeric, no separators).')
param storageName string

@description('SWA-assigned principal userId from /.auth/me that is allowed to moderate submissions.')
param adminPrincipalId string

resource log 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-log'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appi 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-appi'
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

var storageConnection = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${namePrefix}-stapp'
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
    ADMIN_PRINCIPAL_ID: adminPrincipalId
    SUGGESTIONS_STORAGE: storageConnection
  }
}

output staticWebAppName string = swa.name
output staticWebAppDefaultHostname string = swa.properties.defaultHostname
output storageAccountName string = storage.name
