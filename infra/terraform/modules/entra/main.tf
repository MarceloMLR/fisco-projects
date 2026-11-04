# The application of the installation in Entra ID, single tenant (ADR-0003, ADR-0004), ready for the sign-in (ADR-0117):
# the way back to the application, the claims that tell a guest, the consent of the administrator only for the sign-in,
# the assignment of the people and the trust in the managed identity of the App Service as its only credential, the
# reading of the directory for the people search (ADR-0119) and the files in the SharePoint sites an administrator
# chooses (ADR-0120). The permission for the e-mails comes with M365-4. It also exposes its API to the single sign-on of
# Teams, only for the readings of the page in Teams (ADR-0152).

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

variable "site_host" {
  description = "Host of the address of the screens, the one the package of Teams declares in its webApplicationInfo."
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
  # The permissions of the application itself: to read the people of the directory (ADR-0119) and to work only in the
  # SharePoint sites an administrator gives it (ADR-0120), nowhere else.
  directory_role = "User.Read.All"
  sites_role     = "Sites.Selected"
  # The single sign-on of Teams (ADR-0152): the scope the page asks, with a fixed id (the same if the host changes), and
  # the clients of Microsoft that ask it, Teams on the computer and on the phone, and Teams on the web.
  teams_scope    = "access_as_user"
  teams_scope_id = "802cece3-7c0f-4563-b5bd-0ca2b5bdc06f"
  teams_clients = {
    desktop_and_mobile = "1fec8e78-bce4-4aaf-ab1b-5451cc387264"
    web                = "5e3ce6c0-2b1f-4285-8d4b-75ee78787346"
  }
}

data "azuread_service_principal" "graph" {
  client_id = local.graph_app_id
}

resource "azuread_application" "main" {
  display_name     = var.display_name
  sign_in_audience = "AzureADMyOrg"
  owners           = var.owners

  # Access tokens v2 for the API of the application: the audience is the client, and the identifier with the host of the
  # site, which is not a verified domain of the tenant, is allowed (ADR-0152).
  api {
    requested_access_token_version = 2

    oauth2_permission_scope {
      id                         = local.teams_scope_id
      value                      = local.teams_scope
      type                       = "Admin"
      enabled                    = true
      admin_consent_display_name = "Ler o FiscoProjects pelo Teams"
      admin_consent_description  = "Permite que a página do FiscoProjects no Teams leia, como a pessoa, o que ela vê."
    }
  }

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

    # The guest also in the token Teams gives the page (ADR-0007, ADR-0152).
    access_token {
      name = "acct"
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

    resource_access {
      id   = data.azuread_service_principal.graph.app_role_ids[local.directory_role]
      type = "Role"
    }

    resource_access {
      id   = data.azuread_service_principal.graph.app_role_ids[local.sites_role]
      type = "Role"
    }
  }

  # The identifier is its own resource, since it carries the client id of the application itself.
  lifecycle {
    ignore_changes = [identifier_uris]
  }
}

# The identifier of the API, the resource the package of Teams declares: api://{host}/{client} (ADR-0152).
resource "azuread_application_identifier_uri" "teams" {
  application_id = azuread_application.main.id
  identifier_uri = "api://${var.site_host}/${azuread_application.main.client_id}"

  depends_on = [azuread_service_principal.main]
}

# Teams asks the token of the page without consent: its clients are authorized beforehand, only for the scope of the page.
resource "azuread_application_pre_authorized" "teams" {
  for_each = local.teams_clients

  application_id       = azuread_application.main.id
  authorized_client_id = each.value
  permission_ids       = [local.teams_scope_id]
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

# The consent of the administrator for the reading of the directory by the application (ADR-0119).
resource "azuread_app_role_assignment" "directory" {
  app_role_id         = data.azuread_service_principal.graph.app_role_ids[local.directory_role]
  principal_object_id = azuread_service_principal.main.object_id
  resource_object_id  = data.azuread_service_principal.graph.object_id
}

# The consent of the administrator for the sites given to the application, which infra/scripts/Grant-SharePointSite.ps1
# gives, one by one (ADR-0120).
resource "azuread_app_role_assignment" "sites" {
  app_role_id         = data.azuread_service_principal.graph.app_role_ids[local.sites_role]
  principal_object_id = azuread_service_principal.main.object_id
  resource_object_id  = data.azuread_service_principal.graph.object_id
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
