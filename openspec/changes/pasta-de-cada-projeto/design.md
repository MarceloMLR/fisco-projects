## Context

O `IFileStorage` (Files.Contracts) entrega pastas e arquivos por ids que os módulos guardam. Hoje, o `Documents` (pastas de documentos e pedidos de aceite) e o `Tickets` (anexos) montam o caminho `{código} - {nome}/...` a partir da raiz sempre que criam uma pasta, pelo `ProjectAccess` do `IProjectAccess`, e guardam só o id da pasta de baixo (`Folder.StorageFolderId`, `Ticket.AttachmentFolderId`). No SharePoint, o id de um item não muda quando ele é renomeado; na implementação local, o id é o caminho, e nada é renomeado. A raiz do armazenamento vem do Terraform (`FileStorage:SharePoint:RootFolder`, a própria biblioteca por padrão) e continua sendo o limite do que o aplicativo toca.

## Goals / Non-Goals

**Goals:**
- Um lugar só que sabe a pasta de cada projeto, achada pelo id, com o nome acompanhando o projeto.
- A pasta dos projetos escolhida pelo administrador, sem o Terraform.
- O mesmo comportamento nas duas implementações do armazenamento, conferido pelos testes do contrato.

**Non-Goals:**
- Escolher uma pasta existente (M365-3.2), mover pastas entre lugares, renomear as pastas de dentro do projeto.

## Decisions

- **O `Projects` guarda a pasta do projeto.** O projeto ganha `StorageFolderId` (até 512) e `StorageFolderName` (o `{código} - {nome}` com que a pasta foi criada ou renomeada pela última vez, sem o ajuste do armazenamento), fora da auditoria, como os ids de pasta dos outros módulos. Alternativas: o `Documents`, dono das pastas de documentos, faria o `Tickets` depender dele e precisaria saber quando o projeto muda de nome; o `Files` não tem tabelas (ADR-0079).
- **Contrato `IProjectStorage` (Projects.Contracts).** `EnsureFolderAsync(projectId, path)` devolve a pasta do caminho dentro da pasta do projeto, criando o que falta; `FolderOfAsync(projectId)` devolve a pasta do projeto sem criar, para a exclusão. Na primeira vez, a implementação (`ProjectStorage`, Application) lê a pasta dos projetos (`InstallationInfo.ProjectsFolder`); com ela preenchida, procura na raiz uma pasta `{código} - {nome}` criada antes desta change (`ListFoldersAsync`, sem diferenciar maiúsculas) e a adota; senão, cria `{pasta dos projetos}/{código} - {nome}`. O id é gravado só se o projeto ainda não tiver um (`ExecuteUpdate` condicional, fora da auditoria), para que duas primeiras vezes ao mesmo tempo fiquem com a mesma pasta. Um `StorageFolderNotFoundException` da pasta guardada (apagada por fora) limpa o id e cria de novo, uma vez.
- **Os módulos pedem as pastas ao contrato.** `Documents` (`Documentos/{pasta}` e `Pedidos de aceite`) e `Tickets` (`Chamados/#{número}`) trocam o caminho montado por `IProjectStorage.EnsureFolderAsync`, mantendo os ids das pastas de baixo que já guardam. O `ProjectFolders.PathOf` sai.
- **Renomear depois de gravar.** O `ChangeProject`, depois de salvar, compara `{código} - {nome}` com o `StorageFolderName`; se diferem e há pasta, chama `IFileStorage.RenameFolderAsync`. Deu certo: grava o nome. Nome em uso ou falha: um aviso no log com o id do projeto, e o projeto fica gravado; a próxima alteração compara de novo e tenta. A resposta da API não muda. Alternativa rejeitada: renomear dentro da transação, que deixaria o projeto preso ao SharePoint.
- **`IFileStorage.RenameFolderAsync(folderId, name)`** devolve a pasta com o nome ajustado e o mesmo id, `null` quando há um item com o nome ao lado, `StorageFolderNotFoundException` quando a pasta não existe ou está fora da raiz e `ArgumentException` para a raiz. No SharePoint, `PATCH items/{id}` com `name` e `@microsoft.graph.conflictBehavior` `fail` (409 = nome em uso), depois da conferência de que o item está dentro da raiz; o `FakeSharePoint` ganha o `PATCH`.
- **Ids estáveis na implementação local.** Itens novos ganham um id GUID v7, guardado num índice (`ids.json` na pasta interna, id → caminho, gravado inteiro por arquivo temporário e troca, sob o mesmo semáforo da escrita); uma pasta que já existe ganha o id na primeira vez que é entregue. Renomear move a pasta e as versões guardadas, atualiza o índice e registra a troca de caminho num diário, aplicado aos ids antigos (os caminhos entregues antes desta change), para que eles continuem valendo. Alternativa rejeitada: a local não renomear, que quebraria o contrato único (ADR-0079).
- **Remoção até a pasta do projeto.** `IProjectDeletion.DeleteFilesAsync` recebe a pasta do projeto (`string?`), e o `StoredFilesRemoval.RemoveAsync` ganha a pasta que fica (`keepFolderId`): a subida das pastas vazias para nela. Depois de todos os módulos, o `DeleteProject` apaga a pasta do projeto só se estiver vazia, sem subir; a pasta dos projetos nunca é tocada. Projetos sem pasta guardada continuam subindo até a raiz, como antes.
- **Pasta dos projetos.** `InstallationSettings.ProjectsFolder` (até 400, vazia por padrão), no `PUT /api/installation` e no `InstallationInfo`, conferida pela regra de nomes do armazenamento exposta no `Files.Contracts` (`StoragePaths`), a mesma que o armazenamento usa: partes não vazias, sem "." nem "..", sem os caracteres que o SharePoint recusa. Na tela, a seção Arquivos das Configurações gerais, com o campo de texto e a ajuda, e a pasta dos projetos no texto da auditoria.

## Risks / Trade-offs

- [A pasta do projeto no SharePoint pode estar travada (um arquivo aberto) na hora de renomear] → vale como falha: o projeto fica gravado e a próxima alteração tenta de novo.
- [Adotar pela raiz a pasta de mesmo nome que alguém criou à mão] → só acontece quando a pasta tem exatamente `{código} - {nome}`, e o código é único; é a pasta do projeto.
- [O índice da implementação local fica fora de sincronia se alguém mexer em `App_Data/files` à mão] → um id cujo caminho sumiu vale como inexistente, como antes; é só desenvolvimento.
- [Projeto já dividido em duas pastas antes desta change] → continua dividido; juntar fica fora (Non-goals). No `dev` do Azure, os dados são recarregados depois.

## Migration Plan

- Migrações: `projects.projects` ganha as duas colunas, nulas; `administration.installation_settings` ganha `projects_folder`, vazia. Nenhum dado é convertido: cada projeto ganha a pasta no próximo arquivo.
- Voltar a versão anterior deixa as colunas sem uso; os arquivos guardados dentro da pasta do projeto continuam abrindo pelos ids que os módulos guardam.
