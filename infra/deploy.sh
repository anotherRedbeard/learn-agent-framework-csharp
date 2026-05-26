#!/bin/bash
set -e  # Exit on any error

# ============================================================================
# Azure Foundry Project Deployment Script
# ============================================================================
# This script provisions an Azure Foundry project for the TripBot learning repo.
# Following: https://learn.microsoft.com/en-us/azure/foundry/tutorials/quickstart-create-foundry-resources
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Subscription set (az account set --subscription "<subscription-id>")
# ============================================================================

# Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-tripbot}"
LOCATION="${LOCATION:-eastus2}"
FOUNDRY_ACCOUNT="${FOUNDRY_ACCOUNT:-tripbot-foundry}"
PROJECT_NAME="${PROJECT_NAME:-tripbot-project}"
MODEL_DEPLOYMENT="${MODEL_DEPLOYMENT:-gpt-4o-mini}"
MODEL_VERSION="${MODEL_VERSION:-2024-07-18}"
MODEL_CAPACITY="${MODEL_CAPACITY:-10}"

echo ""
echo "╔══════════════════════════════════════════════════════════════════════════╗"
echo "║  Azure Foundry Project Deployment for TripBot                           ║"
echo "╚══════════════════════════════════════════════════════════════════════════╝"
echo ""
echo "Configuration:"
echo "  Resource Group:    $RESOURCE_GROUP"
echo "  Location:          $LOCATION"
echo "  Foundry Account:   $FOUNDRY_ACCOUNT"
echo "  Project Name:      $PROJECT_NAME"
echo "  Model Deployment:  $MODEL_DEPLOYMENT"
echo ""

# Step 1: Create resource group (idempotent)
echo "→ Step 1/6: Creating resource group..."
if az group show --name "$RESOURCE_GROUP" &>/dev/null; then
  echo "  ✓ Resource group '$RESOURCE_GROUP' already exists"
else
  az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
  echo "  ✓ Created resource group '$RESOURCE_GROUP'"
fi

# Step 2: Create Foundry account with project management enabled (idempotent)
echo ""
echo "→ Step 2/6: Creating Foundry account..."
if az cognitiveservices account show --name "$FOUNDRY_ACCOUNT" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  echo "  ✓ Foundry account '$FOUNDRY_ACCOUNT' already exists"
else
  az cognitiveservices account create \
    --name "$FOUNDRY_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --kind AIServices \
    --sku S0 \
    --location "$LOCATION" \
    --allow-project-management \
    --output none
  echo "  ✓ Created Foundry account '$FOUNDRY_ACCOUNT'"
fi

# Step 3: Set custom subdomain (required for project creation)
echo ""
echo "→ Step 3/6: Configuring custom subdomain..."
az cognitiveservices account update \
  --name "$FOUNDRY_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --custom-domain "$FOUNDRY_ACCOUNT" \
  --output none
echo "  ✓ Custom subdomain configured"

# Step 4: Create Foundry project (idempotent)
echo ""
echo "→ Step 4/6: Creating Foundry project..."
if az cognitiveservices account project show \
     --name "$FOUNDRY_ACCOUNT" \
     --resource-group "$RESOURCE_GROUP" \
     --project-name "$PROJECT_NAME" &>/dev/null; then
  echo "  ✓ Project '$PROJECT_NAME' already exists"
else
  az cognitiveservices account project create \
    --name "$FOUNDRY_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --project-name "$PROJECT_NAME" \
    --location "$LOCATION" \
    --output none
  echo "  ✓ Created project '$PROJECT_NAME'"
fi

# Step 5: Deploy model (idempotent)
echo ""
echo "→ Step 5/6: Deploying model '$MODEL_DEPLOYMENT'..."
if az cognitiveservices account deployment show \
     --name "$FOUNDRY_ACCOUNT" \
     --resource-group "$RESOURCE_GROUP" \
     --deployment-name "$MODEL_DEPLOYMENT" &>/dev/null; then
  echo "  ✓ Model deployment '$MODEL_DEPLOYMENT' already exists"
else
  az cognitiveservices account deployment create \
    --name "$FOUNDRY_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --deployment-name "$MODEL_DEPLOYMENT" \
    --model-name "$MODEL_DEPLOYMENT" \
    --model-version "$MODEL_VERSION" \
    --model-format OpenAI \
    --sku-capacity "$MODEL_CAPACITY" \
    --sku-name Standard \
    --output none
  echo "  ✓ Deployed model '$MODEL_DEPLOYMENT'"
fi

# Step 6: Assign Foundry User role to current user (if not already assigned)
echo ""
echo "→ Step 6/6: Assigning Foundry User role..."
PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv)
PROJECT_ID=$(az cognitiveservices account project show \
  --name "$FOUNDRY_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --project-name "$PROJECT_NAME" \
  --query id -o tsv)

# Foundry User role ID (formerly Azure AI User)
FOUNDRY_USER_ROLE="53ca6127-db72-4b80-b1b0-d745d6d5456d"

if az role assignment list \
     --scope "$PROJECT_ID" \
     --role "$FOUNDRY_USER_ROLE" \
     --assignee "$PRINCIPAL_ID" \
     --query "[0].id" -o tsv &>/dev/null | grep -q .; then
  echo "  ✓ Role already assigned to current user"
else
  az role assignment create \
    --role "$FOUNDRY_USER_ROLE" \
    --assignee "$PRINCIPAL_ID" \
    --scope "$PROJECT_ID" \
    --output none
  echo "  ✓ Assigned Foundry User role to current user"
fi

# Get project endpoint
echo ""
echo "→ Getting project connection details..."
PROJECT_ENDPOINT=$(az cognitiveservices account project show \
  --name "$FOUNDRY_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --project-name "$PROJECT_NAME" \
  --query properties.projectEndpoint -o tsv)

echo ""
echo "╔══════════════════════════════════════════════════════════════════════════╗"
echo "║  ✓ Deployment Complete                                                   ║"
echo "╚══════════════════════════════════════════════════════════════════════════╝"
echo ""
echo "Copy these values to configure dotnet user-secrets:"
echo ""
echo "  AZURE_OPENAI_ENDPOINT:        $PROJECT_ENDPOINT"
echo "  AZURE_OPENAI_DEPLOYMENT_NAME: $MODEL_DEPLOYMENT"
echo ""
echo "Your project is now visible at: https://ai.azure.com"
echo "  → Navigate to your project: $PROJECT_NAME"
echo ""
echo "Next steps:"
echo "  1. cd to any module directory (e.g., src/01-hello-agent)"
echo "  2. Run: dotnet user-secrets set AZURE_OPENAI_ENDPOINT \"$PROJECT_ENDPOINT\""
echo "  3. Run: dotnet user-secrets set AZURE_OPENAI_DEPLOYMENT_NAME \"$MODEL_DEPLOYMENT\""
echo "  4. Run: dotnet run"
echo ""
