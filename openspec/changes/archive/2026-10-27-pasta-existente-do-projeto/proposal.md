## Why

Na M365-3.1, cada projeto ganhou uma pasta só no armazenamento, sempre criada pelo sistema como `{código} - {nome}` dentro da pasta dos projetos. O ADR-0030 prevê também ligar o projeto a uma pasta que a empresa já tem no SharePoint, e Marcelo escolheu que isso seja possível ao criar e ao editar o projeto (M365-3.2 do roadmap).

## What Changes

- ADR-0122: escolher uma pasta que já existe para o projeto, quais pastas podem ser escolhidas, trocar só enquanto o projeto não guardou arquivos, e a permissão nova "escolher a pasta" (administrador e gerente), que completa a matriz do ADR-0043.
- `Projects`: o projeto guarda se a pasta foi escolhida (`StorageFolderChosen`); a pasta escolhida não é renomeada com o projeto nem apagada na exclusão. A troca da pasta e a pasta escolhida na criação entram na auditoria, pelo nome da pasta.
- API: `GET /api/project-folders` lista as pastas de dentro de uma pasta do armazenamento, com o projeto que usa cada uma e se ela pode ser escolhida; `POST /api/projects` aceita `folderId`; `GET` e `PUT /api/projects/{id}/folder` dizem e trocam a pasta, com 409 quando o projeto já guardou arquivos.
- Não podem ser escolhidas: a raiz, a pasta dos projetos e as pastas acima dela, a pasta do sistema (os logos), a pasta de outro projeto, as pastas dentro dela e as pastas acima dela.
- Contrato novo `IProjectFolderUse` do `Projects.Contracts`, que o `Documents` e o `Tickets` implementam: se o módulo guardou arquivos do projeto e, depois de uma troca, as pastas vazias que o módulo criou saem da pasta anterior e são criadas na nova.
- Telas: o campo "Pasta dos arquivos" no Novo projeto e no Editar dados (este só para o administrador e o gerente), e o diálogo "Escolher pasta", que navega pelas pastas do armazenamento.

## Capabilities

### New Capabilities

### Modified Capabilities
- `projetos`: escolher e trocar a pasta do projeto, a lista de pastas para escolher, a pasta escolhida que não é renomeada nem apagada, e as telas.

## Non-goals

- Mover os arquivos para outra pasta depois que o projeto já guardou algum.
- Criar uma pasta nova pelo diálogo, renomear ou excluir pastas do SharePoint pela tela.
- Mostrar no FiscoProjects os arquivos que já estavam na pasta escolhida (ADR-0030).
- Escolher a pasta em outra biblioteca ou site.

## Impact

- `Projects` (domínio, migração, casos de uso, endpoints, permissões), `Documents` e `Tickets` (`IProjectFolderUse`), `Files.Contracts` (nada novo além do uso do contrato).
- Telas: `NewProjectPage`, `ProjectDialog`, o campo e o diálogo novos em `web/src/projects`, textos e tipos da API.
- `docs/adr`, `docs/glossario.md`, `CLAUDE.md` e `docs/roadmap.md`.
- ADRs: 0030, 0041, 0043, 0121.
