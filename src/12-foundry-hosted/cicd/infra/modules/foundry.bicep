targetScope = 'resourceGroup'

@description('Tags applied to all resources')
param tags object = {}

@description('Location for the AI Services (Foundry) account')
param location string

@description('List of model deployments to create on the account')
param deployments deploymentsType

var resourceToken = uniqueString(subscription().id, resourceGroup().id, location)

// Azure AI Foundry = a Cognitive Services account of kind 'AIServices'.
// `allowProjectManagement: true` turns on the Foundry project layer, and a
// system-assigned identity is what the hosted agent runtime uses to pull
// images and call models. No explicit capabilityHost resource is needed with
// this API version — the hosted-agent runtime is provisioned automatically.
resource aiAccount 'Microsoft.CognitiveServices/accounts@2026-03-01' = {
  name: 'aif${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    allowProjectManagement: true
    customSubDomainName: 'aif${resourceToken}'
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }

  // Deploy models sequentially to avoid capacity conflicts.
  @batchSize(1)
  resource seqDeployments 'deployments' = [
    for dep in (deployments ?? []): {
      name: dep.name
      properties: {
        model: dep.model
      }
      sku: dep.sku
    }
  ]
}

output accountId string = aiAccount.id
output aiServicesAccountName string = aiAccount.name

type deploymentsType = {
  @description('Name of the model deployment (this is the AZURE_AI_MODEL_DEPLOYMENT_NAME the agent uses).')
  name: string

  @description('Model definition.')
  model: {
    @description('Model format, e.g. OpenAI.')
    format: string

    @description('Model name, e.g. gpt-4o-mini.')
    name: string

    @description('Model version.')
    version: string
  }

  @description('Deployment SKU.')
  sku: {
    @description('SKU name, e.g. GlobalStandard or Standard.')
    name: string

    @description('SKU capacity (tokens-per-minute in thousands).')
    capacity: int
  }
}[]?
