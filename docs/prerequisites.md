# Prerequisites

## 1. .NET 10 SDK

Download and install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Verify with:
```bash
dotnet --version
# Should output 10.x.x
```

## 2. Provision Azure Resources

You have two options — pick one:

### Option A: Bicep (recommended)

The `infra/` folder contains a Bicep template that provisions everything you need:
- **Azure Foundry** account with project management enabled
- **Foundry project** (visible in [ai.azure.com](https://ai.azure.com))
- **gpt-4o-mini** model deployment
- **Foundry User role** assignment for passwordless access

> **Don't edit `infra/main.bicepparam`** — your `principalId` is passed inline on the command line below so it never gets committed to the repo.

```bash
# 1. Log in and set your subscription
az login
az account set --subscription "<your-subscription-id>"

# 2. Create a resource group
az group create --name rg-tripbot --location eastus2

# 3. Deploy — principalId is passed inline so it never touches the committed file
az deployment group create \
  --resource-group rg-tripbot \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --parameters principalId=$(az ad signed-in-user show --query id -o tsv)
```

The full JSON output is verbose — use this to extract just the two values you need:

```bash
# Extract the endpoint and deployment name cleanly
ENDPOINT=$(az deployment group show \
  --resource-group rg-tripbot \
  --name main \
  --query "properties.outputs.azureOpenaiEndpoint.value" -o tsv)

DEPLOYMENT=$(az deployment group show \
  --resource-group rg-tripbot \
  --name main \
  --query "properties.outputs.azureOpenaiDeploymentName.value" -o tsv)

echo "AZURE_OPENAI_ENDPOINT:        $ENDPOINT"
echo "AZURE_OPENAI_DEPLOYMENT_NAME: $DEPLOYMENT"
```

After deployment completes, your project will be visible at [https://ai.azure.com](https://ai.azure.com) under the name **tripbot-project**.

Copy the printed values — you'll use them in Step 4 to configure `dotnet user-secrets`.

### Option B: Manual (Azure AI Foundry portal)

1. Sign in at the [Azure AI Foundry portal](https://ai.azure.com/)
2. Create a project (or use an existing one)
3. Deploy a model — `gpt-4o-mini` is recommended (low cost, fast)
4. Copy your **project endpoint** from the project overview page

## 3. Azure CLI

Install the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) and log in:

```bash
az login
```

The samples use `DefaultAzureCredential`, which automatically picks up your Azure CLI session during development — no API keys needed.

> **Production note:** In production, use `ManagedIdentityCredential` instead of `DefaultAzureCredential`. The default credential probes multiple sources sequentially, adding latency and unnecessary complexity in production.

## 4. Configure credentials for each module

The recommended approach for development is **`dotnet user-secrets`**. This stores credentials outside your project directory so they're never accidentally committed to git.

Run these commands inside each module directory you want to run:

```bash
cd src/01-hello-agent

dotnet user-secrets init
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-project.services.ai.azure.com"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
```

Repeat for each module (`src/02-add-tools`, `src/03-multi-turn`, etc.).

Alternatively, set environment variables in your shell session (applies to all modules):

```bash
# macOS / Linux
export AZURE_OPENAI_ENDPOINT="https://your-project.services.ai.azure.com"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"

# Windows (Command Prompt)
set AZURE_OPENAI_ENDPOINT=https://your-project.services.ai.azure.com
set AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini

# Windows (PowerShell)
$env:AZURE_OPENAI_ENDPOINT = "https://your-project.services.ai.azure.com"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4o-mini"
```

> **Important:** The `.env.example` file at the repo root is a reference template only. .NET applications do **not** automatically load `.env` files — use `dotnet user-secrets` or shell environment variables as shown above.

> **Never commit credentials to source control.** Both `.env` and `appsettings.*.json` are excluded by `.gitignore`.

## 5. Running a module

```bash
cd src/01-hello-agent
dotnet run
```

That's it. The first run will restore NuGet packages automatically.

---

## ✅ Ready to go!

**→ [Start with Module 01: src/README.md](../src/README.md)**

