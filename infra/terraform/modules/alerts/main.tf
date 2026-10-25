# The availability test on /health and the alerts by e-mail to the administrator (ADR-0057, ADR-0112): system down,
# more errors, e-mail queue stopped and database near its limit.

variable "enabled" {
  description = "Off while the application is not published, so that the test does not fail and send e-mails."
  type        = bool
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "name_prefix" {
  type = string
}

variable "action_group_name" {
  type = string
}

variable "alert_email" {
  type = string
}

variable "insights_id" {
  type = string
}

variable "workspace_id" {
  type = string
}

variable "health_url" {
  type = string
}

variable "test_frequency" {
  type = number

  validation {
    condition     = contains([300, 600, 900], var.test_frequency)
    error_message = "Use 300, 600 or 900."
  }
}

variable "test_locations" {
  type = list(string)
}

variable "failed_requests_threshold" {
  type = number
}

variable "database_id" {
  type = string
}

variable "tags" {
  type = map(string)
}

locals {
  count = var.enabled ? 1 : 0
  # The window of the availability alert covers at least one run of the test.
  availability_window = var.test_frequency <= 300 ? "PT5M" : "PT15M"
  # Seconds the oldest e-mail may wait before the queue counts as stopped (ADR-0112).
  queue_stopped_after = 900
}

resource "azurerm_monitor_action_group" "admin" {
  name                = var.action_group_name
  resource_group_name = var.resource_group_name
  short_name          = "fiscoproj"
  tags                = var.tags

  email_receiver {
    name                    = "administrador"
    email_address           = var.alert_email
    use_common_alert_schema = true
  }
}

resource "azurerm_application_insights_standard_web_test" "health" {
  count = local.count

  name                    = "wt-${var.name_prefix}-health"
  resource_group_name     = var.resource_group_name
  location                = var.location
  application_insights_id = var.insights_id
  geo_locations           = var.test_locations
  frequency               = var.test_frequency
  retry_enabled           = true
  enabled                 = true
  tags                    = var.tags

  request {
    url                              = var.health_url
    parse_dependent_requests_enabled = false
  }

  validation_rules {
    expected_status_code = 200
    ssl_check_enabled    = true
  }
}

resource "azurerm_monitor_metric_alert" "down" {
  count = local.count

  name                = "alert-${var.name_prefix}-fora-do-ar"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights_standard_web_test.health[0].id, var.insights_id]
  description         = "O FiscoProjects não respondeu ao teste de disponibilidade."
  severity            = 1
  frequency           = "PT1M"
  window_size         = local.availability_window
  tags                = var.tags

  application_insights_web_test_location_availability_criteria {
    web_test_id           = azurerm_application_insights_standard_web_test.health[0].id
    component_id          = var.insights_id
    failed_location_count = 1
  }

  action {
    action_group_id = azurerm_monitor_action_group.admin.id
  }
}

resource "azurerm_monitor_metric_alert" "errors" {
  count = local.count

  name                = "alert-${var.name_prefix}-erros"
  resource_group_name = var.resource_group_name
  scopes              = [var.insights_id]
  description         = "Muitas requisições com falha nos últimos 15 minutos."
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  tags                = var.tags

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "requests/failed"
    aggregation      = "Count"
    operator         = "GreaterThan"
    threshold        = var.failed_requests_threshold
  }

  action {
    action_group_id = azurerm_monitor_action_group.admin.id
  }
}

resource "azurerm_monitor_scheduled_query_rules_alert_v2" "queue_stopped" {
  count = local.count

  name                    = "alert-${var.name_prefix}-fila-parada"
  resource_group_name     = var.resource_group_name
  location                = var.location
  scopes                  = [var.workspace_id]
  description             = "Um e-mail espera envio há mais de 15 minutos: a fila de e-mails parou."
  severity                = 2
  evaluation_frequency    = "PT15M"
  window_duration         = "PT15M"
  auto_mitigation_enabled = true
  # The metric only exists after the application sends it.
  skip_query_validation = true
  tags                  = var.tags

  criteria {
    query                   = <<-QUERY
      AppMetrics
      | where Name == "fiscoprojects.emails.oldest_waiting"
      | summarize Oldest = max(Max)
    QUERY
    time_aggregation_method = "Maximum"
    metric_measure_column   = "Oldest"
    operator                = "GreaterThan"
    threshold               = local.queue_stopped_after

    failing_periods {
      minimum_failing_periods_to_trigger_alert = 1
      number_of_evaluation_periods             = 1
    }
  }

  action {
    action_groups = [azurerm_monitor_action_group.admin.id]
  }
}

resource "azurerm_monitor_metric_alert" "database_full" {
  count = local.count

  name                = "alert-${var.name_prefix}-banco-cheio"
  resource_group_name = var.resource_group_name
  scopes              = [var.database_id]
  description         = "O banco passou de 80% do tamanho máximo do plano."
  severity            = 2
  frequency           = "PT15M"
  window_size         = "PT1H"
  tags                = var.tags

  criteria {
    metric_namespace = "Microsoft.Sql/servers/databases"
    metric_name      = "storage_percent"
    aggregation      = "Maximum"
    operator         = "GreaterThan"
    threshold        = 80
  }

  action {
    action_group_id = azurerm_monitor_action_group.admin.id
  }
}
