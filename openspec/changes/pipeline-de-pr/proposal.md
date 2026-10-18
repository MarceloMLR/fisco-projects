# Proposta: pipeline de PR (Fundação-3)

## Why

Hoje as verificações (build sem avisos, testes, formatação, specs e ortografia) só rodam quando alguém lembra de rodá-las, e a regra "Protect main" exige PR, mas não verificações aprovadas. O ADR-0055 pede que todo pull request passe por essas verificações no GitHub Actions antes de entrar na `main`, com a lógica em scripts do repositório para facilitar uma futura migração ao Azure DevOps.

## What Changes

- Workflow do GitHub Actions que roda em todo pull request para a `main`: build sem avisos, testes (com SQL Server em contêiner), formatação, validação das specs do OpenSpec e ortografia, cada um como uma etapa.
- Scripts em `scripts/ci/`, um por verificação; o workflow só prepara .NET e Node e chama os scripts. Os mesmos scripts rodam na máquina de desenvolvimento.
- Auditoria de pacotes do NuGet declarada no `Directory.Build.props`: pacote com vulnerabilidade conhecida, direto ou transitivo, quebra o build.
- Ferramentas de Node usadas nas verificações (OpenSpec CLI 1.14.3, cspell e o dicionário de português) fixadas em `scripts/ci/package.json`, com lockfile; `.nvmrc` com o Node 24.
- Workflow sem segredos, só com permissão de leitura, ações fixadas pelo commit e executor fixo.
- Testes que conferem o workflow e os scripts.
- A regra "Protect main" passa a exigir a verificação aprovada para o merge.
- `CLAUDE.md` com os scripts de verificação.

## Non-goals

- Lint, testes e tipos do frontend (Fundação-4) e `terraform fmt`, `validate` e `plan` (Fundação-6): entram nesses scripts quando existirem.
- Deploy no merge e em produção (Fundação-7 e Operação).
- Cobertura mínima de 80% em `Domain` e `Application` (ADR-0056): entra com o primeiro módulo (Acesso-1), quando houver código nessas camadas.
- Alertas do Dependabot e atualização automática de versões (ADR-0060): configuração do repositório, fora desta change.

## Capabilities

### New Capabilities

- `integracao-continua`: verificações automáticas de todo pull request para a `main`, como são executadas e com que garantias de segurança.

### Modified Capabilities

Nenhuma.

## Impact

- Arquivos novos: `.github/workflows/pull-request.yml`, `scripts/ci/*.sh`, `scripts/ci/package.json` e `package-lock.json`, `scripts/ci/spelling.json`, `.nvmrc`, `tests/FiscoProjects.Pipeline.Tests`.
- Arquivos alterados: `Directory.Build.props` (auditoria do NuGet explícita), `CLAUDE.md`, `FiscoProjects.slnx`.
- Dependências novas, só nas verificações: `cspell` e `@cspell/dict-pt-br` (MIT); o OpenSpec CLI já é usado no projeto. Ações oficiais `actions/checkout`, `actions/setup-dotnet` e `actions/setup-node` (MIT).
- Configuração do GitHub: verificação obrigatória na regra "Protect main".
- Plano gratuito do GitHub Actions: o repositório é público, sem limite de minutos.
- ADRs implementados: 0055 (verificações do PR, scripts no repositório, regra da `main`), 0060 (versões fixadas) e 0056 (testes no CI).
