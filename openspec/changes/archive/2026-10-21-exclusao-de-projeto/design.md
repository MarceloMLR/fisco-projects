## Context

O projeto tem dados em seis módulos: `Projects` (projeto, pessoas, cronograma, linha de base e desfazer), `Tickets` (chamados, mensagens, anexos, eventos, quem acompanha, avisos de prazo e registros em fornecedores), `Documents` (pastas, documentos e versões, links, termos de aceite e o histórico deles), `Credentials` (acessos), `Notifications` (sino e itens do resumo) e os arquivos, pelo `IFileStorage` (anexos em `{código} - {nome}/Chamados/#{número}`, documentos em `.../Documentos/{pasta}`, termos em `.../Pedidos de aceite`). As tabelas estão ligadas dentro de cada módulo (algumas em cascata, outras com `Restrict`), nunca entre módulos. O armazenamento não tem transação.

## Goals / Non-Goals

**Goals:**
- Apagar tudo do projeto, com os dados do banco inteiros ou nada, e nenhum arquivo do sistema esquecido.
- Repetir a exclusão depois de uma falha termina o trabalho.
- Um registro só na auditoria, com o resumo.

**Non-Goals:**
- Apagar os registros antigos da auditoria (ficam até o prazo, ADR-0110).
- Desfazer a exclusão, ou uma lixeira.
- Excluir clientes, modelos ou fornecedores.

## Decisions

- **Contrato `IProjectDeletion`** (`Projects.Contracts`), com `CountAsync(projectId)` (as quantidades do módulo, por tipo), `DeleteFilesAsync(projectId)` e `DeleteDataAsync(projectId, DbTransaction)` (as quantidades apagadas). Recebe a transação do ADO.NET, sem o EF Core no contrato; cada módulo monta o próprio contexto na conexão dela, sem a auditoria, como os apagadores de dados pessoais (ADR-0110). Alternativa descartada: avisar depois de gravar o projeto, que deixaria dados de outros módulos sem projeto numa falha.
- **`DeleteProject`** (Projects): confere o projeto e o código (sem diferenciar maiúsculas e minúsculas, sem espaços nas pontas), chama `DeleteFilesAsync` de cada módulo e depois, numa transação serializável do `ProjectStore`, `DeleteDataAsync` de cada módulo e o apagar do próprio `Projects` (dependências, itens da linha de base, linha de base, desfazer, itens, fases, cronograma, pessoas e projeto), tudo por `ExecuteDelete`, filhos antes dos pais. O registro da auditoria vai pelo `IChangeAuditor`, na mesma transação: item `Project`, ação `Deleted`, rótulo o nome, com os campos `Code`, `Name`, `ClientId` (referência ao cliente) e `Status` antes, e as quantidades (`Tickets`, `Documents`, `Links`, `Credentials`, `AcceptanceTerms`, `ScheduleItems`, `Members`) depois.
- **Resumo** (`ProjectDeletionSummary`): `GetProjectDeletion` soma o `CountAsync` de cada módulo com as tarefas e marcos e as pessoas do projeto.
- **Arquivos**: `IFileStorage.FolderOfAsync(id)` diz a pasta de um arquivo ou de uma pasta (nenhuma na raiz ou se não existe). O `StoredFilesRemoval` do `Files.Contracts` apaga os arquivos (guardando as pastas deles) e depois, para cada pasta, apaga enquanto estiver vazia, subindo pela pasta de cima, até a raiz. Os módulos passam os ids que registraram: anexos e pastas dos chamados; documentos, pastas dos documentos e termos.
- **Avisos**: `DigestItem` ganha `ProjectId` (do `Notice`), e as notificações e os itens do projeto são apagados; os itens antigos, sem projeto, saem no resumo do dia seguinte, como sempre.
- **Permissão**: `ProjectPermissions.CanDelete` (só o administrador), no `permissions` do projeto; as rotas usam a política do administrador. O código errado é 400 com `code`.
- **Tela**: "Excluir projeto" no fim de "Ações do projeto", em vermelho, abre um diálogo de alerta com o resumo (`projectDeletionQuery`), o aviso e o campo do código; "Excluir projeto" só fica ativo com o código igual. Depois, a tela vai para Projetos, mostra "Projeto {código} excluído." e descarta do cache o projeto e o que é dele.

## Risks / Trade-offs

- [Falha entre os arquivos e os dados] → o projeto fica com arquivos faltando até repetir; os downloads desses arquivos dão 404, como um arquivo apagado.
- [Projeto grande] → a transação apaga tudo por `ExecuteDelete`, sem carregar as linhas; o tempo é o de alguns comandos por tabela.
- [Arquivos de fora na pasta do projeto] → as pastas só saem vazias, então o que não é do sistema fica, com as pastas acima.
