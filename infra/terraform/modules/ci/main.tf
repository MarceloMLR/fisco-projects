# The identity of the pipeline of an environment (ADR-0055, ADR-0115, ADR-0116): an application in Entra ID that GitHub
# Actions reaches by federation (OIDC), only from the GitHub environment of the same name of this repository, without
# secrets. It publishes the application and runs terraform plan: it changes the resource group, reads and locks the
# Terraform state and reads Entra ID, but cannot give roles or change Entra ID, so the apply stays with whoever operates
# the installation. Where the application does not administer the database, as in production, it joins the group of
# database administrators to apply the migrations (ADR-0125).

variable "display_name" {
  type = string
}

variable "owners" {
  type = list(string)
}

variable "repository" {
  description = "Repository on GitHub, as owner@owner_id/name@repository_id: the ids keep the trust when another repository takes the name."
  type        = string
}

variable "environment" {
  description = "Environment of GitHub whose jobs get the identity."
  type        = string
}

variable "resource_group_id" {
  type = string
}

variable "state_account_id" {
  description = "Storage account of the Terraform state, where terraform plan reads the role of the identity on the container."
  type        = string
}

variable "state_container_id" {
  description = "Container of the Terraform state, read and locked by terraform plan."
  type        = string
}

variable "database_admin" {
  description = "Whether the identity joins the group of database administrators, to apply the migrations (ADR-0125)."
  type        = bool
}

variable "database_admins_group" {
  description = "Group of database administrators."
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

# Only reads the storage account of the state, so that terraform plan reads the role above, which is in it.
resource "azurerm_role_assignment" "state_reader" {
  scope                = var.state_account_id
  role_definition_name = "Reader"
  principal_id         = azuread_service_principal.main.object_id
}

# terraform plan reads the groups and their members (Directory.Read.All) and the applications with their federated
# credentials (Application.Read.All, which Directory.Read.All does not cover) in Entra ID.
data "azuread_service_principal" "graph" {
  client_id = "00000003-0000-0000-c000-000000000000"
}

resource "azuread_app_role_assignment" "directory_read" {
  app_role_id         = data.azuread_service_principal.graph.app_role_ids["Directory.Read.All"]
  principal_object_id = azuread_service_principal.main.object_id
  resource_object_id  = data.azuread_service_principal.graph.object_id
}

resource "azuread_app_role_assignment" "application_read" {
  app_role_id         = data.azuread_service_principal.graph.app_role_ids["Application.Read.All"]
  principal_object_id = azuread_service_principal.main.object_id
  resource_object_id  = data.azuread_service_principal.graph.object_id
}

# Only where the application does not apply the migrations itself (ADR-0114, ADR-0125). The membership is given by the
# apply of whoever operates the installation, never by the pipeline.
resource "azuread_group_member" "database_admin" {
  count = var.database_admin ? 1 : 0

  group_object_id  = var.database_admins_group
  member_object_id = azuread_service_principal.main.object_id
}

output "client_id" {
  value = azuread_application.main.client_id
}
