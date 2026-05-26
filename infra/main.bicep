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

@description('Capacity in thousands of tokens per minute (TPM). 10 = 10K TPM.')
param modelCapacity int = 10

@description('''
Object ID of the user or managed identity that will run the samples.
This grants the "Foundry User" role on the project, enabling DefaultAzureCredential access.
Get your own ID with:  az ad signed-in-user show --query id -o tsv
Leave empty to skip role assignment (you can add it manually later).
''')
param principalId string = ''

// ── Azure Foundry Resource (AIServices with project management) ────────────────
// This is the parent resource that hosts Foundry projects.
// Following https://learn.microsoft.com/en-us/azure/foundry/tutorials/quickstart-create-foundry-resources
resource foundryAccount 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${name}-foundry'
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: '${name}-foundry'
    publicNetworkAccess: 'Enabled'
    // Enable project creation within this resource
    allowProjectManagement: true
  }
}

// ── Foundry Project ────────────────────────────────────────────────────────────
// The project organizes your work and is visible in ai.azure.com.
// AIProjectClient connects to the project endpoint.
resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2024-10-01' = {
  parent: foundryAccount
  name: projectName
  location: location
  properties: {}
}

// ── Model Deployment ───────────────────────────────────────────────────────────
// Deploys gpt-4o-mini (or your chosen model) using the Standard SKU.
// The model is deployed to the Foundry account and is accessible by all projects.
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundryAccount
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: modelCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelDeploymentName
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
output AZURE_OPENAI_ENDPOINT string = foundryProject.properties.projectEndpoint

@description('Set this as AZURE_OPENAI_DEPLOYMENT_NAME in dotnet user-secrets.')
output AZURE_OPENAI_DEPLOYMENT_NAME string = modelDeployment.name

@description('The project will be visible at https://ai.azure.com under this name.')
output PROJECT_NAME string = foundryProject.name
