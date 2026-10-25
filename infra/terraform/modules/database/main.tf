# The Azure SQL database (ADR-0048) with its backups (ADR-0059). Only Entra ID signs in, with no SQL password: the
# administrator is a group of Entra ID, and the application uses its managed identity (ADR-0052, ADR-0114).

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "server_name" {
  type = string
}

variable "database_name" {
  type = string
}

variable "admins_group_name" {
  type = string
}

variable "operator_object_ids" {
  description = "Owners and members of the group of administrators."
  type        = list(string)
}

variable "tenant_id" {
  type = string
}

variable "sku" {
  type = string
}

variable "max_size_gb" {
  type = number
}

variable "restore_days" {
  type = number
}

variable "monthly_backup_months" {
  type = number
}

variable "backup_redundancy" {
  type = string
}

variable "tags" {
  type = map(string)
}

resource "azuread_group" "admins" {
  display_name            = var.admins_group_name
  security_enabled        = true
  prevent_duplicate_names = true
  owners                  = var.operator_object_ids
}

# Members by their own resources, as the identity of the application (modules/app), so that no apply takes one out.
resource "azuread_group_member" "operators" {
  for_each = toset(var.operator_object_ids)

  group_object_id  = azuread_group.admins.object_id
  member_object_id = each.value
}

resource "azurerm_mssql_server" "main" {
  name                = var.server_name
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = "12.0"
  minimum_tls_version = "1.2"
  tags                = var.tags

  azuread_administrator {
    login_username              = azuread_group.admins.display_name
    object_id                   = azuread_group.admins.object_id
    tenant_id                   = var.tenant_id
    azuread_authentication_only = true
  }
}

# Only services inside Azure, such as the App Service, reach the server; an operator adds their address by hand when
# needed and removes it after.
resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAllWindowsAzureIps"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "main" {
  name                 = var.database_name
  server_id            = azurerm_mssql_server.main.id
  sku_name             = var.sku
  max_size_gb          = var.max_size_gb
  storage_account_type = var.backup_redundancy
  tags                 = var.tags

  short_term_retention_policy {
    retention_days = var.restore_days
  }

  dynamic "long_term_retention_policy" {
    for_each = var.monthly_backup_months > 0 ? [var.monthly_backup_months] : []
    content {
      monthly_retention = "P${long_term_retention_policy.value}M"
    }
  }

  lifecycle {
    precondition {
      condition     = var.sku != "Basic" || (var.restore_days <= 7 && var.max_size_gb <= 2)
      error_message = "The Basic plan keeps at most 7 days of restore and 2 GB."
    }
  }
}

output "server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "database_name" {
  value = azurerm_mssql_database.main.name
}

output "database_id" {
  value = azurerm_mssql_database.main.id
}

output "admins_group_object_id" {
  value = azuread_group.admins.object_id
}
