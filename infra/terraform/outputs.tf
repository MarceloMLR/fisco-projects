output "app_url" {
  description = "Address of the application."
  value       = "https://${local.host}"
}

output "app_name" {
  description = "Name of the App Service, where the pipeline publishes the application."
  value       = module.app.name
}

output "resource_group" {
  description = "Resource group of the installation."
  value       = azurerm_resource_group.main.name
}

output "key_vault_name" {
  description = "Key Vault where the secrets are created by hand (ADR-0053)."
  value       = module.key_vault.name
}

output "database_server" {
  description = "Address of the Azure SQL server."
  value       = module.database.server_fqdn
}

output "database_name" {
  description = "Name of the database."
  value       = module.database.database_name
}

output "entra_client_id" {
  description = "Client ID of the application in Entra ID."
  value       = module.entra.client_id
}
