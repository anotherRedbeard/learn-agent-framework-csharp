using './main.bicep'

// Short prefix for resource names — must be unique within your Azure subscription.
// Resources created: <name>-ai (Azure AI Services account)
param name = 'tripbot'

// Model to deploy. gpt-4o-mini is recommended: low cost, fast, sufficient for all modules.
param modelDeploymentName = 'gpt-4o-mini'

// Capacity in thousands of tokens per minute. 10 = 10K TPM (sufficient for learning).
param modelCapacity = 10

// Your Azure user object ID — grants passwordless access via DefaultAzureCredential.
// Run this to get your ID:  az ad signed-in-user show --query id -o tsv
param principalId = ''
