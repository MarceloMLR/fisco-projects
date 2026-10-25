# The Key Vault of the installation (ADR-0052, ADR-0059), with access by role: the operators administer it, and the
# application reads the secrets (modules/app). Secrets and certificates are created in it by hand, never by Terraform
# (ADR-0053).

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "name" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "administrators" {
  description = "Object IDs of who creates and changes the secrets."
  type        = list(string)
}

variable "soft_delete_days" {
  type = number
}

variable "tags" {
  type = map(string)
}

resource "azurerm_key_vault" "main" {
  name                       = var.name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = true
  soft_delete_retention_days = var.soft_delete_days
  tags                       = var.tags
}

resource "azurerm_role_assignment" "administrators" {
  for_each = toset(var.administrators)

  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = each.value
}

output "id" {
  value = azurerm_key_vault.main.id
}

output "name" {
  value = azurerm_key_vault.main.name
}
