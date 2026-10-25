# Parameters of an installation and environment (ADR-0053). The defaults are those of production (ADR-0048, ADR-0052,
# ADR-0059); environments/<environment>.tfvars changes what differs. Nothing here is a secret.

variable "installation" {
  description = "Short code of the installation, part of every name: 2 to 6 lowercase letters or digits."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{2,6}$", var.installation))
    error_message = "Use 2 to 6 lowercase letters or digits."
  }
}

variable "environment" {
  description = "Environment of the installation: prod, or dev for development and demonstration (ADR-0052)."
  type        = string

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "Use dev or prod."
  }
}

variable "location" {
  description = "Azure region (ADR-0052)."
  type        = string
  default     = "brazilsouth"
}

variable "operator_object_ids" {
  description = "Object IDs in Entra ID of who operates the installation: owners of the Entra objects, administrators of the database and of the Key Vault. Personal identifiers: passed outside the repository (local.auto.tfvars or TF_VAR_operator_object_ids)."
  type        = list(string)

  validation {
    condition     = length(var.operator_object_ids) > 0
    error_message = "Give at least one operator."
  }
}

variable "alert_email" {
  description = "E-mail of the administrator who receives the alerts (ADR-0057, ADR-0112). Passed outside the repository."
  type        = string
}

variable "aspnetcore_environment" {
  description = "ASPNETCORE_ENVIRONMENT of the application. Development only in the dev environment, before the integrations with Microsoft 365 (ADR-0114)."
  type        = string
  default     = "Production"

  validation {
    condition     = contains(["Development", "Production"], var.aspnetcore_environment)
    error_message = "Use Development or Production."
  }
}

variable "app_service_sku" {
  description = "Plan of the App Service (ADR-0052)."
  type        = string
  default     = "B1"
}

variable "app_settings" {
  description = "Settings of the application that vary by environment, without secrets (for example, the folders of the local files)."
  type        = map(string)
  default     = {}
}

variable "syncfusion_license_in_key_vault" {
  description = "Whether the secret Syncfusion--LicenseKey already exists in the Key Vault; when true, the application reads it from there (ADR-0051, ADR-0087)."
  type        = bool
  default     = false
}

variable "app_is_database_admin" {
  description = "Whether the identity of the application is in the group of database administrators, to apply the migrations itself (only in the dev environment, ADR-0114)."
  type        = bool
  default     = false
}

variable "database_sku" {
  description = "Plan of the Azure SQL database: S0, or Basic to start (ADR-0048, ADR-0114)."
  type        = string
  default     = "S0"
}

variable "database_max_size_gb" {
  description = "Maximum size of the database, in GB (Basic: up to 2)."
  type        = number
  default     = 250
}

variable "database_restore_days" {
  description = "Days of point-in-time restore (ADR-0059): 14, or 7 in the Basic plan."
  type        = number
  default     = 14
}

variable "database_monthly_backup_months" {
  description = "Months each monthly backup is kept (ADR-0059); 0 keeps none, for an environment that is destroyed without leaving costs."
  type        = number
  default     = 12
}

variable "database_backup_redundancy" {
  description = "Where the backups are kept (ADR-0059): Zone (inside the region), Local, Geo or GeoZone."
  type        = string
  default     = "Zone"

  validation {
    condition     = contains(["Zone", "Local", "Geo", "GeoZone"], var.database_backup_redundancy)
    error_message = "Use Zone, Local, Geo or GeoZone."
  }
}

variable "key_vault_soft_delete_days" {
  description = "Days a deleted secret or vault can be recovered (7 to 90)."
  type        = number
  default     = 90
}

variable "log_daily_cap_gb" {
  description = "Daily cap of the telemetry kept, in GB, so that a peak does not leave the free band (ADR-0112)."
  type        = number
  default     = 1
}

variable "alerts_enabled" {
  description = "Whether the availability test and the alerts are on; off while the application is not published."
  type        = bool
  default     = true
}

variable "availability_test_frequency" {
  description = "Seconds between runs of the availability test: 300, 600 or 900. Each run is billed."
  type        = number
  default     = 300
}

variable "availability_test_locations" {
  description = "Where the availability test runs from; each location is one run more."
  type        = list(string)
  default     = ["latam-br-gru-edge"]
}

variable "failed_requests_threshold" {
  description = "Failed requests in 15 minutes that raise the alert of errors."
  type        = number
  default     = 10
}
