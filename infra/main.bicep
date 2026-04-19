targetScope = 'subscription'

@description('Azure region for all resources. West Europe by default.')
param location string = 'westeurope'

@description('Short org/owner prefix used in all resource names.')
param orgPrefix string = 'iptyphet'

@description('Workload / app name used in all resource names.')
param workload string = 'simsub'

@description('Environment abbreviation (p = prod).')
param env string = 'p'

var suffix = '${orgPrefix}-${workload}-${env}-we'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${suffix}'
  location: location
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    suffix: suffix
    orgPrefix: orgPrefix
    workload: workload
    env: env
  }
}

output resourceGroupName string = rg.name
output staticWebAppName string = resources.outputs.staticWebAppName
output staticWebAppDefaultHostname string = resources.outputs.staticWebAppDefaultHostname
output storageAccountName string = resources.outputs.storageAccountName
