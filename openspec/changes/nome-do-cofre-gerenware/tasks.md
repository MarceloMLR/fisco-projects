## 1. Infraestrutura

- [x] 1.1 Nome curto `gw<instalação><ambiente>` no `infra/terraform/main.tf`; `terraform validate` pelo `scripts/ci/infra.sh`
- [x] 1.2 Spec `infraestrutura` com o nome do cofre; `docs/roadmap.md`; `openspec validate --all --strict` sem erros

## Workflow follow-up

- Archive na mesma branch depois da aprovação do PR.
- No `dev`, quem opera aplica de novo o Terraform com este código, que cria o cofre `kv-gwmlrdev`, e segue o roteiro da Marca-6 a partir dos segredos da Autentique.
