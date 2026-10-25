# The subscription and the tenant come from the Azure CLI (az login) or from the ARM_* variables of the pipeline.
provider "azurerm" {
  # Only the resource providers this configuration uses (since azurerm 5.0, none by default).
  resource_providers_to_register = [
    "Microsoft.AlertsManagement",
    "Microsoft.Insights",
    "Microsoft.KeyVault",
    "Microsoft.OperationalInsights",
    "Microsoft.Sql",
    "Microsoft.Web",
  ]

  features {
    key_vault {
      # The vault has purge protection (ADR-0059): a destroyed vault stays recoverable, and applying again recovers it.
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

provider "azuread" {}
