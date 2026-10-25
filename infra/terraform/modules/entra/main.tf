# The application of the installation in Entra ID, single tenant (ADR-0003, ADR-0004). The sign-in, the permissions of
# the Microsoft Graph and the credential come with the integrations (M365-1 to M365-4).

variable "display_name" {
  description = "Name of the application in Entra ID."
  type        = string
}

variable "owners" {
  description = "Object IDs of the owners."
  type        = list(string)
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

output "client_id" {
  value = azuread_application.main.client_id
}

output "service_principal_object_id" {
  value = azuread_service_principal.main.object_id
}
