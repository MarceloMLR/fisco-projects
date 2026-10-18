# Design: pipeline de PR (Fundação-3)

## Context

Motivação em proposal.md. O repositório tem a solução .NET 10 (`global.json` com `rollForward: latestFeature`), testes xUnit v3 que sobem SQL Server com Testcontainers, `.editorconfig` verificado por `dotnet format`, specs do OpenSpec e o `cspell.json` usado pela extensão do VS Code. Não há frontend nem Terraform ainda. O repositório é público, e a regra "Protect main" já exige PR. A sessão de nuvem não altera regras do GitHub (o proxy bloqueia); isso é feito pelo `gh` da máquina de Marcelo.

## Goals / Non-Goals

**Goals:**

- Uma verificação só (`verify`), exigida na `main`, que mostre de uma vez todos os problemas do pull request.
- Trocar o GitHub Actions pelo Azure DevOps mexendo só no arquivo do workflow.

**Non-Goals:**

- Cache de pacotes e da imagem do SQL Server: o tempo de hoje (poucos minutos) não justifica.
- Relatórios de teste e cobertura publicados no PR.

## Decisions

### 1. Um script por verificação, em bash

`scripts/ci/build.sh`, `test.sh`, `format.sh`, `specs.sh` e `spelling.sh`. Cada script começa com `set -euo pipefail` e vai para a raiz do repositório, então para no primeiro erro e roda de qualquer pasta. Na máquina de Marcelo, rodam no Git Bash, que vem com o Git for Windows.

- Alternativa: PowerShell 7. Funciona nos dois sistemas, mas é instalação extra no Linux e nos contêineres de desenvolvimento; o bash é o shell padrão dos executores do GitHub e do Azure DevOps.
- Alternativa: um script único. Mais simples, mas o PR mostraria uma etapa só, sem dizer qual verificação falhou.

### 2. Workflow só como casca

`.github/workflows/pull-request.yml`, disparado por `pull_request` para a `main`, com um job `verify`:

- prepara o .NET (`actions/setup-dotnet` com `global-json-file`, que respeita o `rollForward`) e o Node (`actions/setup-node` com `.nvmrc`) e chama os scripts, uma etapa por script;
- testes só rodam se o build passou; formatação, specs e ortografia rodam mesmo depois de uma falha anterior, para o PR mostrar todos os problemas numa execução só;
- `permissions: contents: read`, nenhum segredo, `concurrency` que cancela a execução anterior do mesmo PR e limite de 20 minutos;
- ações fixadas pelo SHA do commit, com a versão em comentário (checkout v7.0.1, setup-dotnet v6.0.0, setup-node v7.1.0), e executor `ubuntu-24.04`, não `ubuntu-latest`.

Um PR vindo de fork roda com o mesmo token só de leitura; o workflow não usa `pull_request_target`.

### 3. Ferramentas de Node fixadas com lockfile

`scripts/ci/package.json` e `package-lock.json` fixam o OpenSpec CLI 1.14.3, o `cspell` e o `@cspell/dict-pt-br`; os scripts instalam com `npm ci --prefix scripts/ci`. O OpenSpec roda com a telemetria desligada (`OPENSPEC_TELEMETRY=0`), com `validate --all --strict` e `validate --archived` (changes arquivadas com todas as tarefas feitas).

- Alternativa: `npm i -g` no workflow. Versões fora do lockfile e lógica no workflow, contra o ADR-0055.
- `.nvmrc` na raiz com `24`. A troca para o Node 26, LTS a partir de 28/10/2026, é uma change própria (ADR-0060).

### 4. Ortografia com a mesma configuração do VS Code

`scripts/ci/spelling.json` importa o `cspell.json` da raiz e o dicionário de português do `node_modules` de `scripts/ci`. O `cspell.json` da raiz não muda, então a extensão do VS Code continua sem depender desse `node_modules`.

### 5. Pacotes vulneráveis pelo build

O `Directory.Build.props` passa a declarar `NuGetAudit`, `NuGetAuditMode=all` (inclui transitivos) e `NuGetAuditLevel=low`. Com avisos como erro, qualquer alerta NU1901 a NU1904 quebra o build, que já roda no PR. Não há etapa separada.

### 6. Testes do workflow e dos scripts

`tests/FiscoProjects.Pipeline.Tests` lê o workflow e os scripts como texto e confere os cenários da spec: gatilho, ordem das etapas, só scripts existentes, sem segredos, permissões, ações fixadas pelo commit, `set -euo pipefail`, ida para a raiz e auditoria do NuGet. O workflow segue um formato simples e fixo para isso.

- Alternativa: ler o YAML com uma biblioteca (YamlDotNet). Mais robusto a mudanças de formatação, mas é dependência nova para pouco ganho.

### 7. Verificação exigida na `main`

Depois da primeira execução no PR desta change, a regra "Protect main" ganha a verificação obrigatória `verify` (GitHub Actions), com a branch atualizada em relação à `main`. A alteração é feita pelo `gh` da máquina de Marcelo, com a autorização dele, e conferida pela API. É configuração do GitHub, fora do repositório, sem teste automatizado.

## Risks / Trade-offs

- [Renomear o job quebra o merge até a regra ser atualizada] → O nome `verify` fica documentado no `CLAUDE.md` e no teste "Todas as verificações no pull request".
- [Os testes do workflow dependem do formato do arquivo] → O teste que falhar aponta a regra; reformatar o YAML exige ajustar o teste junto.
- [A imagem do SQL Server (cerca de 1,5 GB) é baixada a cada execução] → Leva perto de um minuto; cache só se o tempo pesar.
- [Palavras novas em português reprovam a ortografia] → Mesmo comportamento do VS Code; a palavra entra na lista `words` do `cspell.json`.
- [Se o repositório voltar a ser privado, o plano gratuito tem 2.000 minutos por mês] → Cada execução usa poucos minutos.

## Migration Plan

Sem dados. Para desfazer, basta reverter o PR e tirar a verificação obrigatória da regra "Protect main".

## Open Questions

Nenhuma.
