## Why

As telas já dizem GerenWare, mas o código ainda se chama FiscoProjects: a solução, os projetos, os namespaces, os tipos de erro, o cookie, os medidores, a chave da conexão e o nome que entra na cifra das senhas dos acessos. Esta change troca os nomes do código, como o ADR-0161 decidiu.

## What Changes

- `FiscoProjects.slnx`, as pastas e os projetos de `src/` e `tests/`, os namespaces e os nomes de tipo passam a `GerenWare`. As migrações acompanham nos namespaces e nos instantâneos; os identificadores delas e os dados que gravam não mudam.
- **BREAKING** Tipos de erro da API: `urn:gerenware:problem:…`. O contrato (`openapi.json`) e os tipos do frontend foram gerados de novo.
- **BREAKING** Cookie da sessão `__Host-gerenware-session`: quem está com sessão aberta entra de novo.
- **BREAKING** Chave da conexão `ConnectionStrings:GerenWare` e banco local `GerenWare`.
- **BREAKING** Proteção das senhas dos acessos com o nome `GerenWare.Credentials`: as senhas gravadas antes não abrem e são gravadas de novo (só há dados de demonstração).
- Medidores `GerenWare.*` e métricas `gerenware.emails.*`, com os alertas do Terraform que as leem.
- Pasta do sistema no armazenamento `GerenWare` e pasta interna do armazenamento local `.gerenware`.
- Pacotes `gerenware-web` e `gerenware-ci-tools`, projeto do Docker `gerenware`, scripts de `scripts/`, workflows e Codespace com os caminhos novos.
- Terraform e scripts de `infra/`: só o que nomeia código ou o produto em texto (a chave da conexão, as métricas, a DLL da API, os nomes de exibição e a marca `aplicacao`). Os nomes dos recursos ficam para a Marca-6.
- Nomes de código nas specs, no `CLAUDE.md`, no `openspec/config.yaml` e nos guias, para os comandos continuarem certos.

## Capabilities

### New Capabilities

### Modified Capabilities

Nenhuma regra muda. Os nomes de código citados nas specs (a convenção `GerenWare.<Modulo>` da arquitetura e os medidores) foram trocados direto no texto.

## Non-goals

- Nomes dos recursos do Azure, do estado do Terraform e do pacote da versão (`fiscoprojects-…`): Marca-6.
- Nome do repositório e o que depende dele: Marca-7.
- O nome do produto no texto corrido das documentações: Marca-8.
- ADRs aceitos, changes arquivadas e registros antigos do roadmap.

## Impact

- Quase todo arquivo de `src/` e `tests/` muda de caminho ou de conteúdo; o Git mostra a maior parte como renomeação.
- Depois do merge, o `dev` precisa do Terraform aplicado (a chave da conexão mudou) e fica fora do ar até lá; a Marca-6 recria o ambiente.
- Quem desenvolve: `dotnet user-secrets` com a chave nova, banco local novo (`docker compose down -v` ou apontar a conexão para o banco antigo) e demonstração carregada de novo.
