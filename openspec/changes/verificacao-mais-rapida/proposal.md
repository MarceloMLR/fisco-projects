# Proposta: verificação mais rápida (Fundação-3.2)

## Why

A verificação do PR leva de 5,5 a 7 minutos porque tudo roda em sequência num job só. O push do archive, que só move a proposta e atualiza specs e roadmap, espera o mesmo tempo antes do merge. Marcelo pediu para manter a verificação e deixá-la mais rápida, com a recomendação aprovada no chat em 19/10/2026: partes em paralelo, cache dos pacotes e só as specs quando o commit muda apenas a documentação.

## What Changes

- **Jobs paralelos** (ADR-0090): `backend` (build e testes), `format`, `frontend` e `specs` rodam ao mesmo tempo. O job `verify`, com o mesmo nome exigido pela regra "Protect main", espera os outros e decide o resultado pelo script `scripts/ci/verdict.sh`.
- **Cache:** os pacotes do NuGet e do npm ficam em cache entre as execuções.
- **Só documentação:** o novo job `changes` roda `scripts/ci/changes.sh`, que compara o commit com o último commit do PR que passou na verificação, ou com a `main` se não houver nenhum.
  - Se só mudou documentação (`docs/`, `openspec/`, `.claude/` e arquivos `.md` fora de `web/`), backend, formatação e frontend são pulados, e as specs rodam.
  - Se a `main` mudou código depois da última verificação, tudo roda.
- **Permissões só de leitura:** além do conteúdo, as verificações e os pull requests, para o `changes.sh` achar o último commit verificado pela API do GitHub. Nenhum segredo.
- **ADR-0090** (novo, proposta), que substitui em parte o ADR-0055 (a linha "Em todo pull request" da tabela).

## Non-goals

- Dividir os testes do backend em mais jobs, ou acelerar os testes da API, o maior tempo (cerca de 2,5 minutos): fica para quando pesar de novo.
- Pular só o backend quando muda só o frontend, ou o contrário: os testes do backend leem o contrato do frontend (`web/src/api/openapi.json`), e a regra ficaria frágil.
- Mudar o que cada verificação confere: os scripts `build.sh`, `test.sh`, `format.sh`, `frontend.sh` e `specs.sh` continuam iguais.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `integracao-continua`: as verificações em jobs paralelos com o `verify` decidindo o resultado, a dispensa do código no commit que só muda documentação, o cache e as permissões de leitura.

## Impact

- **Workflow:** `.github/workflows/pull-request.yml` com os jobs `changes`, `backend`, `format`, `frontend`, `specs` e `verify`.
- **Scripts:** `scripts/ci/changes.sh` e `scripts/ci/verdict.sh`, novos; os outros não mudam.
- **Testes:** `tests/FiscoProjects.Pipeline.Tests`, para o novo formato do workflow e para os dois scripts novos, com repositórios Git de exemplo.
- **Documentação:**
  - ADR-0090, e ADR-0055 marcado como substituído em parte;
  - `CLAUDE.md`, na seção da verificação do PR;
  - roadmap, com a Fundação-3.2.
