# Design: verificação mais rápida (Fundação-3.2)

## Context

Numa execução recente, a verificação levou 5,5 minutos, nesta ordem e em sequência:

| Passo | Tempo |
| --- | --- |
| build | 38 s |
| testes | 2 min 34 s (o maior é o projeto da API, com 2 min 28 s; os projetos de teste já rodam em paralelo entre si) |
| formatação | 25 s |
| frontend | 1 min 43 s (o `npm ci` leva 10 s) |
| specs | 1 s |

O repositório é público: os minutos do GitHub Actions não são cobrados, e os jobs rodam em paralelo.

## Goals / Non-Goals

**Goals:**

- Uma verificação completa perto do tempo do backend, cerca de 3,5 minutos.
- O commit que só muda documentação, como o do archive, termina em cerca de 1 minuto.
- Sem aceitar código que não passou por build, testes e formatação.

**Non-Goals:**

- Acelerar os testes da API ou dividi-los em jobs; pular só o backend ou só o frontend (proposta).

## Decisions

### 1. Seis jobs, com o `verify` no fim

```
changes ─┬─ backend  (build → testes)  ─┐
         ├─ format                      ├─ verify
         └─ frontend                    │
specs ──────────────────────────────────┘
```

- `backend`, `format` e `frontend` dependem do `changes` e só rodam quando ele diz que o código mudou (`code == 'true'`).
- `specs` não depende de nada e roda sempre.
- `verify` depende de todos, roda sempre (`!cancelled()`) e chama `scripts/ci/verdict.sh` com o resultado de cada um. O nome `verify` continua, porque a regra "Protect main" exige essa verificação.
- O `verdict.sh` reprova quando:
  - o `changes` ou as specs não passaram;
  - com código mudado, alguma parte não passou;
  - sem código mudado, alguma parte falhou ou foi cancelada.
- No `backend`, os testes só rodam depois de um build aprovado, como hoje. A formatação vira job próprio porque não depende do build e tira 25 a 40 s do caminho mais longo.

### 2. Como o `changes.sh` decide

1. **Base da comparação:**
   - a variável `CHANGES_BASE`, se existir, para os testes e para rodar à mão;
   - no GitHub, o commit mais recente do PR, fora o atual, que tem a verificação `verify` aprovada. O script lê os commits do PR e as verificações de cada um pelo `gh api`, com o token de leitura do job;
   - sem nenhum desses, o ponto em que o branch saiu da `main` (`git merge-base origin/main HEAD`).
2. **Diferença:** `git diff --name-only <base> HEAD`. No GitHub, o `HEAD` é o commit que a verificação testa, a junção do PR com a `main` atual. Se a `main` ganhou código depois da última verificação aprovada, esse código aparece na diferença e tudo roda: a comparação erra para o lado seguro.
3. **Documentação:**
   - caminhos que começam com `docs/`, `openspec/` ou `.claude/`;
   - arquivos `.md` fora de `web/`.

   Qualquer outro arquivo é código, inclusive o workflow e os scripts de `scripts/ci`.
4. **Saída:** `code=true` ou `code=false`, no terminal e, no GitHub, no `GITHUB_OUTPUT`. Também os arquivos que decidiram, para quem lê o log.
5. **Base sumida:** se o commit da base não está no histórico, `code=true`.

- **Alternativa:** comparar sempre com a `main`. Recusada: o PR do archive sempre tem código, porque leva a implementação junto, e nada seria pulado.
- **Alternativa:** comparar com o commit anterior (`before` do evento). Recusada: se o anterior falhou nos testes, um commit só de documentação aprovaria o código que falhou. Por isso a base é o último commit **aprovado**.

### 3. Permissões e token

O bloco único de permissões do workflow passa a ser `contents: read`, `checks: read` e `pull-requests: read`. O `changes` recebe o token do job (`github.token`, não um segredo) só como `GH_TOKEN`, para o `gh api`. Os checkouts continuam com `persist-credentials: false`. O `changes` usa `fetch-depth: 0`, para ter os commits do PR e a `main`.

### 4. Cache

- **NuGet:** `actions/cache` v6.1.0, fixado pelo commit, em `~/.nuget/packages`, com a chave do sistema e do hash dos `*.csproj`, `Directory.*.props` e `global.json`. Ele fica no `backend` e no `format`.
- **npm:** o próprio `setup-node`, com `cache: npm` e o `package-lock.json` de `web/`, no `frontend`, e o de `scripts/ci/`, nas `specs`.

O cache só evita baixar os pacotes de novo: o `npm ci` e o `dotnet restore` continuam conferindo tudo.

### 5. Testes do pipeline

`tests/FiscoProjects.Pipeline.Tests` continua lendo o workflow como texto, agora com os jobs, as dependências, as permissões e os caches. Os dois scripts novos são testados de verdade:

- o `changes.sh`, com repositórios Git criados no teste e um `gh` falso no `PATH`, que responde pelos commits e pelas verificações;
- o `verdict.sh`, com as combinações de resultado.

Esses testes chamam o `bash` e o `git`; no Windows, onde o `bash` do sistema pode ser o do WSL, eles são pulados com o motivo, e rodam no CI e no Linux.

## Risks / Trade-offs

- **[Lista de documentação errada deixa passar código]** → A lista é pequena e fechada. Um `.md` dentro de `src/` ou `tests/` não entra no build nem é lido pelos testes (conferido: nenhum teste lê arquivos `.md`, e o `.md` de `web/` conta como código).
- **[API do GitHub fora do ar]** → Sem resposta, o script usa a `main` como base, e tudo roda.
- **[Job pulado conta como aprovado]** → O `verdict.sh` só aceita pulado quando o `changes` disse que não há código, e o próprio `changes` precisa ter passado.
