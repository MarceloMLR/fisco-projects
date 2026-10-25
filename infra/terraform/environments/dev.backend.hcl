# State of the dev environment, in the storage account created by infra/scripts/Prepare-TerraformState.ps1 (ADR-0053).
resource_group_name  = "rg-fiscoprojects-mlr-tfstate"
storage_account_name = "stfpmlrtfstate"
container_name       = "tfstate"
key                  = "dev.tfstate"
