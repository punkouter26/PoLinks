targetScope = 'subscription'

@description('Target location for all resources.')
param location string = 'eastus'

@description('Application short name prefix. Must match solution prefix.')
param appName string = 'polinks'

@description('Shared resource prefix for cross-app resources.')
param sharedName string = 'poshared'

@description('Subscription ID for compliance checks.')
param subscriptionId string = 'Bbb8dfbe-9169-432f-9b7a-fbf861b51037'

@description('Primary app resource group name.')
param appResourceGroupName string = 'rg-polinks-app-dev'

@description('Shared resource group name.')
param sharedResourceGroupName string = 'rg-poshared-core-dev'

@description('App Service SKU kept at low-cost by default.')
@allowed([
  'F1'
  'B1'
])
param appServiceSkuName string = 'F1'

@description('Shared Key Vault where secrets are stored.')
param keyVaultName string = 'kv-poshared'

@description('Per-app storage account. Do not use shared RG storage for app data tables.')
param storageAccountName string = 'stpolinksdev01'

resource appRg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: appResourceGroupName
  location: location
  tags: {
    solution: 'PoLinks'
    prefix: 'PoLinks'
    role: 'app'
  }
}

resource sharedRg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: sharedResourceGroupName
  location: location
  tags: {
    solution: 'PoLinks'
    prefix: 'PoShared'
    role: 'shared'
  }
}

module appInfra './modules/app.bicep' = {
  name: 'polinks-app-infra'
  scope: resourceGroup(appRg.name)
  params: {
    location: location
    appName: appName
    appServiceSkuName: appServiceSkuName
    storageAccountName: storageAccountName
    keyVaultName: keyVaultName
    sharedResourceGroupName: sharedResourceGroupName
  }
}

output targetSubscriptionId string = subscriptionId
output appResourceGroup string = appRg.name
output sharedResourceGroup string = sharedRg.name
