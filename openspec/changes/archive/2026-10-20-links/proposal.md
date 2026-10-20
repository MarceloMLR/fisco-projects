# Proposta: links do projeto (Documentos-2)

## Why

O time guarda os endereços de cada projeto (sistemas, ambientes, repositórios, painéis) em mensagens e planilhas. O ADR-0032 decidiu os links do projeto na v1, e o ADR-0045, quem faz o quê:

- **Links do projeto:** título, endereço, categoria e descrição, guardados no banco do FiscoProjects, sem arquivos.
- **Na tela:** abrir em nova aba e copiar o endereço; busca por título e endereço.
- **Categorias:** de uma lista configurável na Administração. O padrão é Sistemas, Ambientes, Repositórios, Painéis e Outros.
- **Permissões:** todos do projeto veem; todos, menos o leitor, cadastram, editam e excluem.

## What Changes

- **Categorias de links** (`/api/admin/link-categories`, no módulo `Documents`, ADR-0092):
  - o administrador inclui, renomeia, desativa e reativa as categorias, com nomes únicos;
  - a migração cria as cinco do padrão;
  - a categoria inativa não aparece para links novos, e os links que já a usam continuam com ela;
  - as categorias ativas ficam em `GET /api/link-categories`, para quem tem sessão.
- **Links** (`/api/projects/{id}/links`):
  - a lista do projeto, com a busca por título e endereço e o filtro por categoria;
  - incluir, editar e excluir, com o título (até 120), o endereço (http ou https, até 2.000), a categoria e a descrição (até 500, opcional);
  - quem incluiu e quando;
  - a auditoria, com o projeto e a categoria pelos nomes atuais.
- **Permissões** (ADR-0045):
  - quem está no projeto vê; o leitor só vê;
  - o projeto encerrado é só leitura (ADR-0016).
- **Na tela:**
  - a aba "Links" do projeto, como no canvas ProjetoLinks, com a contagem;
  - os links agrupados por categoria, a busca, as categorias num `SegmentedControl` e "Novo link";
  - em cada linha, copiar o endereço, abrir em nova aba e o "⋮" com editar e excluir;
  - "Categorias de links" na Administração, desenhada como "Pastas padrão".
- **Demonstração:** links fictícios do ERP-FIN em quatro categorias.

## Capabilities

### New Capabilities

- `links`: categorias de links, links do projeto, busca, permissões e a tela.

### Modified Capabilities

- `dados-de-demonstracao`: links na demonstração.

## Impact

- **Backend:** módulo `Documents`, com as tabelas `link_categories` e `links` e a migração.
- **Frontend:** a aba Links e a página "Categorias de links".
- **Documentação:** CLAUDE.md, glossário e roadmap.

## Non-goals

- Links que já vêm com o modelo de projeto (depois, ADR-0032).
- Verificar se o link está no ar (fora, ADR-0032).
- Ordem manual das categorias: a lista segue a ordem do nome, como as pastas padrão.
