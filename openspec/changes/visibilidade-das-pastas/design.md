## Context

As pastas padrão (`DefaultFolder`, auditadas) e as pastas de cada projeto (`DocumentFolder`, cópias feitas na criação, fora da auditoria) ficam no módulo `Documents`. Toda leitura e escrita de documentos passa pelo `DocumentAreas.OfAsync`, que monta a área da pessoa: o projeto, as permissões do `DocumentAccess` e as pastas que ela vê. Hoje só a pasta de termos é tirada, para quem não é da gestão (`SeesAcceptanceFolder`). A lista, a busca, o detalhe, o arquivo, o envio, o status, o vínculo e a exclusão procuram a pasta do documento na área (`FolderOf`), e a contagem da aba soma só as pastas da área.

## Goals / Non-Goals

**Goals:**

- Quem vê cada pasta por papel, nas pastas padrão e em cada projeto, aplicado no ponto único da área.
- A mudança no projeto só pelo administrador, com registro na auditoria.

**Non-Goals:**

- Pessoas específicas, só leitura por pasta, o gerente ajustando, as permissões do SharePoint.

## Decisions

### Domínio

- **Papéis da pasta:** `FolderRole` (`TechLead`, `Contributor`, `Developer`, `Viewer`), o que a API recebe e devolve. Guardado como `FolderViewers` (`[Flags]`, `None` a `Everyone`).
- **Regra num lugar só:** `FolderVisibility`.
  - `Of(roles)` e `RolesOf(viewers)`, na ordem dos papéis;
  - `Sees(viewers, role)`: o papel do projeto está no conjunto.
- **Pastas:**
  - `DefaultFolder` e `DocumentFolder` ganham `VisibleTo`;
  - a pasta de termos é sempre `None`;
  - `DefaultFolder.ChangeVisibility` e `DocumentFolder.ChangeVisibility` devolvem falso na pasta de termos;
  - o `DocumentFolder.For` copia o `VisibleTo` da padrão.
- **Acesso:**
  - `DocumentAccess.SeesFolder(viewer, project, folder)`: o administrador e o gerente sempre; a pasta de termos nunca para os outros; os outros pelo `FolderVisibility.Sees`;
  - `DocumentAccess.MayChangeFolderVisibility(viewer)`: só o administrador;
  - o `SeesAcceptanceFolder` sai, porque o `SeesFolder` o cobre.

### Casos de uso

- **Área:** o `DocumentAreas.OfAsync` filtra as pastas pelo `SeesFolder`. Toda ação que já procura a pasta na área passa a recusar a pasta escondida com 404, sem mudança nelas.
- **Pastas padrão:** o `AddDefaultFolder` recebe os papéis (sem eles, os quatro). O `ChangeDefaultFolder` recebe os papéis opcionais (sem eles, mantém). Os papéis na pasta de termos dão `FolderOutcome.AcceptanceVisibilityFixed` (400).
- **`ChangeFolderVisibility(projectId, folderId, roles, viewer)`:**
  - 404 sem o projeto ou sem a pasta;
  - 403 de quem não é administrador;
  - 400 na pasta de termos;
  - 409 com o projeto encerrado;
  - grava pelo `IFolderStore.SaveVisibilityAsync(folder, antes)`.
- **Auditoria da pasta do projeto:** a store salva e grava, na mesma transação, um registro pelo `IChangeAuditor`, como a exclusão do projeto:
  - tipo `DocumentFolder`, ação alterada, rótulo com o nome da pasta;
  - o campo `VisibleTo` antes e depois;
  - o `ProjectId` como contexto e referência ao projeto.
  - A pasta continua fora da auditoria automática, para a criação não gerar registros.

### API

- **Pastas padrão:** `DefaultFolderResponse` ganha `visibleTo`. `NewDefaultFolderRequest` e `DefaultFolderRequest` ganham `visibleTo` opcional.
- **Pastas do projeto:** `DocumentFolderResponse` ganha `visibleTo`, e `DocumentFoldersResponse` ganha `canChangeVisibility` (administrador, projeto em aberto).
- **Rota nova:** `PUT /api/projects/{projectId}/documents/folders/{folderId}/visibility` (`ChangeFolderVisibility`), com `{ visibleTo }`. Responde 204, 400 em `visibleTo`, 403, 404 ou 409 `project-closed`.

### Banco

- **Migração `FolderVisibility`:** a coluna `VisibleTo` (int) em `default_folders` e `folders`, com 15 nas linhas que existem e 0 nas de termos.
- **Modelo sem valor padrão:** o 0 é um valor válido, e o EF trocaria o 0 pelo padrão do banco. O `HasData` das pastas do MVP leva 15 ou 0.

### Tela

- **Pastas padrão:**
  - o `DefaultFolderDialog` ganha "Quem vê", com uma caixa por papel (`Checkbox`) e a nota de que o administrador e o gerente sempre veem; na pasta de termos, o texto fixo;
  - a `DefaultFoldersPage` ganha a coluna "Quem vê" (`visibilityText`: "Todos", "Só a gestão" ou a lista dos papéis).
- **Aba Documentos:**
  - com `canChangeVisibility`, a pasta escolhida (não "Todos" nem a de termos) mostra "Quem vê esta pasta", que abre o `FolderVisibilityDialog`;
  - as pastas que algum papel não vê ganham, para a gestão, o ícone de cadeado com "Restrita: {quem vê}" no nome acessível;
  - as regras de tela ficam em `documents/folderVisibility.ts`.
- **Auditoria:** a tela descreve "Pasta do projeto" e o campo "Quem vê" pelos papéis.

### Demonstração

Depois de enviar os documentos, o plano deixa "01 Gestão do projeto" do ERP-FIN só para o líder técnico. Os envios do plano nessa pasta são da gestão.

## Risks / Trade-offs

- **[Uma pasta escondida com documentos de quem a enviava]:** a pessoa deixa de vê-los, como pede a regra. O administrador e o gerente continuam vendo tudo.
- **[O 0 confundido com o valor padrão do EF]:** o modelo não tem valor padrão, e um teste confere a pasta "só da gestão" gravada com 0.
- **[A contagem da aba muda de pessoa para pessoa]:** a contagem já vinha da área de cada pessoa.
