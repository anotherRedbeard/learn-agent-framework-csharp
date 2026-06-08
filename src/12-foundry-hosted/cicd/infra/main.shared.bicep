targetScope = 'resourceGroup'

// =============================================================================
// Hosted-agent add-on for an EXISTING Foundry project.
//
// Use this instead of main.bicep when you want the hosted agent to live in the
// SAME Foundry account/project as Modules 1–11's prompt agent (e.g. the
// tripbot-foundry / tripbot-project deployed by the repo-root infra/main.bicep).
//
// It does NOT create a Foundry account or project — it references the existing
// ones and adds only what a hosted container agent needs on top of the base:
//   1. A container registry (ACR) + the project's AcrPull grant + the project
//      ManagedIdentity ACR connection.
//   2. Foundry User for the project managed identity at the account scope, so
//      the container can call the model (the base only grants Foundry User to
//      the human user, not the project identity).
//
// Prerequisite: the existing account must be hosting-capable, i.e. declared with
// the 2026-03-01 account API. Redeploy the repo-root infra/main.bicep (which now
// uses 2026-03-01) before running this.
// =============================================================================

@description('Location for the container registry. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Name of the existing AI Foundry (AIServices) account.')
param existingAccountName string = 'tripbot-foundry'

@description('Name of the existing Foundry project the agent should live in.')
param existingProjectName string = 'tripbot-project'

@description('Name of the existing model deployment the agent calls (AZURE_AI_MODEL_DEPLOYMENT_NAME).')
param modelDeploymentName string = 'gpt-4o-mini'

@description('Tags applied to resources created here.')
param tags object = {}

var resourceToken = uniqueString(subscription().id, resourceGroup().id, existingAccountName)

// The Foundry User built-in role — lets the project managed identity call the
// model endpoint. The base infra grants this role to the human user, not to the
// project identity, so the hosted container needs it added here.
var foundryUserRoleId = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

resource aiAccount 'Microsoft.CognitiveServices/accounts@2026-03-01' existing = {
  name: existingAccountName

  resource project 'projects' existing = {
    name: existingProjectName
  }
}

// Container registry + AcrPull (project MI) + ManagedIdentity ACR connection.
module acr 'modules/acr.bicep' = {
  name: 'acr-shared'
  params: {
    location: location
    tags: tags
    resourceName: 'cr${resourceToken}'
    connectionName: 'acr-${resourceToken}'
    aiServicesAccountName: existingAccountName
    aiProjectName: existingProjectName
  }
}

// Foundry User for the project managed identity at the account scope.
resource foundryUserForProjectMI 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: aiAccount
  name: guid(resourceGroup().id, existingAccountName, existingProjectName, foundryUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', foundryUserRoleId)
    principalId: aiAccount::project.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Same output contract as main.bicep so deploy.sh can read either stack.
output AZURE_RESOURCE_GROUP string = resourceGroup().name
output AZURE_AI_ACCOUNT_NAME string = existingAccountName
output AZURE_AI_PROJECT_NAME string = existingProjectName
output AZURE_AI_PROJECT_ID string = aiAccount::project.id
output AZURE_AI_PROJECT_ENDPOINT string = aiAccount::project.properties.endpoints['AI Foundry API']
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = acr.outputs.containerRegistryLoginServer
output AZURE_AI_MODEL_DEPLOYMENT_NAME string = modelDeploymentName
