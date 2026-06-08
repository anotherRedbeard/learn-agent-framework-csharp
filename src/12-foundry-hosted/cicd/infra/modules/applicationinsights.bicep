metadata description = 'Creates an Application Insights instance backed by an existing Log Analytics workspace.'

@description('Name of the Application Insights component')
param name string

@description('Location for the component')
param location string = resourceGroup().location

@description('Tags applied to the component')
param tags object = {}

@description('Resource ID of the Log Analytics workspace that backs this component')
param logAnalyticsWorkspaceId string

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output connectionString string = applicationInsights.properties.ConnectionString
output id string = applicationInsights.id
output name string = applicationInsights.name
