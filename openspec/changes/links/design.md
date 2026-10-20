# Design: links do projeto (Documentos-2)

## Context

- **ADR-0032:** links do projeto com título, endereço, categoria e descrição, no banco; abrir e copiar; categorias de uma lista da Administração; busca por título e endereço.
- **ADR-0045:** todos veem; todos, menos o leitor, cadastram, editam e excluem.
- **ADR-0092:** os links ficam no módulo `Documents`.
- **O que já existe:**
  - o módulo `Documents`, com o `DocumentAccess` e as pastas padrão, que serviram de modelo para a lista da Administração;
  - o `IProjectAccess`, com o papel e o projeto encerrado;
  - o `DataTable` com linhas de grupo e o `SegmentedControl`.
- **Canvas ProjetoLinks:**
  - a página com a tabela e as ferramentas: a busca "Buscar por título ou endereço", as categorias e "Novo link";
  - os grupos por categoria, com a contagem;
  - as colunas Link (a inicial e o título), Endereço (sem o protocolo), Para que serve, Incluído por (nome e data) e as ações (copiar, abrir e "⋮").

## Goals / Non-Goals

**Goals:**
- as categorias na Administração;
- os links do projeto, com a busca e o filtro;
- as permissões do ADR-0045.

**Non-Goals:**
- links que vêm com o modelo de projeto;
- verificar se o link está no ar;
- ordem manual das categorias.

## Decisions

### 1. Categorias (`link_categories`)

- **Campos:** nome (até 80, único) e situação.
- **Migração:** cria Sistemas, Ambientes, Repositórios, Painéis e Outros com ids fixos.
- **Ordem:** a do nome, como as pastas padrão. A ordem manual ficaria para uma tela que ainda não existe.
- **Auditoria:** entra, com o nome como rótulo.
- **API:**
  - `/api/admin/link-categories` (`GET`, `POST`, `PUT /{id}`), só do administrador;
  - nome repetido dá 409 (`link-category-name-in-use`);
  - `GET /api/link-categories` traz as ativas, para quem tem sessão.
- **Nunca excluída:** só desativada, porque os links a guardam.

### 2. Links (`links`)

- **Campos:** o projeto, a categoria, o título (até 120), o endereço (http ou https, até 2.000), a descrição (até 500, opcional), quem incluiu e quando, quem mudou por último e quando.
- **Auditoria:**
  - o título é o rótulo;
  - o projeto é o contexto e a categoria aparece pelo nome atual (`DocumentsAudit.LinkCategoryReference`);
  - quem incluiu e as datas ficam de fora.
- **Categoria inativa:**
  - não é aceita num link novo nem numa troca de categoria (400);
  - o link que já a usa pode ser editado sem trocá-la, como o cliente inativo de um projeto.

### 3. API

- **Lista:** `GET /api/projects/{id}/links?search=&categoryId=`:
  - traz os links do projeto, na ordem da categoria e do título, sem paginação (um projeto tem poucos links);
  - a busca procura no título e no endereço;
  - traz as categorias que os links do projeto usam, com a contagem, sem a busca, para o controle segmentado e para a aba;
  - traz se a pessoa pode cadastrar (`canEdit`, falso no projeto encerrado).
- **Mudanças:** `POST /api/projects/{id}/links`, `PUT .../{linkId}` e `DELETE .../{linkId}`.
- **Erros:** 400 com os campos, 403 para o leitor, 404 fora do projeto e 409 (`project-closed`) no projeto encerrado.

### 4. Permissões

- **`DocumentAccess`:** ganha o `MayEditLinks` (todos, menos o leitor), no mesmo lugar das regras dos documentos (ADR-0045).

### 5. Tela

- **Aba "Links":**
  - entre "Documentos" e "Membros", com a contagem dos links;
  - rota `/projects/:id/links`.
- **Tabela (`DataTable` com linhas de grupo):**
  - um grupo por categoria, com a contagem;
  - Link: um quadrado com a inicial do título e o título;
  - Endereço: sem o protocolo, que abre em nova aba;
  - Para que serve: a descrição;
  - Incluído por: o nome e a data;
  - as ações: "Copiar endereço", "Abrir em nova aba" e o "⋮" com "Editar" e "Excluir".
- **Ferramentas:** a busca, as categorias num `SegmentedControl` ("Todos" e as categorias usadas) e "Novo link".
- **Copiar:** pelo `navigator.clipboard`, com a mensagem "Endereço copiado".
- **Diálogo:** formulário RHF com Zod, com o título, o endereço, a categoria (as ativas e a atual do link) e a descrição (`TextArea`).
- **Celular:** a coluna Link traz o título e o endereço; as outras ficam de fora, menos as ações.
- **Administração:** "Categorias de links" em Cadastros, desenhada como "Pastas padrão".

### 6. Demonstração

- Links fictícios do ERP-FIN em Ambientes, Sistemas, Painéis e Repositórios, incluídos pelo líder técnico, pela colaboradora, pelo desenvolvedor e pela gerente.

## Risks / Trade-offs

- **[Endereço com dados sensíveis]** Um link pode levar um token na consulta. Mitigação: a tela mostra o endereço sem o protocolo, como no canvas; a política de não guardar segredos fica no guia de uso, e senhas ficam nos acessos (Documentos-3).
