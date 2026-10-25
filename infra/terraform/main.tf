# An installation of the FiscoProjects in Azure (ADR-0052, ADR-0053, ADR-0114): the application in the App Service, the
# database, the Key Vault, the monitoring and the application in Entra ID, all in one resource group.

locals {
  name  = "fiscoprojects-${var.installation}-${var.environment}"
  short = "fp${var.installation}${var.environment}"
  # Names are fixed here, so that a resource can point to another before it exists (the address of the application,
  # the database in the connection of the application).
  names = {
    resource_group = "rg-${local.name}"
    plan           = "asp-${local.name}"
    app            = "app-${local.name}"
    sql_server     = "sql-${local.name}"
    database       = "sqldb-fiscoprojects"
    key_vault      = "kv-${local.short}"
    log            = "log-${local.name}"
    insights       = "appi-${local.name}"
    action_group   = "ag-${local.name}"
  }
  host = "${local.names.app}.azurewebsites.net"
  # The application and the database stay together, for the latency of the queries.
  app_location = coalesce(var.app_location, var.location)
  tags = {
    aplicacao  = "FiscoProjects"
    instalacao = var.installation
    ambiente   = var.environment
  }
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "main" {
  name     = local.names.resource_group
  location = var.location
  tags     = local.tags
}

module "entra" {
  source = "./modules/entra"

  display_name = "FiscoProjects (${var.installation} ${var.environment})"
  owners       = var.operator_object_ids
}

module "database" {
  source = "./modules/database"

  resource_group_name   = azurerm_resource_group.main.name
  location              = local.app_location
  server_name           = local.names.sql_server
  database_name         = local.names.database
  admins_group_name     = "FiscoProjects ${var.installation} ${var.environment} - administradores do banco"
  operator_object_ids   = var.operator_object_ids
  sku                   = var.database_sku
  max_size_gb           = var.database_max_size_gb
  restore_days          = var.database_restore_days
  monthly_backup_months = var.database_monthly_backup_months
  backup_redundancy     = var.database_backup_redundancy
  tenant_id             = data.azurerm_client_config.current.tenant_id
  tags                  = local.tags
}

module "key_vault" {
  source = "./modules/key-vault"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  name                = local.names.key_vault
  tenant_id           = data.azurerm_client_config.current.tenant_id
  administrators      = var.operator_object_ids
  soft_delete_days    = var.key_vault_soft_delete_days
  tags                = local.tags
}

module "monitoring" {
  source = "./modules/monitoring"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  log_name            = local.names.log
  insights_name       = local.names.insights
  daily_cap_gb        = var.log_daily_cap_gb
  tags                = local.tags
}

module "app" {
  source = "./modules/app"

  resource_group_name    = azurerm_resource_group.main.name
  location               = local.app_location
  plan_name              = local.names.plan
  name                   = local.names.app
  sku                    = var.app_service_sku
  aspnetcore_environment = var.aspnetcore_environment
  site_url               = "https://${local.host}"
  database_server        = module.database.server_fqdn
  database_name          = module.database.database_name
  insights_connection    = module.monitoring.insights_connection_string
  key_vault_id           = module.key_vault.id
  key_vault_name         = module.key_vault.name
  syncfusion_in_vault    = var.syncfusion_license_in_key_vault
  app_settings           = var.app_settings
  database_admin         = var.app_is_database_admin
  database_admins_group  = module.database.admins_group_object_id
  tags                   = local.tags
}

module "alerts" {
  source = "./modules/alerts"

  enabled                   = var.alerts_enabled
  resource_group_name       = azurerm_resource_group.main.name
  location                  = var.location
  name_prefix               = local.name
  action_group_name         = local.names.action_group
  alert_email               = var.alert_email
  insights_id               = module.monitoring.insights_id
  workspace_id              = module.monitoring.workspace_id
  health_url                = "https://${local.host}/health"
  test_frequency            = var.availability_test_frequency
  test_locations            = var.availability_test_locations
  failed_requests_threshold = var.failed_requests_threshold
  database_id               = module.database.database_id
  tags                      = local.tags
}
