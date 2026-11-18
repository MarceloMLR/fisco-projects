## 1. Decisão

- [x] 1.1 ADR-0162 e o índice dos ADRs

## 2. Código da infraestrutura

- [x] 2.1 Prefixo `gerenware`, banco, estado do Terraform e nome curto dos alertas no Terraform, nos `environments/*.backend.hcl` e nos scripts de `infra/scripts` e de `scripts/cd`
- [x] 2.2 Pacote da versão `gerenware-vX.Y.Z.zip` no `release.sh` e no `Publish-Version.ps1`
- [x] 2.3 Testes do pipeline com os nomes novos; `terraform validate` pelo `scripts/ci/infra.sh`

## 3. Documentação

- [x] 3.1 Guias, `CLAUDE.md` e as specs `infraestrutura` e `publicacao` com os nomes novos; `docs/roadmap.md`; `openspec validate --all --strict` sem erros

## Workflow follow-up

- Archive na mesma branch depois da aprovação do PR.
- Depois do merge, quem opera segue o roteiro do `design.md` ("Migration Plan") no `dev` e no `prod`: destruir com o código antigo, preparar o estado novo, aplicar, dar as permissões de novo, publicar, instalar o Teams e conferir.
