targetScope = 'subscription'

@description('Azure region for all resources. West Europe by default.')
param location string = 'westeurope'

@description('System name — overall product/owner prefix (docs/azure-naming.md).')
param system string = 'iptyphet'

@description('Environment abbreviation: d=dev, t=test, s=staging, p=prod.')
param env string = 'p'

@description('Two-letter region abbreviation (we, ne, cc, ce).')
param regionAbbr string = 'we'

@description('Component name within the system. Short, lowercase, no separators.')
param component string = 'simsub'

@description('GitHub numeric user id that is allowed to moderate submissions.')
param adminGitHubUserId string = '48017567'

var namePrefix = '${system}-${env}-${regionAbbr}-${component}'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: '${namePrefix}-rg'
  location: location
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    namePrefix: namePrefix
    storageName: toLower('${system}${env}${regionAbbr}${component}st')
    adminGitHubUserId: adminGitHubUserId
  }
}

output resourceGroupName string = rg.name
output staticWebAppName string = resources.outputs.staticWebAppName
output staticWebAppDefaultHostname string = resources.outputs.staticWebAppDefaultHostname
output storageAccountName string = resources.outputs.storageAccountName
