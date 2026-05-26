// =============================================================================
// Azure AI Foundry Project for TripBot Learning Repo
//
// Deploys:
//   1. AI Foundry account (Microsoft.CognitiveServices/accounts, kind: AIServices)
//   2. AI Project (child resource)
//   3. gpt-4o-mini model deployment
//   4. Foundry User role assignment
//
// API version 2025-06-01 required for allowProjectManagement and projects support.
// Reference: https://github.com/anotherRedbeard/agentic-learning/tree/main/infra/ai-foundry
// =============================================================================

targetScope = 'resourceGroup'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short name prefix used for all resource names (max 15 characters).')
@maxLength(15)
param name string = 'tripbot'

@description('Name for the Foundry project.')
param projectName string = 'tripbot-project'

@description('Name of the GPT model to deploy. Used as AZURE_OPENAI_DEPLOYMENT_NAME.')
@allowed(['gpt-4o-mini', 'gpt-4o', 'gpt-4'])
param modelDeploymentName string = 'gpt-4o-mini'

@description('Model version to deploy.')
param modelVersion string = '2024-07-18'

@description('Capacity in thousands of tokens per minute (TPM). 10 = 10K TPM.')
param modelCapacity int = 10

@description('''
Object ID of the user or managed identity that will run the samples.
This grants the "Foundry User" role on the project, enabling DefaultAzureCredential access.
Get your own ID with:  az ad signed-in-user show --query id -o tsv
Leave empty to skip role assignment (you can add it manually later).
''')
param principalId string = ''

// ── Azure Foundry Account ──────────────────────────────────────────────────────
// AIServices account with allowProjectManagement: true enables project creation
resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: '${name}-foundry'
  location: location
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  properties: {
    allowProjectManagement: true
    customSubDomainName: '${name}-foundry'
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

// ── Foundry Project ────────────────────────────────────────────────────────────
// The project organizes your work and is visible in ai.azure.com.
// AIProjectClient connects to the project endpoint.
resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: foundryAccount
  name: projectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

// ── Model Deployment ───────────────────────────────────────────────────────────
// Deploys gpt-4o-mini (or your chosen model) at the account level.
// Available to all projects within the account.
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: foundryAccount
  name: modelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: modelCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelDeploymentName
      version: modelVersion
    }
  }
}

// ── Role Assignment ────────────────────────────────────────────────────────────
// "Foundry User" (built-in role) — grants minimum permissions to use the project.
// Scoped to the project so the user can access it in ai.azure.com and via AIProjectClient.
// Role definition ID: 53ca6127-db72-4b80-b1b0-d745d6d5456d
var foundryUserRoleId = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(foundryProject.id, principalId, foundryUserRoleId)
  scope: foundryProject
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      foundryUserRoleId
    )
    principalId: principalId
    principalType: 'User'
  }
}

// ── Outputs ────────────────────────────────────────────────────────────────────
// These are the two values you need for dotnet user-secrets.
// See docs/prerequisites.md for how to set them.

@description('Set this as AZURE_OPENAI_ENDPOINT in dotnet user-secrets.')
output AZURE_OPENAI_ENDPOINT string = '${foundryAccount.properties.endpoint}api/projects/${foundryProject.name}'

@description('Set this as AZURE_OPENAI_DEPLOYMENT_NAME in dotnet user-secrets.')
output AZURE_OPENAI_DEPLOYMENT_NAME string = modelDeployment.name

@description('The project will be visible at https://ai.azure.com under this name.')
output PROJECT_NAME string = foundryProject.name
