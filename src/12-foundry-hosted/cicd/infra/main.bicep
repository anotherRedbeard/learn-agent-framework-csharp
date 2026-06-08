targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment — used as part of the resource naming convention')
param environmentName string

@minLength(1)
@maxLength(90)
@description('Name of the resource group to use or create')
param resourceGroupName string = 'rg-${environmentName}'

@minLength(1)
@description('Primary location for all resources')
@allowed([
  'australiaeast'
  'eastus'
  'eastus2'
  'francecentral'
  'japaneast'
  'koreacentral'
  'norwayeast'
  'polandcentral'
  'southindia'
  'swedencentral'
  'switzerlandnorth'
  'uaenorth'
  'uksouth'
  'westus'
  'westus2'
  'westus3'
])
param location string

@description('Location for the AI model deployments (defaults to the primary location)')
param aiDeploymentsLocation string = location

@description('Name of the AI Foundry project')
param aiFoundryProjectName string = 'project-${environmentName}'

@description('List of model deployments to create on the Foundry account')
param deployments array

var tags = {
  'azd-env-name': environmentName
}

var resourceToken = uniqueString(subscription().id, resourceGroupName, aiDeploymentsLocation)

resource rg 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Azure AI Foundry account + model deployments.
module foundry 'modules/foundry.bicep' = {
  scope: rg
  name: 'foundry'
  params: {
    tags: tags
    location: aiDeploymentsLocation
    deployments: deployments
  }
}

// Log Analytics workspace — backing store for Application Insights telemetry.
module logAnalytics 'modules/loganalytics.bicep' = {
  scope: rg
  name: 'logAnalytics'
  params: {
    name: 'log-${resourceToken}'
    location: aiDeploymentsLocation
    tags: tags
  }
}

// Application Insights — captures traces/metrics from the hosted agent. Wired
// into the project so the portal and later observability modules can use it.
module applicationInsights 'modules/applicationinsights.bicep' = {
  scope: rg
  name: 'applicationInsights'
  params: {
    name: 'appi-${resourceToken}'
    location: aiDeploymentsLocation
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// Foundry project (with the managed identity the hosted agent runs as).
module foundryProject 'modules/foundry-project.bicep' = {
  scope: rg
  name: 'foundry-project'
  params: {
    tags: tags
    location: aiDeploymentsLocation
    aiFoundryProjectName: aiFoundryProjectName
    aiServicesAccountName: foundry.outputs.aiServicesAccountName
    appInsightsId: applicationInsights.outputs.id
    appInsightsConnectionString: applicationInsights.outputs.connectionString
  }
}

// Container registry for the agent image + the project ACR connection.
module acr 'modules/acr.bicep' = {
  scope: rg
  name: 'acr'
  params: {
    location: aiDeploymentsLocation
    tags: tags
    resourceName: 'cr${resourceToken}'
    connectionName: 'acr-${resourceToken}'
    aiServicesAccountName: foundry.outputs.aiServicesAccountName
    aiProjectName: foundryProject.outputs.projectName
  }
}

output AZURE_RESOURCE_GROUP string = resourceGroupName
output AZURE_AI_ACCOUNT_NAME string = foundry.outputs.aiServicesAccountName
output AZURE_AI_PROJECT_NAME string = foundryProject.outputs.projectName
output AZURE_AI_PROJECT_ID string = foundryProject.outputs.projectId
output AZURE_AI_PROJECT_ENDPOINT string = foundryProject.outputs.AZURE_AI_PROJECT_ENDPOINT
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = acr.outputs.containerRegistryLoginServer
output AZURE_AI_MODEL_DEPLOYMENT_NAME string = deployments[0].name
output APPLICATIONINSIGHTS_CONNECTION_STRING string = applicationInsights.outputs.connectionString
