# Where the telemetry of the application goes (ADR-0057, ADR-0112): a Log Analytics workspace and the Application
# Insights on it, with a daily cap.

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "log_name" {
  type = string
}

variable "insights_name" {
  type = string
}

variable "daily_cap_gb" {
  type = number
}

variable "tags" {
  type = map(string)
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = var.log_name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = var.daily_cap_gb
  tags                = var.tags
}

resource "azurerm_application_insights" "main" {
  name                 = var.insights_name
  resource_group_name  = var.resource_group_name
  location             = var.location
  application_type     = "web"
  workspace_id         = azurerm_log_analytics_workspace.main.id
  daily_data_cap_in_gb = var.daily_cap_gb
  tags                 = var.tags
}

output "insights_id" {
  value = azurerm_application_insights.main.id
}

output "workspace_id" {
  value = azurerm_log_analytics_workspace.main.id
}

# Not a credential: it only lets telemetry in, and it is in the state with the resource anyway (ADR-0114).
output "insights_connection_string" {
  value     = azurerm_application_insights.main.connection_string
  sensitive = true
}
