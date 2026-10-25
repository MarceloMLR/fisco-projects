# Development and demonstration environment of Marcelo's installation (ADR-0052, ADR-0114). The personal values
# (operator_object_ids, alert_email) stay out of the repository, in infra/terraform/local.auto.tfvars or TF_VAR_*.
installation = "mlr"
environment  = "dev"

# The trial subscription has no quota for the App Service in Brazil South: the application and the database go to
# West US 3, only in this environment, with fictitious data (ADR-0114). Back to Brazil South with quota.
app_service_sku = "B1"
app_location    = "westus3"

# Development until the integrations with Microsoft 365: local sign-in, files and e-mails in the persistent folder of
# the App Service, and the migrations applied by the application itself.
aspnetcore_environment = "Development"
app_is_database_admin  = true
app_settings = {
  FileStorage__Local__RootPath = "/home/data/files"
  Email__Local__Folder         = "/home/data/emails"
}

# Basic instead of the free offer, which the background processes would use up in a few days (ADR-0114); without
# monthly backups, so that destroying the environment leaves no costs.
database_sku                   = "Basic"
database_max_size_gb           = 2
database_restore_days          = 7
database_monthly_backup_months = 0
database_backup_redundancy     = "Local"

key_vault_soft_delete_days = 7

# Fewer runs of the availability test: each one is billed.
availability_test_frequency = 900

# Off until the Fundação-7 publishes the application.
alerts_enabled = false
