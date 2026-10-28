# Production of Marcelo's installation (ADR-0052, ADR-0125), not created yet: the defaults of variables.tf are those of
# production, so only the names and the repository of the pipeline go here. The personal values (operator_object_ids,
# alert_email, first_administrator_object_id, sharepoint_site_url, email_mailbox) stay out of the repository, in
# infra/terraform/local.auto.tfvars or TF_VAR_*.
installation = "mlr"
environment  = "prod"

# The pipeline of this repository publishes the versions by hand, from the GitHub environment "prod" (ADR-0125), named
# by the ids that GitHub puts in the subject of its tokens (ADR-0116).
github_repository = "MarceloMLR@106113485/fisco-projects@1423675463"
