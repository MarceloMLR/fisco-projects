# The identity of the pipeline of an environment (ADR-0055, ADR-0115): an application in Entra ID that GitHub Actions
# reaches by federation (OIDC), only from the GitHub environment of the same name, without secrets. It publishes the
# application and runs terraform plan: it changes the resource group, reads the Terraform state and reads Entra ID,
# but cannot give roles or change Entra ID, so the apply stays with whoever operates the installation.

variable "display_name" {
  type = string
}

variable "owners" {
  type = list(string)
}

variable "repository" {
  description = "Repository on GitHub, as owner/name."
  type        = string
}

variable "environment" {
  description = "Environment of GitHub whose jobs get the identity."
  type        = string
}

variable "resource_group_id" {
  type = string
}

variable "state_container_id" {
  description = "Container of the Terraform state, read and locked by terraform plan."
  type        = string
}

resource "azuread_application" "main" {
  display_name     = var.display_name
  sign_in_audience = "AzureADMyOrg"
  owners           = var.owners
}

resource "azuread_service_principal" "main" {
  client_id = azuread_application.main.client_id
  owners    = var.owners
}

resource "azuread_application_federated_identity_credential" "github" {
  application_id = azuread_application.main.id
  display_name   = "github-${var.environment}"
  description    = "GitHub Actions, environment ${var.environment} of ${var.repository}."
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.repository}:environment:${var.environment}"
}

resource "azurerm_role_assignment" "resource_group" {
  scope                = var.resource_group_id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.main.object_id
}

resource "azurerm_role_assignment" "state" {
  scope                = var.state_container_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.main.object_id
}

# terraform plan reads the application, the groups and their members in Entra ID.
data "azuread_service_principal" "graph" {
  client_id = "00000003-0000-0000-c000-000000000000"
}

resource "azuread_app_role_assignment" "directory_read" {
  app_role_id         = data.azuread_service_principal.graph.app_role_ids["Directory.Read.All"]
  principal_object_id = azuread_service_principal.main.object_id
  resource_object_id  = data.azuread_service_principal.graph.object_id
}

output "client_id" {
  value = azuread_application.main.client_id
}
