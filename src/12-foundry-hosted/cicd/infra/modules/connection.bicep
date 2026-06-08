targetScope = 'resourceGroup'

@description('AI Services (Foundry) account name')
param aiServicesAccountName string

@description('AI Foundry project name')
param aiProjectName string

// Connection configuration (trimmed to the fields a ContainerRegistry
// connection uses).
type ConnectionConfig = {
  @description('Name of the connection')
  name: string

  @description('Category of the connection, e.g. ContainerRegistry')
  category: string

  @description('Target endpoint or URL for the connection')
  target: string

  @description('Authentication type')
  authType: string

  @description('Whether the connection is shared to all users')
  isSharedToAll: bool?

  @description('Additional metadata for the connection')
  metadata: object?
}

@description('Connection configuration')
param connectionConfig ConnectionConfig

@secure()
@description('Credentials for the connection. Kept separate and @secure so secrets never appear in deployment logs.')
param credentials object = {}

resource aiAccount 'Microsoft.CognitiveServices/accounts@2026-03-01' existing = {
  name: aiServicesAccountName

  resource project 'projects' existing = {
    name: aiProjectName
  }
}

resource connection 'Microsoft.CognitiveServices/accounts/projects/connections@2026-03-01' = {
  parent: aiAccount::project
  name: connectionConfig.name
  properties: {
    category: connectionConfig.category
    target: connectionConfig.target
    #disable-next-line BCP036
    authType: connectionConfig.authType
    isSharedToAll: connectionConfig.?isSharedToAll ?? true
    credentials: !empty(credentials) ? credentials : null
    metadata: connectionConfig.?metadata
  }
}

output connectionName string = connection.name
output connectionId string = connection.id
