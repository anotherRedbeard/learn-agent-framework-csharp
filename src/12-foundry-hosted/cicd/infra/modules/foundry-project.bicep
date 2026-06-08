targetScope = 'resourceGroup'

@description('Tags applied to all resources')
param tags object = {}

@description('Location for the project')
param location string

@description('Name of the AI Foundry project')
param aiFoundryProjectName string

@description('Name of the parent AI Services (Foundry) account')
param aiServicesAccountName string

@description('Resource ID of the Application Insights instance. Pass empty string to skip the connection.')
param appInsightsId string = ''

@description('Connection string for Application Insights. Pass empty string to skip.')
@secure()
param appInsightsConnectionString string = ''

var resourceToken = uniqueString(subscription().id, resourceGroup().id, location)

// Reference the parent account created by foundry.bicep.
resource aiAccount 'Microsoft.CognitiveServices/accounts@2026-03-01' existing = {
  name: aiServicesAccountName
}

// The Foundry project — scoped under the AI Services account. Its
// system-assigned identity is the principal that the hosted agent container
// runs as: it pulls the image (via AcrPull, granted in acr.bicep) and calls
// the model endpoint (via the Foundry User role, granted below).
resource project 'Microsoft.CognitiveServices/accounts/projects@2026-03-01' = {
  parent: aiAccount
  name: aiFoundryProjectName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    description: '${aiFoundryProjectName} Project'
    displayName: aiFoundryProjectName
  }
}

// Foundry User for the project managed identity. The Foundry runtime injects
// FOUNDRY_PROJECT_ENDPOINT into the container; this role is what allows that
// endpoint to be called with the container's managed identity. Without it the
// container gets a 401 when it tries to call the model.
resource foundryUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: aiAccount
  name: guid(resourceGroup().id, aiFoundryProjectName, '53ca6127-db72-4b80-b1b0-d745d6d5456d')
  properties: {
    principalId: project.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d') // Foundry User
  }
}

// App Insights connection on the project — lets the Foundry portal and later
// observability modules surface agent traces. authType 'ApiKey' with the
// connection string is the option the portal supports for this category.
module appInsightConnection './connection.bicep' = if (!empty(appInsightsId)) {
  name: 'appi-connection'
  params: {
    aiServicesAccountName: aiServicesAccountName
    aiProjectName: aiFoundryProjectName
    connectionConfig: {
      name: 'appi-${resourceToken}'
      category: 'AppInsights'
      target: appInsightsId
      authType: 'ApiKey'
      isSharedToAll: true
      metadata: {
        ApiType: 'Azure'
        ResourceId: appInsightsId
      }
    }
    credentials: {
      key: appInsightsConnectionString
    }
  }
  dependsOn: [project]
}

// Reference App Insights so we can scope the Log Analytics Reader role.
resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = if (!empty(appInsightsId)) {
  name: last(split(appInsightsId, '/'))
}

// Log Analytics Reader for the project managed identity — required for running
// evaluations against agent traces stored in the Log Analytics workspace.
resource logAnalyticsReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(appInsightsId)) {
  scope: existingAppInsights
  name: guid(resourceGroup().id, aiFoundryProjectName, '73c42c96-874c-492b-b04d-ab87d138a893')
  properties: {
    principalId: project.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '73c42c96-874c-492b-b04d-ab87d138a893') // Log Analytics Reader
  }
}

output AZURE_AI_PROJECT_ENDPOINT string = project.properties.endpoints['AI Foundry API']
output projectId string = project.id
output projectName string = project.name
output projectPrincipalId string = project.identity.principalId
