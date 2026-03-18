param location string
param appName string
param appServiceSkuName string
param storageAccountName string
param keyVaultName string
param sharedResourceGroupName string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${appName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'asp-${appName}'
  location: location
  sku: {
    name: appServiceSkuName
    tier: appServiceSkuName == 'F1' ? 'Free' : 'Basic'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

resource app 'Microsoft.Web/sites@2024-04-01' = {
  name: 'app-${appName}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'KeyVault__Uri'
          value: 'https://${keyVaultName}.vault.azure.net/'
        }
      ]
    }
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowSharedKeyAccess: false
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource appConfig 'Microsoft.Web/sites/config@2024-04-01' = {
  name: '${app.name}/appsettings'
  properties: {
    'AzureStorage__TableServiceUri': 'https://${storage.name}.table.core.windows.net/'
  }
}

output appNameOut string = app.name
output appManagedIdentityPrincipalId string = app.identity.principalId
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output sharedResourceGroupReference string = sharedResourceGroupName
