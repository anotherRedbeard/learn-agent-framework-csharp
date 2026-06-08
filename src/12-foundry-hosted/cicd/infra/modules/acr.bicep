targetScope = 'resourceGroup'

@description('Location used for the container registry')
param location string = resourceGroup().location

@description('Tags applied to all resources')
param tags object = {}

@description('Resource name for the container registry')
param resourceName string

@description('AI Services (Foundry) account name — parent of the project')
param aiServicesAccountName string

@description('AI Foundry project name — owner of the ACR connection')
param aiProjectName string

@description('Name for the Foundry ACR connection')
param connectionName string

// Reference the project so we can grant its managed identity AcrPull.
resource aiAccount 'Microsoft.CognitiveServices/accounts@2026-03-01' existing = {
  name: aiServicesAccountName

  resource aiProject 'projects' existing = {
    name: aiProjectName
  }
}

// The container registry that holds the agent image.
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2026-01-01-preview' = {
  name: resourceName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

// AcrPull for the Foundry project managed identity — this is the identity the
// hosted agent runtime uses to pull the image when it provisions the container.
resource projectAcrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, resourceName, aiProjectName, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  scope: containerRegistry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    principalId: aiAccount::aiProject.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ACR connection on the Foundry project. This is what tells the Foundry Agent
// Service where to pull the image from. authType 'ManagedIdentity' means the
// project identity (granted AcrPull above) is used — no stored credentials.
module acrConnection './connection.bicep' = {
  name: 'acr-connection'
  params: {
    aiServicesAccountName: aiServicesAccountName
    aiProjectName: aiProjectName
    connectionConfig: {
      name: connectionName
      category: 'ContainerRegistry'
      target: containerRegistry.properties.loginServer
      authType: 'ManagedIdentity'
      isSharedToAll: true
      metadata: {
        ResourceId: containerRegistry.id
      }
    }
    credentials: {
      clientId: aiAccount::aiProject.identity.principalId
      resourceId: containerRegistry.id
    }
  }
}

output containerRegistryName string = containerRegistry.name
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output containerRegistryResourceId string = containerRegistry.id
output containerRegistryConnectionName string = acrConnection.outputs.connectionName
