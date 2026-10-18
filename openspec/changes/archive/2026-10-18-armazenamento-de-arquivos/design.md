# Design: armazenamento de arquivos (Acesso-4)

## Context

Motivação em proposal.md e decisão de arquitetura no ADR-0079. Hoje nenhum módulo guarda arquivos. A estrutura de módulos (ADR-0047) já tem `Users` e `Audit`, e os testes de arquitetura encontram módulos novos pelo nome do assembly. A configuração por instalação segue o padrão de opções validadas ao subir, como a entrada local (`LocalSignInValidation`, que recusa produção). O SharePoint, pelo Microsoft Graph, só entra quando houver tenant.

## Goals / Non-Goals

**Goals:**

- Um contrato que a implementação do SharePoint possa cumprir sem mudar quem o usa: ids opacos, versões e pastas como no SharePoint.
- Testes de contrato escritos uma vez, que a implementação do SharePoint vai reaproveitar.
- Nenhuma escrita ou leitura fora da raiz configurada.

**Non-Goals:**

- Desempenho para arquivos muito grandes (envio em partes, retomada): o limite de cada uso é pequeno (15 MB nos anexos) e o envio em partes do Graph entra com a implementação do SharePoint.

## Decisions

### 1. Módulo `Files` sem tabelas e sem rotas

- `FiscoProjects.Files.Contracts`: `IFileStorage`, os dados devolvidos (`StoredFolder`, `StoredFile`, `FileDownload`), `SameNameAction` e a exceção de pasta não encontrada.
- `FiscoProjects.Files`:
  - `Application`: as regras de nomes e caminhos (`FileNames`), sem tecnologia, usadas por toda implementação;
  - `Infrastructure`: a implementação local, as opções e o registro (`AddFilesModule`).
- Sem `Endpoints`: o envio e o download pela API chegam com cada módulo, com as permissões dele.

### 2. Contrato

```csharp
Task<StoredFolder> EnsureFolderAsync(string? parentFolderId, string path, CancellationToken ct);
Task<IReadOnlyList<StoredFolder>> ListFoldersAsync(string? parentFolderId, CancellationToken ct);
Task<StoredFile> UploadAsync(string folderId, string fileName, Stream content, SameNameAction sameName, CancellationToken ct);
Task<FileDownload?> OpenAsync(string fileId, string? versionId, CancellationToken ct);
Task<bool> DeleteFileAsync(string fileId, CancellationToken ct);
Task<bool> DeleteFolderIfEmptyAsync(string folderId, CancellationToken ct);
```

- `parentFolderId` nulo é a raiz configurada. `path` tem uma ou mais pastas separadas por `/`.
- Não encontrado: `OpenAsync` devolve nulo, e as exclusões devolvem falso. Enviar para uma pasta que não existe lança `StorageFolderNotFoundException`.
- Caminho ou id que sairia da raiz lança `ArgumentException`.
- `FileDownload` traz o conteúdo como `Stream` e é descartável.
- `SameNameAction`: `NewVersion` (o arquivo mantém o id, a versão sobe e a anterior fica guardada) ou `KeepBoth` (o novo ganha ` (1)`, ` (2)`... antes da extensão).
- Alternativa: endereçar tudo por caminho, sem ids. Descartada porque, no SharePoint, alguém pode renomear a pasta do projeto, e os ids continuam valendo (ADR-0030).
- Alternativa: incluir uma opção de recusar o nome repetido. Descartada por enquanto: nenhum uso previsto precisa dela.

### 3. Nomes e caminhos (`FileNames`)

- Caracteres que o SharePoint não aceita (`" * : < > ? / \ |`) e os de controle viram `_`. Espaços nas pontas e pontos no fim saem.
- Nomes reservados do Windows e do SharePoint (`CON`, `PRN`, `AUX`, `NUL`, `COM1` a `COM9`, `LPT1` a `LPT9`, `.lock`, `desktop.ini`, `_vti_`) e o nome da pasta interna do armazenamento ganham `_` na frente.
- Nome vazio depois do ajuste vira `file`. O nome é cortado em 200 caracteres, mantendo a extensão.
- Num caminho, `.` e `..` são recusados, não ajustados: quem pede um caminho assim está errado, e ajustar esconderia o erro.

### 4. Implementação local

- A raiz é `FileStorage:Local:RootPath`, relativa à pasta da aplicação. No desenvolvimento é `App_Data/files`, fora do Git.
- O id de pasta ou de arquivo é o caminho relativo à raiz, com `/`. É opaco para quem usa. Antes de qualquer acesso, o caminho completo é resolvido e conferido dentro da raiz.
- As versões anteriores ficam na pasta interna `.fiscoprojects/versions/<caminho do arquivo>/<número>`. A versão atual é o número de versões anteriores mais um ("1", "2"...). Excluir o arquivo exclui essa pasta.
- A pasta interna `.fiscoprojects` (versões e arquivos temporários) não aparece na lista de subpastas.
- O envio grava num arquivo temporário e move para o lugar só no fim, então um envio interrompido nunca deixa um arquivo pela metade. Em "manter os dois", se outro envio tomar o nome no meio do caminho, tenta o número seguinte.
- O tipo do conteúdo vem da extensão (`FileExtensionContentTypeProvider` do ASP.NET Core); se for desconhecido, `application/octet-stream`.

### 5. Configuração

- `FileStorage:Provider`: `Local`, por enquanto o único. O SharePoint entra no grupo M365. O desenvolvimento configura `Local` no `appsettings.Development.json`.
- Sem provedor configurado, a aplicação sobe, e pedir o `IFileStorage` falha com uma mensagem que aponta `FileStorage:Provider`. É o caso da produção até a implementação do SharePoint, e é o que permite subir a aplicação em produção nos testes.
- Validação ao subir:
  - provedor conhecido;
  - pasta raiz informada para o local;
  - local recusado em produção, como a entrada local (ADR-0078).
- Alternativa: `Local` como padrão, recusado em produção. Descartada porque, sem outra implementação, a aplicação não subiria em produção por nenhum motivo.
- Os arquivos não entram na auditoria: o registro de cada arquivo fica no banco do módulo que o usa e é auditado lá.

### 6. Testes

- `FileStorageContract`: uma classe de testes abstrata com os cenários da spec, que recebe a implementação. `LocalFileStorageTests` a usa numa pasta temporária de cada teste. A implementação do SharePoint vai herdar a mesma classe.
- `FileNames`: unitários para caracteres, nomes reservados, corte e caminhos recusados.
- Host: a aplicação não sobe em produção com o armazenamento local, e o registro do módulo resolve `IFileStorage`.

## Risks / Trade-offs

- [A implementação local diverge do SharePoint em detalhes, como o formato do id da versão (no SharePoint, "1.0")] → Os ids são opacos, e os testes de contrato valem para as duas.
- [Pasta local cresce sem limite no desenvolvimento] → Fica em `App_Data/files`, fora do Git; apagar a pasta zera os arquivos de demonstração.
- [Arquivos guardados no disco do App Service se alguém ligasse o local fora do desenvolvimento] → A aplicação não sobe em produção com ele.

## Migration Plan

Sem banco. Para desfazer, basta reverter o PR.
