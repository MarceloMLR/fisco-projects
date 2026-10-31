## Why

Com a Linha do tempo própria (Gantt-1, ADR-0134), nada mais usa a chave do Syncfusion, mas a instalação continua pedindo e guardando a chave. O guia de instalação a pede à empresa e tem um passo para gravá-la no Key Vault. O Terraform liga a referência dela no App Service. A API a entrega ao navegador em `GET /api/client-settings` (ADR-0087), e o Codespaces e o desenvolvimento a leem dos segredos. Gantt-2, primeira change da v2 depois da Linha do tempo própria.

## What Changes

- **BREAKING:** sai a rota `GET /api/client-settings`, que só levava a chave. Nenhum cliente além das telas a chama, e as telas já não a usam desde a Gantt-1. O contrato e os tipos do frontend são gerados de novo, e sai a consulta `clientSettingsQuery`.
- Saem do Terraform a variável `syncfusion_license_in_key_vault` e a configuração `Syncfusion__LicenseKey` do App Service. O `dev.tfvars` deixa de ligar a chave.
- O guia de instalação deixa de pedir a chave do Syncfusion nestes lugares:
  - o que a empresa providencia;
  - o passo dos segredos no Key Vault;
  - o ambiente pelo repositório;
  - as licenças de terceiros;
  - o desenvolvimento.
- O guia ganha, em "Atualizar para uma versão nova", o que fazer numa instalação que ainda tem a chave: tirar a variável do `local.auto.tfvars`, aplicar o Terraform e apagar o segredo do Key Vault.
- O Codespaces, a demonstração e a restauração deixam de citar a chave.
- O ADR-0051 e o ADR-0087 passam a "substituída pelo ADR-0134".
- O roadmap marca a v2 a partir do grupo 13, Linha do tempo própria. A v1 termina na Assinatura e nos refinamentos do design.
- O contexto do OpenSpec, o discovery e o glossário deixam de citar o Syncfusion como parte do sistema.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `cronograma`: a Linha do tempo sem licença de terceiros, agora também sem a chave na instalação.
- `dados-de-demonstracao`: a demonstração no Codespaces sem a chave do Syncfusion.

## Non-goals

- Aplicar o Terraform no `dev` e apagar o segredo `Syncfusion--LicenseKey` do Key Vault. Isso fica com Marcelo depois do merge, pelo guia; até lá, o `plan.sh` da publicação avisa a diferença.
- Apagar o segredo do Codespaces e o dos segredos do usuário na máquina de Marcelo (opcional, dele).
- Importar do MS Project: Gantt-3.
- Publicar uma versão (tag) da v1 ou da v2.

## Impact

- API: `src/FiscoProjects.Api` (`ClientSettings.cs`, `ApiSetup.cs`) e `tests/FiscoProjects.Api.Tests` (`ClientSettingsTests`).
- Frontend: `web/src/api` (o contrato e os tipos gerados, `queries.ts`), `web/src/test/server.ts` e o teste de tela "Instalação sem chave de licença".
- Infraestrutura: `infra/terraform` (`variables.tf`, `main.tf`, `modules/app/main.tf`, `environments/dev.tfvars`) e `tests/FiscoProjects.Pipeline.Tests` (`InfrastructureTests`, `DevContainerTests`).
- Documentação: `docs/instalacao.md`, `docs/demonstracao.md`, `docs/restauracao.md`, `docs/discovery.md`, `docs/glossario.md`, `docs/roadmap.md`, `CLAUDE.md` e `openspec/config.yaml`.
- CI: o comentário do `scripts/ci/frontend.sh`; a verificação da fonte do Google continua.
- ADRs: 0051, 0087 e 0134.
