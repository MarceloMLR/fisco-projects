---
status: proposta
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0090: Verificação do PR em partes paralelas, com só as specs quando o commit muda apenas a documentação

Substitui em parte o ADR-0055: a linha "Em todo pull request" da tabela do fluxo.

## Contexto e problema

A verificação `verify` roda num job só, em sequência: build (cerca de 35 s), testes (cerca de 2 min 40 s), formatação (cerca de 30 s), frontend (cerca de 2 min) e specs. Cada PR espera de 5,5 a 7 minutos, e o archive, que só move a proposta e atualiza o roadmap e as specs, espera o mesmo tempo antes do merge. Como deixar a verificação mais rápida sem aceitar código que não passou pelos testes?

## Fatores de decisão

- Nada chega à `main` sem build, testes e formatação aprovados no mesmo código (ADR-0055).
- A regra "Protect main" exige a verificação `verify`: o nome do job não pode mudar.
- A lógica fica nos scripts de `scripts/ci`; o workflow só prepara as ferramentas e chama os scripts (ADR-0055).
- Workflow sem segredos, com permissões só de leitura e versões fixadas.

## Opções consideradas

- A. Manter o job único, em sequência
- B. Partes em jobs paralelos, com cache dos pacotes
- C. B, mais pular build, testes e frontend no commit que só muda documentação, quando o mesmo código já passou na verificação
- D. Filtro de caminhos no gatilho do workflow (`paths-ignore`)

## Resultado da decisão

Opção escolhida: **C**.

- **Jobs paralelos:** `backend` (build e testes), `format`, `frontend` e `specs` rodam ao mesmo tempo. O `verify`, com o mesmo nome de antes, espera todos e só aprova quando todos passaram, ou quando os pulados podiam ser pulados. O tempo cai para o do job mais longo, o do backend, perto de 3,5 minutos.
- **Cache:** os pacotes do NuGet, pela chave dos projetos e do `global.json`, e os do npm, pelos `package-lock.json`.
- **Só documentação:** o job `changes` (`scripts/ci/changes.sh`) compara o commit com o último commit do PR que passou na verificação. Sem ele, compara com a `main`. Se tudo o que mudou é documentação, o build, os testes, a formatação e o frontend não rodam, e as specs rodam sempre. Documentação é:
  - o que está em `docs/`, `openspec/` e `.claude/`;
  - todo arquivo `.md` fora de `web/`.
- **Comparação conservadora:** a comparação usa o commit que a verificação testa, que junta o PR com a `main` atual. Se a `main` mudou código depois da última verificação aprovada, a diferença inclui esse código e tudo roda.
- **Permissões:** para achar o último commit verificado, o `changes` lê pela API do GitHub os commits do PR e o resultado das verificações. As permissões continuam só de leitura: conteúdo, verificações e pull requests. Fora do GitHub, o script compara com a `main`.

### Consequências

- Bom, porque a verificação completa cai de 5,5 a 7 para cerca de 3,5 minutos.
- Bom, porque o push do archive, que só muda documentação, termina em cerca de 1 minuto.
- Ruim, porque o workflow fica com seis jobs em vez de um. Em repositório público os minutos não são cobrados. Se o repositório ficar privado, cada job é cobrado arredondado para cima, e a verificação completa passa a custar uns 3 minutos a mais.
- Ruim, porque achar o último commit verificado depende da API do GitHub. Na migração ao Azure DevOps, esse trecho do `changes.sh` é trocado pelo equivalente ou retirado, e aí tudo roda sempre.

### Confirmação

O workflow tem os jobs `changes`, `backend`, `format`, `frontend`, `specs` e `verify`, e todo passo que executa comandos chama um script de `scripts/ci`. O `verify` reprova quando uma parte falhou, ou quando uma parte foi pulada sem que o commit mudasse só documentação. Os testes do pipeline (`tests/FiscoProjects.Pipeline.Tests`) conferem essas regras no texto do workflow e o `changes.sh` com repositórios de exemplo.

## Prós e contras das opções

### A. Job único em sequência

- Bom, porque é o mais simples.
- Ruim, porque cada PR espera a soma de todas as partes, inclusive o archive.

### B. Jobs paralelos com cache

- Bom, porque o tempo é o da parte mais longa.
- Ruim, porque o archive ainda espera os testes de um código que já passou.

### C. B, pulando o código no commit que só muda documentação

- Bom, porque o archive termina em cerca de 1 minuto, sem aceitar código não testado.
- Ruim, porque depende da API do GitHub para achar o último commit verificado.

### D. Filtro de caminhos no gatilho

- Bom, porque não precisa de script.
- Ruim, porque o workflow nem roda no PR só de documentação, a verificação `verify` nunca aparece, e a regra "Protect main" bloqueia o merge.
