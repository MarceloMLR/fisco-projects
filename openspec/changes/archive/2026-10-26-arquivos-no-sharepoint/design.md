## Context

O `IFileStorage` (Files.Contracts) tem a implementação local (`LocalFileStorage`), conferida pelos testes do contrato (`FileStorageContract`). O aplicativo do Entra prova quem é pela identidade gerenciada, e o `Users` já pede tokens do Graph (`IGraphTokens`, M365-2). O site "Projetos" existe no tenant de Marcelo, com o Microsoft 365 Business Basic em avaliação.

## Goals / Non-Goals

**Goals:**
- Guardar, abrir, versionar e excluir os arquivos de todos os módulos no SharePoint, sem mudar o contrato.

**Non-Goals:**
- Ligar um projeto a uma pasta que já existe no SharePoint (fica para outra change), importar arquivos que já estavam lá, gerenciar permissões.

## Decisions

- **Token pelo contrato:** `IGraphTokens` e `MicrosoftGraph.Address` vão para o `Users.Contracts`; o `ClientAssertionGraphTokens` explica que falta a entrada pelo Entra quando ela está desligada.
- **Graph:** `SharePointFileStorage` (Files/Infrastructure), pelo `HttpClient` nomeado `sharepoint`, singleton. A raiz (o site por `sites/{host}:{path}`, a biblioteca por nome em `sites/{id}/drives` ou a padrão em `sites/{id}/drive`, e a pasta raiz criada por `POST .../children` com `fail`) é achada uma vez e guardada.
- **Pastas:** cada parte do caminho é procurada por `items/{pai}:/{nome}` e criada quando falta; um `409` quer dizer que outro a criou, e ela é lida de novo. A lista segue as páginas (`@odata.nextLink`) e esconde a pasta interna na raiz, como a local.
- **Envio:** `PUT items/{pasta}:/{nome}:/content` com `replace` (nova versão) ou `fail` (manter os dois, tentando " (1)", " (2)"...). O conteúdo é lido para a memória, para poder ser enviado de novo. A versão é a primeira da lista de versões (a mais nova).
- **Abrir:** a versão atual por `items/{id}/content` e uma anterior por `versions/{id}/content`; o `HttpClient` segue o endereço de download sem o token. O tipo sai do nome (`ContentTypes`, extraído da local).
- **Dentro da raiz:** antes de abrir, excluir ou dizer a pasta de um item, o `parentReference.path` precisa estar dentro da pasta raiz, e a biblioteca, ser a mesma; senão, o item conta como inexistente. Um id que não é do Graph (400) também.
- **Repetir:** 429, 503 e 504 são repetidos até 4 vezes, depois do `Retry-After` (no máximo 30 s) ou de 2, 4, 6 s.
- **Configuração e Terraform:** `FileStorage:SharePoint:SiteUrl`, `Library` e `RootFolder` (vazia por padrão: a própria biblioteca, num site só do sistema); a validação exige o endereço https. O Terraform liga o armazenamento com `sharepoint_site_url` (no `local.auto.tfvars`), `sharepoint_library` e `sharepoint_root_folder`, e dá `Sites.Selected` com consentimento. O `plan.sh` lê o site do App Service e o mascara.
- **Testes:** `FakeSharePoint` (`FiscoProjects.Testing`) simula o site, as bibliotecas, as pastas, as versões, os `eTag`, o `409`, as páginas e o `429`; `SharePointFileStorageTests` herda o `FileStorageContract`.

## Risks / Trade-offs

- [O `dev` tem documentos, anexos e logos na pasta local, com ids que o SharePoint não conhece] → depois do `apply`, os dados de demonstração do `dev` são carregados de novo (com a aprovação de Marcelo), e os arquivos antigos deixam de abrir até lá.
- [Dar o site depende do Microsoft Graph PowerShell na máquina de quem administra] → o guia de instalação traz o comando de instalação, e o script confere os módulos antes de começar.
