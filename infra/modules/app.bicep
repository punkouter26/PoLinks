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

// Explicit table resources — ensures tables exist after bicep deploy (idempotent)
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  name: 'default'
  parent: storage
}

resource pulseBatchesTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: 'PulseBatches'
  parent: tableService
}

resource anchorNodesTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: 'AnchorNodes'
  parent: tableService
}

resource ingestedPostsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: 'IngestedPosts'
  parent: tableService
}

// Grant the App Service managed identity Storage Table Data Contributor on this account
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  // Storage Table Data Contributor built-in role ID
  name: guid(storage.id, app.id, '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource appConfig 'Microsoft.Web/sites/config@2024-04-01' = {
  name: 'appsettings'
  parent: app
  properties: {
    // App reads the table service URI and uses DefaultAzureCredential (managed identity in Azure)
    AzureStorage__TableServiceUri: 'https://${storage.name}.table.${environment().suffixes.storage}/'
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    KeyVault__Uri: 'https://${keyVaultName}.vault.${environment().suffixes.keyvaultDns}/'
    ASPNETCORE_ENVIRONMENT: 'Production'
    WEBSITES_PORT: '8080'
  }
}

output appNameOut string = app.name
output appManagedIdentityPrincipalId string = app.identity.principalId
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output sharedResourceGroupReference string = sharedResourceGroupName
output tableServiceUri string = 'https://${storage.name}.table.${environment().suffixes.storage}/'
