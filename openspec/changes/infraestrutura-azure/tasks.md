## 1. Decisão

- [x] 1.1 ADR-0114 (como o Terraform monta uma instalação no Azure)

## 2. Terraform

- [x] 2.1 Versões fixadas, provedores e estado no `azurerm` com Entra ID
- [x] 2.2 Módulos `app`, `database`, `key-vault`, `monitoring`, `alerts` e `entra`
- [x] 2.3 Parâmetros do ambiente de desenvolvimento e o arquivo de travas dos provedores
- [x] 2.4 `infra/scripts/Prepare-TerraformState.ps1`

## 3. Verificação

- [x] 3.1 `scripts/ci/infra.sh` e o job `infra` no workflow e no veredito
- [x] 3.2 Testes da infraestrutura, do workflow e do veredito

## 4. Ambiente de desenvolvimento

- [x] 4.1 Estado preparado e `terraform apply` na assinatura de Marcelo
- [x] 4.2 Conferência dos recursos criados

## 5. Documentação

- [x] 5.1 `docs/instalacao.md` (como aplicar, segredos e como apagar) e `docs/custos-estimados.md`
- [x] 5.2 `CLAUDE.md`, glossário e roadmap
