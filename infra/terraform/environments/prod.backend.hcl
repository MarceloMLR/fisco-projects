# State of the production environment, in the storage account created by infra/scripts/Prepare-TerraformState.ps1 (ADR-0053).
resource_group_name  = "rg-gerenware-mlr-tfstate"
storage_account_name = "stgwmlrtfstate"
container_name       = "tfstate"
key                  = "prod.tfstate"
