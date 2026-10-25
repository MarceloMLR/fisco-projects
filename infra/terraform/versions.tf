# Versions of Terraform and of the providers, pinned (ADR-0053, ADR-0060). The code stays compatible with OpenTofu.
terraform {
  required_version = "~> 1.16.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 5.9.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "= 3.10.0"
    }
  }

  # The state of each installation and environment in its own storage account, created by
  # infra/scripts/Prepare-TerraformState.ps1; the names come from environments/<environment>.backend.hcl.
  backend "azurerm" {
    use_azuread_auth = true
  }
}
