# Design: documentos (Documentos-1)

## Context

- **ADR-0030:** os arquivos ficam no SharePoint, na pasta de cada projeto, com a estrutura padrão criada pelo sistema; o status e o vínculo ficam no banco do FiscoProjects.
- **ADR-0031:** pastas padrão configuráveis, versões, status manual, vínculo com o cronograma, busca por nome, visualização no navegador e exclusão só pelo gestor.
- **ADR-0045:** a matriz de permissões; a pasta de termos só para o gerente e o administrador.
- **ADR-0092:** o módulo `Documents`, com os contratos do `Projects`.
- **ADR-0093:** a nova versão de um aprovado volta para Em revisão; a pasta de termos só recebe os termos assinados.
- **O que já existe:**
  - o `IFileStorage`, com `NewVersion` e a abertura de uma versão pelo id (ADR-0079);
  - o `IProjectAccess`, com o papel de cada pessoa e o projeto encerrado;
  - os anexos dos chamados, com as regras de tipo e tamanho;
  - a coluna de navegação da página Chamados, o `DataTable`, o `DetailsDrawer` e a tela "Fornecedores".
- **Canvas ProjetoDocumentos:**
  - as pastas numa coluna, com a contagem;
  - o título da pasta com a contagem e "Enviar arquivos";
  - a busca "Buscar nos documentos do projeto" e o filtro "Todos, menos obsoletos" / "Todos";
  - a tabela com Nome (ícone, nome e "Versão 3 · Parametrizar regras de ICMS"), Status, Atualizado (data e pessoa) e Tamanho;
  - o painel do documento escolhido, com "Abrir", "Baixar", "⋮", as propriedades e as versões;
  - o painel vira a gaveta do ADR-0081, como na Auditoria.

## Goals / Non-Goals

**Goals:**
- as pastas padrão e as do projeto;
- documentos com versões, status e vínculo;
- busca, visualização, download e exclusão;
- as permissões do ADR-0045.

**Non-Goals:**
- subpastas, edição do Office, fluxo de aprovação e busca pelo conteúdo;
- os termos de aceite (Documentos-5);
- mover e renomear documentos.

## Decisions

### 1. Módulo `Documents`

- **Schema:** `documents`, com o contexto `DocumentsDbContext`.
- **Contrato próprio:** `Documents.Contracts`, com `DocumentsAudit.FolderReference` para a auditoria mostrar a pasta pelo nome atual (ADR-0084).
- **Pasta de arquivos:** `{código} - {nome}/Documentos/{pasta}` sob a raiz, como os anexos usam `{código} - {nome}/Chamados/#{número}`. O nome "Documentos" fica no código, como "Chamados", até a M365-3 ligar o projeto a uma pasta do SharePoint.

### 2. Pastas padrão (`default_folders`)

- **Campos:** nome (até 120, único), `IsAcceptance` e situação.
- **Migração:** cria as seis pastas do MVP com ids fixos; "06 Termos de aceite" é a de termos.
- **Ordem:** a do nome, porque os nomes do padrão começam com o número. Não há ordem manual.
- **Pasta de termos:** pode ser renomeada, mas não desativada (400); só existe uma, e outra pasta não vira de termos.
- **Auditoria:** entra, com o nome como rótulo.
- **API** (`/api/admin/document-folders`, só do administrador):
  - `GET` traz todas;
  - `POST` inclui com o nome;
  - `PUT /{id}` muda o nome e a situação;
  - nome repetido dá 409 (`document-folder-name-in-use`).

### 3. Pastas do projeto (`folders`)

- **Campos:** o projeto, o nome, `IsAcceptance` e o id da pasta no armazenamento (`StorageFolderId`, nulo até ser criada).
- **Criação:**
  - o `Projects` chama, depois de gravar o projeto novo, os `IProjectSetup` registrados (contrato novo do `Projects.Contracts`), como faz com o `IProjectMemberChanges`;
  - o `Documents` copia as pastas padrão ativas e cria as pastas no armazenamento;
  - a falha vai para o log e não desfaz o projeto;
  - quem abre a área de um projeto sem pastas as recebe na hora, a partir das pastas padrão ativas: é o caso dos projetos criados antes do módulo e de uma falha na criação.
- **Armazenamento:** a pasta ainda sem id é criada no primeiro envio.
- **Auditoria:** fica de fora, porque é cópia das pastas padrão.
- **Mudanças depois:** renomear ou desativar uma pasta padrão não mexe nas pastas dos projetos que já existem.

### 4. Documentos (`documents`) e versões (`document_versions`)

- **Documento:**
  - o projeto, a pasta, o nome do arquivo (até 255);
  - o arquivo no armazenamento, o número da versão atual e o tamanho;
  - o status, quem mudou o status e quando;
  - o vínculo: `SchedulePhaseId` ou `ScheduleItemId`, no máximo um;
  - quem enviou primeiro e quando, quem enviou a última versão e quando.
- **Nome:** é único na pasta (índice único em pasta e nome, sem diferença de maiúsculas, como no SharePoint).
- **Auditoria:**
  - o nome é o rótulo;
  - o projeto é o contexto, pelo nome atual (`ProjectsAudit.ProjectReference`);
  - a pasta e o vínculo aparecem pelos nomes atuais (`DocumentsAudit.FolderReference`, `SchedulePhaseReference` e `ScheduleItemReference`);
  - o arquivo, o tamanho e as datas técnicas ficam de fora;
  - o status e o número da versão entram, para o registro mostrar "versão 1 → 2".
- **Versão:**
  - o número, o id da versão no armazenamento, o tamanho, quem enviou e quando;
  - fica fora da auditoria, porque a mudança do número no documento já registra.
- **Abrir:** abre sempre pelo id da versão guardado no banco. Uma versão que ficou no armazenamento sem chegar ao banco, por uma falha na gravação, nunca aparece.

### 5. Envio

- **Um arquivo por requisição:** `POST /api/projects/{id}/documents`, multipart com `folderId` e `file`. A tela envia os arquivos escolhidos ou soltos um de cada vez.
- **Limite:** `Documents:MaxMegabytes` (25). A rota ajusta o limite do corpo da requisição a esse valor antes de ler o formulário.
- **Recusas:**
  - arquivo vazio, maior que o limite ou programa e script dão 400 (`document-refused`, com o motivo);
  - a regra de tipo e de programa passa para o `Files.Contracts` (`FileTypes`), e os anexos dos chamados a usam, sem duplicar a lista.
- **Mesmo nome:**
  - o documento da pasta com o mesmo nome recebe a nova versão (`SameNameAction.NewVersion`);
  - o Aprovado fica Em revisão (ADR-0093), com quem enviou e quando no registro do status.
- **Pasta de termos:** recusa o envio (409, `acceptance-folder-read-only`).
- **Ao mesmo tempo:** o envio simultâneo de um arquivo novo com o mesmo nome esbarra no índice único e dá 409 (`document-changed`); a tela pede para enviar de novo.
- **Falha ao gravar:** o arquivo novo é apagado do armazenamento, como o logo da instalação.

### 6. Status

- **Valores:** `DocumentStatus` tem `Draft`, `InReview`, `Approved` e `Obsolete`, com os ícones e as cores do design system (document: rascunho, em_revisao, aprovado, obsoleto).
- **Rota:** `PUT /api/projects/{id}/documents/{documentId}/status`, com o status.
- **Regra:** quem envia muda para Rascunho, Em revisão e Obsoleto; só os gestores aprovam (ADR-0045).
- **Registro:** cada mudança grava quem e quando no documento. A gaveta mostra "Aprovado por Marina Costa em 14/10/2026" ou "Status mudado por".
- **Lista padrão:** tira os obsoletos (`status=Active`); `status=All` traz todos.

### 7. Vínculo com o cronograma

- **Contrato novo `IScheduleItems`** (`Projects.Contracts`): `ItemsAsync(projectId)` traz as fases, as tarefas e os marcos do projeto, com o id, o tipo (`Phase`, `Task`, `Milestone`) e o nome atual. O `Projects` o implementa lendo as próprias tabelas.
- **Rota:** `PUT /api/projects/{id}/documents/{documentId}/link`, com `phaseId` ou `itemId` (os dois nulos desfazem). O item precisa ser do projeto (400).
- **Leitura:** a lista e os detalhes trazem o vínculo com o tipo e o nome atual, numa chamada ao contrato por requisição. Um item que saiu do cronograma não vem, e o documento aparece sem vínculo.

### 8. Lista, busca e contagens

- **Pastas:** `GET /api/projects/{id}/documents/folders` traz:
  - as pastas que a pessoa vê, com a contagem dos documentos que não estão obsoletos;
  - o total, que vai na aba;
  - as permissões da pessoa no projeto (`canUpload`, `canApprove`, `canDelete`);
  - o limite de tamanho.
- **Documentos:** `GET /api/projects/{id}/documents?folderId=&search=&status=&page=&pageSize=`:
  - com a busca e sem pasta, procura em todas as pastas que a pessoa vê;
  - ordem pelo nome;
  - cada linha traz a pasta, o status, a versão, o tamanho, a última atualização, o vínculo e o registro do status.
- **Detalhes:** `GET .../documents/{documentId}` traz também as versões.

### 9. Abrir e baixar

- **Rota:** `GET .../documents/{documentId}/content?version=&download=`.
- **No navegador:** PDF, imagens, CSV e texto. O CSV e o texto vão como `text/plain; charset=utf-8`, para o navegador mostrar em vez de baixar.
- **Baixar:** os outros tipos, ou com `download=true`, vão com o nome do arquivo.
- **Cabeçalhos:** `nosniff`, `Content-Security-Policy: sandbox` e `no-store`, como os anexos: o navegador não executa nada do arquivo.
- **Na tela:** "Abrir" só aparece para os tipos que o navegador mostra.

### 10. Exclusão

- **Rota:** `DELETE .../documents/{documentId}`, só dos gestores.
- **O que apaga:** o arquivo com as versões no armazenamento, depois de gravar a exclusão. Se o armazenamento falhar, o registro já saiu e a falha vai para o log.

### 11. Permissões num lugar só

- **`DocumentAccess` (Domain):** recebe o papel no projeto (`IProjectAccess`) e se a pessoa é administradora, e diz:
  - quem vê;
  - quem envia, vincula e muda os status comuns;
  - quem aprova e exclui;
  - quem vê a pasta de termos.
- **Fora do projeto:** quem não vê o projeto recebe 404.
- **Projeto encerrado:** dá 409 (`project-closed`, o mesmo tipo dos chamados e do projeto) em toda alteração.

### 12. Tela

- **Aba "Documentos":**
  - entre "Chamados" e "Membros", como no canvas, com a contagem;
  - rota `/projects/:id/documents`.
- **Computador:**
  - a coluna das pastas (`SideNav`, novo em `web/src/components`, a coluna de navegação já usada em Chamados);
  - a área da pasta com o título e a contagem, "Enviar arquivos", a busca e o filtro;
  - o `DataTable`.
- **Arrastar e soltar:** a lista aceita arquivos soltos e mostra "Solte para enviar a {pasta}". O envio mostra o andamento numa mensagem e, no fim, o que foi enviado e o que foi recusado, com o motivo.
- **Celular:** a pasta vira um `Select` acima da lista.
- **Gaveta (`DetailsDrawer`):**
  - o ícone e o nome, com o tamanho e a versão;
  - "Abrir", "Baixar" e o "⋮", com os status como `menuitemradio`, "Vincular ao cronograma" e "Excluir";
  - as propriedades: status, quem mudou, cronograma (link para a aba), pasta;
  - as versões, cada uma com "Baixar".
- **Diálogos:** vincular, com um `Select` das fases, tarefas e marcos lidos do `GET /schedule`, e confirmar a exclusão.
- **Administração:** "Pastas padrão" em Cadastros, desenhada como "Fornecedores", com a pasta de termos marcada e a nota de que as mudanças valem para os projetos novos.

### 13. Demonstração

- Documentos fictícios, com conteúdo de texto gerado pelo comando, em três pastas do ERP-FIN:
  - um na versão 2;
  - um aprovado pelo líder técnico;
  - um em revisão;
  - um obsoleto;
  - vínculos com tarefas do cronograma.

## Risks / Trade-offs

- **[Arquivos grandes na memória]** O envio passa pelo formulário do ASP.NET Core, que guarda em disco temporário acima de 64 KB. Mitigação: o limite de 25 MB por arquivo, configurável.
- **[Versão sem registro]** Uma falha entre o armazenamento e o banco deixa uma versão a mais no armazenamento. Mitigação: o sistema abre sempre pela versão registrada.
- **[Busca por nome]** O `LIKE` no nome não usa índice. Mitigação: poucos documentos por projeto (ADR-0012).
