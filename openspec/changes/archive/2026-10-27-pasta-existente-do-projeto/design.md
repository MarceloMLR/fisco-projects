## Context

Desde a M365-3.1, o `Projects` guarda a pasta de cada projeto (`StorageFolderId`, `StorageFolderName`), criada com o projeto pelo `IProjectStorage` quando o `Documents` monta as pastas de documentos (`IProjectSetup`), e os módulos guardam só os ids das pastas de baixo. O `IFileStorage` não diz o nome nem o caminho de um id, mas lista as pastas de uma pasta (`ListFoldersAsync`, que falha para um id que não é pasta ou está fora da raiz) e diz a pasta de cima (`FolderOfAsync`, nula na raiz).

## Goals / Non-Goals

**Goals:**
- Escolher a pasta na criação e trocá-la enquanto não há arquivos, sem dividir o projeto.
- As mesmas regras de escolha na lista, na criação e na troca, num lugar só.

**Non-Goals:**
- Mover arquivos, criar pastas pelo diálogo, olhar outras bibliotecas.

## Decisions

- **O projeto marca a pasta escolhida.** `Project.StorageFolderChosen`, fora da auditoria; `ChooseFolder(id, nome)` e `UseSystemFolder()` no domínio, gravados com o projeto. O `StorageFolderName` passa a entrar na auditoria ("Pasta dos arquivos"): a escolha na criação e a troca aparecem pelo nome; a criação e o renome feitos pelo sistema continuam fora (`ExecuteUpdate` do `ProjectFolderStore`).
- **Regras num lugar só (`ProjectFolderChoices`, Application).** Para uma pasta, sobe pelo `FolderOfAsync` até a raiz e recusa, nesta ordem: a pasta de outro projeto (`InUse`, com o código), a pasta dentro da de um projeto (`InsideProject`), a pasta dos projetos (`ProjectsFolder`), as pastas acima dela ou acima da pasta de um projeto (`AboveProjects`) e a pasta do sistema (`SystemFolder`, a `FiscoProjects` da raiz, dos logos). As pastas dos projetos vêm da tabela; as de cima, da subida de cada uma, uma vez por pedido. A pasta do próprio projeto que está sendo editado conta como dele.
- **Existência e nome.** Uma pasta escolhida existe quando `ListFoldersAsync` dela não falha, e o nome sai da lista da pasta de cima; assim, um id de arquivo, de fora da raiz ou apagado vira 400 no campo `folderId`.
- **Lista (`GET /api/project-folders?parentId=&projectId=`, `ListProjectFolders`).** Só `SystemPolicies.ProjectCreators`. Devolve a pasta aberta (nula na raiz, que nunca é escolhida) e as de dentro, em ordem alfabética, cada uma com `project` (o código de quem usa) e `refusal`. O `projectId` diz o projeto editado.
- **Criação.** `NewProjectRequest.FolderId`; o `CreateProject` confere a escolha antes de gravar e chama `ChooseFolder`, e o `Documents` cria as pastas de documentos dentro dela pelo `IProjectStorage`, como antes.
- **Troca (`PUT /api/projects/{id}/folder`, `ChangeProjectFolder`).** `ProjectPermissions.CanChooseFolder` (administrador e gerente); projeto encerrado, 409 `project-closed`; algum arquivo, 409 `project-has-files`, pela pergunta `IProjectFolderUse.HasFilesAsync` a cada módulo; a mesma pasta, nada muda. Grava o projeto, e só então chama `IProjectFolderUse.FolderChangedAsync(projeto, pasta anterior)`: o `Documents` apaga as pastas vazias que registrou, subindo até a pasta anterior (`StoredFilesRemoval` com a pasta que fica), esquece os ids e as cria na pasta nova; o `Tickets` não tem o que mover sem anexos. Por último, a pasta anterior criada pelo sistema sai, se ficou vazia. `GET /api/projects/{id}/folder` diz o nome, se foi escolhida, se tem arquivos e se a pessoa pode trocar.
- **Renomear e excluir.** O `ProjectFolderRenaming` não renomeia a pasta escolhida; o `DeleteProject` não apaga a pasta escolhida. Uma pasta escolhida apagada por fora volta a ser a do sistema no próximo arquivo (`ForgetAsync` limpa a marca).
- **Telas.** `ProjectFolderField` (o texto da pasta, "Escolher pasta…" e "Usar a pasta criada pelo sistema") no `NewProjectPage` e no `ProjectDialog`; o `ProjectFolderDialog` (`Dialog` do design system) navega pela `projectFoldersQuery`, com o caminho em botões, as pastas como linhas que abrem e "Usar esta pasta" para a pasta aberta, com o motivo quando não pode. O `ProjectDialog` grava os dados e, se a pasta mudou, a troca.

## Risks / Trade-offs

- [Subir até a raiz custa uma chamada ao Graph por nível, para a pasta aberta e para cada pasta de projeto] → uma vez por pedido da lista ou da escolha, e as pastas de projeto ficam a poucos níveis da raiz.
- [Alguém cria um arquivo no meio da troca] → o arquivo fica na pasta em que foi criado; a janela é curta e a troca é rara, antes do primeiro arquivo.
- [Uma pasta escolhida que já tinha pastas "Documentos"] → o sistema usa a que existe; nada é apagado da pasta escolhida além das pastas vazias que o `Documents` registrou.

## Migration Plan

- Migração do `Projects`: `StorageFolderChosen`, falso para todos. Nada a converter.
