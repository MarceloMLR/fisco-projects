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
  # With the shared mailbox, the e-mails to the people of the demonstration stay in the folder (ADR-0123).
  Email__Graph__LocalDomains__0 = "empresa.example"
  # The requests of the local provider in the persistent folder, and those of the signers of the demonstration stay in
  # it beside the Autentique (ADR-0130).
  Signature__Local__Folder   = "/home/data/signatures"
  Signature__LocalDomains__0 = "cliente.example"
  Signature__LocalDomains__1 = "empresa.example"
  # The first start applies every migration on the Basic plan, which takes a few minutes.
  WEBSITES_CONTAINER_START_TIME_LIMIT = "600"
}

# Basic instead of the free offer, which the background processes would use up in a few days (ADR-0114); without
# monthly backups, so that destroying the environment leaves no costs.
database_sku                   = "Basic"
database_max_size_gb           = 2
database_restore_days          = 7
database_monthly_backup_months = 0
database_backup_redundancy     = "Local"

key_vault_soft_delete_days = 7

# The sandbox of the Autentique, with its notice: the secrets Signature--Autentique--ApiKey and
# Signature--Autentique--WebhookSecret are created by hand in the Key Vault (docs/instalacao.md, ADR-0130).
autentique = {
  sandbox = true
  webhook = true
}

# Fewer runs of the availability test: each one is billed.
availability_test_frequency = 900

# The pipeline of this repository publishes the environment after each merge (ADR-0115), named by the ids that GitHub
# puts in the subject of its tokens (ADR-0116).
github_repository = "MarceloMLR@106113485/fisco-projects@1423675463"

# On since the Fundação-7 publishes the application.
alerts_enabled = true
