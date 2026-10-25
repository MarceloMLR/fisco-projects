# The application in the App Service Linux (ADR-0052): API and screens in the same site, always on, HTTPS only, with a
# managed identity that reaches the database and the Key Vault without passwords.

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "plan_name" {
  type = string
}

variable "name" {
  type = string
}

variable "sku" {
  type = string
}

variable "aspnetcore_environment" {
  type = string
}

variable "site_url" {
  description = "Address of the screens, the base of the links of the e-mails."
  type        = string
}

variable "database_server" {
  type = string
}

variable "database_name" {
  type = string
}

variable "insights_connection" {
  type      = string
  sensitive = true
}

variable "key_vault_id" {
  type = string
}

variable "key_vault_name" {
  type = string
}

variable "syncfusion_in_vault" {
  type = bool
}

variable "app_settings" {
  type = map(string)
}

variable "database_admin" {
  description = "Whether the identity joins the group of database administrators (ADR-0114)."
  type        = bool
}

variable "database_admins_group" {
  description = "Group of database administrators."
  type        = string
}

variable "tags" {
  type = map(string)
}

locals {
  free_plan = contains(["F1", "FREE"], upper(var.sku))
  # The secret names use "--" for the ":" of the setting (ADR-0087).
  secret_settings = var.syncfusion_in_vault ? {
    Syncfusion__LicenseKey = "@Microsoft.KeyVault(VaultName=${var.key_vault_name};SecretName=Syncfusion--LicenseKey)"
  } : {}
}

resource "azurerm_service_plan" "main" {
  name                = var.plan_name
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku
  tags                = var.tags
}

resource "azurerm_linux_web_app" "main" {
  name                                           = var.name
  resource_group_name                            = var.resource_group_name
  location                                       = var.location
  service_plan_id                                = azurerm_service_plan.main.id
  https_only                                     = true
  client_certificate_enabled                     = false
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false
  tags                                           = var.tags

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = !local.free_plan
    ftps_state                        = "Disabled"
    http2_enabled                     = true
    minimum_tls_version               = "1.2"
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 10
    use_32_bit_worker                 = false

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = merge(var.app_settings, local.secret_settings, {
    ASPNETCORE_ENVIRONMENT                = var.aspnetcore_environment
    APPLICATIONINSIGHTS_CONNECTION_STRING = var.insights_connection
    Email__SiteUrl                        = var.site_url
    # The App Service ends the TLS: the application reads the https of the request from the proxy, for the address of
    # the way back of the sign-in (ADR-0117).
    ASPNETCORE_FORWARDEDHEADERS_ENABLED = "true"
  })

  # No password: the application signs in to the database with its managed identity (ADR-0052).
  connection_string {
    name  = "FiscoProjects"
    type  = "SQLAzure"
    value = "Server=tcp:${var.database_server},1433;Database=${var.database_name};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False"
  }
}

resource "azurerm_role_assignment" "key_vault_secrets" {
  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.main.identity[0].principal_id
}

resource "azuread_group_member" "database_admin" {
  count = var.database_admin ? 1 : 0

  group_object_id  = var.database_admins_group
  member_object_id = azurerm_linux_web_app.main.identity[0].principal_id
}

output "name" {
  value = azurerm_linux_web_app.main.name
}

output "principal_id" {
  value = azurerm_linux_web_app.main.identity[0].principal_id
}
