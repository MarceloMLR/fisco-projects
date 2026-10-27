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

variable "app_location" {
  description = "Region of the application and of the database, when the subscription has no quota for the App Service in the region of the installation; null keeps them in location. Only for the dev environment, with fictitious data (ADR-0114)."
  type        = string
  default     = null
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
  description = "E-mail of the administrator who receives the alerts (ADR-0057, ADR-0112). Passed outside the repository, and kept out of the plans, which the pipeline shows (ADR-0115)."
  type        = string
  sensitive   = true
}

variable "github_repository" {
  description = "Repository on GitHub whose pipeline publishes this environment, as the immutable subject of GitHub names it (owner@owner_id/name@repository_id), which a renamed or recreated repository does not take over; null creates no identity for a pipeline (ADR-0115, ADR-0116)."
  type        = string
  default     = null

  validation {
    condition     = var.github_repository == null || can(regex("^[^@/]+@[0-9]+/[^@/]+@[0-9]+$", var.github_repository))
    error_message = "Use owner@owner_id/name@repository_id, with the ids of gh api repos/<owner>/<name> (owner.id and id)."
  }
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

variable "entra_sign_in" {
  description = "Whether people sign in through Entra ID (ADR-0117). The application of Entra ID is ready either way."
  type        = bool
  default     = true
}

variable "entra_assignment_required" {
  description = "Whether only the people and groups assigned to the application in Entra ID sign in (ADR-0005)."
  type        = bool
  default     = true
}

variable "first_administrator_object_id" {
  description = "Object ID in Entra ID of the first administrator of the installation (ADR-0006), in local.auto.tfvars; null leaves the first administrator out of the settings."
  type        = string
  default     = null
}

variable "sharepoint_site_url" {
  description = "Address of the SharePoint site of the files (ADR-0030, ADR-0120), in local.auto.tfvars: it names the tenant. Null keeps the file storage of app_settings."
  type        = string
  default     = null

  validation {
    condition     = var.sharepoint_site_url == null || can(regex("^https://[^/?#]+(/[^?#]*)?$", var.sharepoint_site_url))
    error_message = "The SharePoint site is an https address, as https://contoso.sharepoint.com/sites/Projetos."
  }
}

variable "sharepoint_library" {
  description = "Document library of the SharePoint site; null for the default library of the site."
  type        = string
  default     = null
}

variable "sharepoint_root_folder" {
  description = "Folder inside the library where the files of the system go (ADR-0030), created when missing; null for the library itself."
  type        = string
  default     = null
}

variable "email_mailbox" {
  description = "Address of the shared mailbox the e-mails leave from through Microsoft Graph (ADR-0036, ADR-0123), in local.auto.tfvars: it names the domain of the tenant. Null keeps the e-mail sending of app_settings."
  type        = string
  default     = null

  validation {
    condition     = var.email_mailbox == null || can(regex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", var.email_mailbox))
    error_message = "The shared mailbox is an e-mail address, as projetos@empresa.com.br."
  }
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
