# ============================================================================
# Azure Foundry Project Deployment Script (PowerShell)
# ============================================================================
# This script provisions an Azure Foundry project for the TripBot learning repo.
# Following: https://learn.microsoft.com/en-us/azure/foundry/tutorials/quickstart-create-foundry-resources
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Subscription set (az account set --subscription "<subscription-id>")
# ============================================================================

$ErrorActionPreference = "Stop"

# Configuration - can be overridden via environment variables
$RESOURCE_GROUP = if ($env:RESOURCE_GROUP) { $env:RESOURCE_GROUP } else { "rg-tripbot" }
$LOCATION = if ($env:LOCATION) { $env:LOCATION } else { "eastus2" }
$FOUNDRY_ACCOUNT = if ($env:FOUNDRY_ACCOUNT) { $env:FOUNDRY_ACCOUNT } else { "tripbot-foundry" }
$PROJECT_NAME = if ($env:PROJECT_NAME) { $env:PROJECT_NAME } else { "tripbot-project" }
$MODEL_DEPLOYMENT = if ($env:MODEL_DEPLOYMENT) { $env:MODEL_DEPLOYMENT } else { "gpt-4o-mini" }
$MODEL_VERSION = if ($env:MODEL_VERSION) { $env:MODEL_VERSION } else { "2024-07-18" }
$MODEL_CAPACITY = if ($env:MODEL_CAPACITY) { $env:MODEL_CAPACITY } else { "10" }

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Azure Foundry Project Deployment for TripBot                           ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:"
Write-Host "  Resource Group:    $RESOURCE_GROUP"
Write-Host "  Location:          $LOCATION"
Write-Host "  Foundry Account:   $FOUNDRY_ACCOUNT"
Write-Host "  Project Name:      $PROJECT_NAME"
Write-Host "  Model Deployment:  $MODEL_DEPLOYMENT"
Write-Host ""

# Step 1: Create resource group (idempotent)
Write-Host "→ Step 1/6: Creating resource group..." -ForegroundColor Yellow
try {
    az group show --name $RESOURCE_GROUP 2>$null | Out-Null
    Write-Host "  ✓ Resource group '$RESOURCE_GROUP' already exists" -ForegroundColor Green
} catch {
    az group create --name $RESOURCE_GROUP --location $LOCATION --output none
    Write-Host "  ✓ Created resource group '$RESOURCE_GROUP'" -ForegroundColor Green
}

# Step 2: Create Foundry account with project management enabled (idempotent)
Write-Host ""
Write-Host "→ Step 2/6: Creating Foundry account..." -ForegroundColor Yellow
try {
    az cognitiveservices account show --name $FOUNDRY_ACCOUNT --resource-group $RESOURCE_GROUP 2>$null | Out-Null
    Write-Host "  ✓ Foundry account '$FOUNDRY_ACCOUNT' already exists" -ForegroundColor Green
} catch {
    az cognitiveservices account create `
        --name $FOUNDRY_ACCOUNT `
        --resource-group $RESOURCE_GROUP `
        --kind AIServices `
        --sku S0 `
        --location $LOCATION `
        --allow-project-management `
        --output none
    Write-Host "  ✓ Created Foundry account '$FOUNDRY_ACCOUNT'" -ForegroundColor Green
}

# Step 3: Set custom subdomain (required for project creation)
Write-Host ""
Write-Host "→ Step 3/6: Configuring custom subdomain..." -ForegroundColor Yellow
az cognitiveservices account update `
    --name $FOUNDRY_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --custom-domain $FOUNDRY_ACCOUNT `
    --output none
Write-Host "  ✓ Custom subdomain configured" -ForegroundColor Green

# Step 4: Create Foundry project (idempotent)
Write-Host ""
Write-Host "→ Step 4/6: Creating Foundry project..." -ForegroundColor Yellow
try {
    az cognitiveservices account project show `
        --name $FOUNDRY_ACCOUNT `
        --resource-group $RESOURCE_GROUP `
        --project-name $PROJECT_NAME 2>$null | Out-Null
    Write-Host "  ✓ Project '$PROJECT_NAME' already exists" -ForegroundColor Green
} catch {
    az cognitiveservices account project create `
        --name $FOUNDRY_ACCOUNT `
        --resource-group $RESOURCE_GROUP `
        --project-name $PROJECT_NAME `
        --location $LOCATION `
        --output none
    Write-Host "  ✓ Created project '$PROJECT_NAME'" -ForegroundColor Green
}

# Step 5: Deploy model (idempotent)
Write-Host ""
Write-Host "→ Step 5/6: Deploying model '$MODEL_DEPLOYMENT'..." -ForegroundColor Yellow
try {
    az cognitiveservices account deployment show `
        --name $FOUNDRY_ACCOUNT `
        --resource-group $RESOURCE_GROUP `
        --deployment-name $MODEL_DEPLOYMENT 2>$null | Out-Null
    Write-Host "  ✓ Model deployment '$MODEL_DEPLOYMENT' already exists" -ForegroundColor Green
} catch {
    az cognitiveservices account deployment create `
        --name $FOUNDRY_ACCOUNT `
        --resource-group $RESOURCE_GROUP `
        --deployment-name $MODEL_DEPLOYMENT `
        --model-name $MODEL_DEPLOYMENT `
        --model-version $MODEL_VERSION `
        --model-format OpenAI `
        --sku-capacity $MODEL_CAPACITY `
        --sku-name Standard `
        --output none
    Write-Host "  ✓ Deployed model '$MODEL_DEPLOYMENT'" -ForegroundColor Green
}

# Step 6: Assign Foundry User role to current user (if not already assigned)
Write-Host ""
Write-Host "→ Step 6/6: Assigning Foundry User role..." -ForegroundColor Yellow
$PRINCIPAL_ID = az ad signed-in-user show --query id -o tsv
$PROJECT_ID = az cognitiveservices account project show `
    --name $FOUNDRY_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --project-name $PROJECT_NAME `
    --query id -o tsv

# Foundry User role ID (formerly Azure AI User)
$FOUNDRY_USER_ROLE = "53ca6127-db72-4b80-b1b0-d745d6d5456d"

$existingAssignment = az role assignment list `
    --scope $PROJECT_ID `
    --role $FOUNDRY_USER_ROLE `
    --assignee $PRINCIPAL_ID `
    --query "[0].id" -o tsv 2>$null

if ($existingAssignment) {
    Write-Host "  ✓ Role already assigned to current user" -ForegroundColor Green
} else {
    az role assignment create `
        --role $FOUNDRY_USER_ROLE `
        --assignee $PRINCIPAL_ID `
        --scope $PROJECT_ID `
        --output none
    Write-Host "  ✓ Assigned Foundry User role to current user" -ForegroundColor Green
}

# Get project endpoint
Write-Host ""
Write-Host "→ Getting project connection details..." -ForegroundColor Yellow
$PROJECT_ENDPOINT = az cognitiveservices account project show `
    --name $FOUNDRY_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --project-name $PROJECT_NAME `
    --query properties.projectEndpoint -o tsv

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  ✓ Deployment Complete                                                   ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Copy these values to configure dotnet user-secrets:"
Write-Host ""
Write-Host "  AZURE_OPENAI_ENDPOINT:        $PROJECT_ENDPOINT" -ForegroundColor Green
Write-Host "  AZURE_OPENAI_DEPLOYMENT_NAME: $MODEL_DEPLOYMENT" -ForegroundColor Green
Write-Host ""
Write-Host "Your project is now visible at: https://ai.azure.com"
Write-Host "  → Navigate to your project: $PROJECT_NAME"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. cd to any module directory (e.g., src\01-hello-agent)"
Write-Host "  2. Run: dotnet user-secrets set AZURE_OPENAI_ENDPOINT `"$PROJECT_ENDPOINT`""
Write-Host "  3. Run: dotnet user-secrets set AZURE_OPENAI_DEPLOYMENT_NAME `"$MODEL_DEPLOYMENT`""
Write-Host "  4. Run: dotnet run"
Write-Host ""
