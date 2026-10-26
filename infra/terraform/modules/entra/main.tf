# The application of the installation in Entra ID, single tenant (ADR-0003, ADR-0004), ready for the sign-in (ADR-0117):
# the way back to the application, the claims that tell a guest, the consent of the administrator only for the sign-in,
# the assignment of the people and the trust in the managed identity of the App Service as its only credential. The
# permissions of the Microsoft Graph for the people, the files and the e-mails come with M365-2 to M365-4.

variable "display_name" {
  description = "Name of the application in Entra ID."
  type        = string
}

variable "owners" {
  description = "Object IDs of the owners."
  type        = list(string)
}

variable "tenant_id" {
  description = "Tenant of the installation, the issuer of the tokens of the managed identity."
  type        = string
}

variable "redirect_uri" {
  description = "Address where Entra ID brings the person back after the sign-in."
  type        = string
}

variable "app_identity_principal_id" {
  description = "Object ID of the managed identity of the App Service, which proves the application to Entra ID."
  type        = string
}

variable "assignment_required" {
  description = "Whether only the people and groups assigned to the application sign in (ADR-0005)."
  type        = bool
}

locals {
  graph_app_id = "00000003-0000-0000-c000-000000000000"
  # Only the scopes of the sign-in: who the person is, the name and the e-mail.
  sign_in_scopes = ["openid", "profile", "email"]
}

data "azuread_service_principal" "graph" {
  client_id = local.graph_app_id
}

resource "azuread_application" "main" {
  display_name     = var.display_name
  sign_in_audience = "AzureADMyOrg"
  owners           = var.owners

  web {
    redirect_uris = [var.redirect_uri]

    implicit_grant {
      access_token_issuance_enabled = false
      id_token_issuance_enabled     = false
    }
  }

  # "acct" tells a guest of the tenant (ADR-0007); "email" comes even when the e-mail is not the sign-in name.
  optional_claims {
    id_token {
      name = "acct"
    }

    id_token {
      name = "email"
    }
  }

  required_resource_access {
    resource_app_id = local.graph_app_id

    dynamic "resource_access" {
      for_each = local.sign_in_scopes

      content {
        id   = data.azuread_service_principal.graph.oauth2_permission_scope_ids[resource_access.value]
        type = "Scope"
      }
    }
  }
}

resource "azuread_service_principal" "main" {
  client_id                    = azuread_application.main.client_id
  owners                       = var.owners
  app_role_assignment_required = var.assignment_required

  # Listed in Enterprise applications of the portal, where the people are assigned (ADR-0005).
  feature_tags {
    enterprise = true
  }
}

# The consent of the administrator for everyone, so no person is asked for the scopes of the sign-in.
resource "azuread_service_principal_delegated_permission_grant" "sign_in" {
  service_principal_object_id          = azuread_service_principal.main.object_id
  resource_service_principal_object_id = data.azuread_service_principal.graph.object_id
  claim_values                         = local.sign_in_scopes
}

# The only credential of the application: a token of the managed identity of the App Service. No secret nor certificate.
resource "azuread_application_federated_identity_credential" "app_identity" {
  application_id = azuread_application.main.id
  display_name   = "app-service-managed-identity"
  description    = "The managed identity of the App Service proves the application (ADR-0117)."
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://login.microsoftonline.com/${var.tenant_id}/v2.0"
  subject        = var.app_identity_principal_id
}

output "client_id" {
  value = azuread_application.main.client_id
}

output "service_principal_object_id" {
  value = azuread_service_principal.main.object_id
}
