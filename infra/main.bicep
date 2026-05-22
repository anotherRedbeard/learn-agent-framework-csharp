targetScope = 'resourceGroup'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short name prefix used for all resource names (max 15 characters).')
@maxLength(15)
param name string = 'tripbot'

@description('Name of the GPT model to deploy. Used as AZURE_OPENAI_DEPLOYMENT_NAME.')
@allowed(['gpt-4o-mini', 'gpt-4o', 'gpt-4'])
param modelDeploymentName string = 'gpt-4o-mini'

@description('Capacity in thousands of tokens per minute (TPM). 10 = 10K TPM.')
param modelCapacity int = 10

@description('''
Object ID of the user or managed identity that will run the samples.
This grants the "Cognitive Services OpenAI User" role, enabling DefaultAzureCredential access.
Get your own ID with:  az ad signed-in-user show --query id -o tsv
Leave empty to skip role assignment (you can add it manually later).
''')
param principalId string = ''

// ── Azure AI Services ──────────────────────────────────────────────────────────
// AIServices provides the AI Foundry-compatible endpoint used by AIProjectClient.
// The endpoint output below is what goes into AZURE_OPENAI_ENDPOINT.
resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${name}-ai'
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: '${name}-ai'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Model Deployment ───────────────────────────────────────────────────────────
// Deploys gpt-4o-mini (or your chosen model) using the GlobalStandard SKU.
// GlobalStandard routes across Azure regions for better availability.
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: aiServices
  name: modelDeploymentName
  sku: {
    name: 'GlobalStandard'
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
// "Cognitive Services OpenAI User" (built-in role) — required for passwordless
// access via DefaultAzureCredential. Only created when principalId is provided.
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(aiServices.id, principalId, cognitiveServicesOpenAIUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      cognitiveServicesOpenAIUserRoleId
    )
    principalId: principalId
    principalType: 'User'
  }
}

// ── Outputs ────────────────────────────────────────────────────────────────────
// These are the two values you need for dotnet user-secrets.
// See docs/prerequisites.md for how to set them.

@description('Set this as AZURE_OPENAI_ENDPOINT in dotnet user-secrets.')
output AZURE_OPENAI_ENDPOINT string = aiServices.properties.endpoint

@description('Set this as AZURE_OPENAI_DEPLOYMENT_NAME in dotnet user-secrets.')
output AZURE_OPENAI_DEPLOYMENT_NAME string = modelDeployment.name
