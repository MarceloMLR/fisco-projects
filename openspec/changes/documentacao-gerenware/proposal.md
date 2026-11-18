## Why

O produto, o código e a infraestrutura já se chamam GerenWare (Marca-1 a Marca-7), mas as documentações vivas ainda dizem FiscoProjects no texto. Esta change fecha o rebranding nos documentos que as pessoas e o agente de código leem hoje.

## What Changes

- `README.md` com a logo, o nome e o link para `docs/marca/`.
- `CLAUDE.md` com o nome novo, a regra da marca (o nome, a logo dos arquivos, o histórico com o nome antigo) e o manual de identidade visual entre as fontes de verdade.
- `openspec/config.yaml` com o nome novo e a marca nas fontes de verdade.
- `docs/discovery.md`, `docs/instalacao.md`, `docs/demonstracao.md`, `docs/restauracao.md`, `docs/glossario.md` e `docs/custos-estimados.md` com o nome novo; o glossário ganha "Marca do produto".
- `docs/apresentacao.html` com a logo e as cores da marca.
- `docs/design-justificativa.md` ganha só uma nota no topo: é um documento datado, que fica como foi escrito.

## Capabilities

### New Capabilities

### Modified Capabilities

Nenhuma: só documentação.

## Non-goals

- Mudar ADRs aceitos, changes arquivadas, os registros antigos do roadmap ou as linhas "Definições sem ADR" do discovery, que contam a história com o nome antigo.
- Os dois requisitos de spec que ainda citam o nome antigo: as changes Marca-2 e Marca-4, ainda não arquivadas, os substituem no archive.
- Os documentos do projeto no Claude (cópias dos ADRs), que não fazem parte do repositório.

## Impact

- Só documentação: `README.md`, `CLAUDE.md`, `openspec/config.yaml` e `docs/`. Pela verificação do PR, só as specs rodam.
