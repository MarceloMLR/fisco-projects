## Why

Hoje todas as pessoas do projeto veem todas as pastas de documentos, menos a de termos (ADR-0045). Um colaborador vê "01 Gestão do projeto", que em geral é só da gestão, e a regra varia de empresa para empresa e até de projeto para projeto. Em 02/11/2026, Marcelo pediu que quem vê cada pasta seja configurável, nas pastas padrão e em cada projeto, por papel, só pelo administrador (ADR-0148). Documentos-1.1.

## What Changes

- **Quem vê:** cada pasta, padrão ou do projeto, ganha os papéis que a veem, entre Líder técnico, Colaborador, Desenvolvedor e Leitor. O administrador e o gerente sempre veem todas, e a pasta de termos fica só com eles, sem como mudar.
- **Pastas padrão:** "Nova pasta padrão" e "Editar pasta padrão" ganham "Quem vê", e a lista mostra quem vê cada uma. A pasta nova começa com os quatro papéis.
- **Projeto novo:** recebe o "Quem vê" de cada pasta padrão; mudar a padrão depois vale só para os novos.
- **Ajuste no projeto:** na aba Documentos, o administrador muda quem vê a pasta escolhida ("Quem vê esta pasta"), com o projeto em aberto. As pastas que não são vistas por todos os papéis ganham a marca de restrita para a gestão.
- **Quem não vê a pasta:** não a vê nem vê os documentos dela (na lista, na busca, em "Todos", no detalhe, no arquivo), não envia nada nela e não conta os documentos dela.
- **API:**
  - `GET .../documents/folders` traz `visibleTo` em cada pasta e `canChangeVisibility`;
  - nova rota `PUT /api/projects/{id}/documents/folders/{folderId}/visibility` (`ChangeFolderVisibility`, só do administrador, 403 para as outras pessoas do projeto, 400 na pasta de termos, 409 no projeto encerrado);
  - as rotas das pastas padrão recebem e devolvem `visibleTo`.
- **Banco:** a migração inclui a coluna nas pastas padrão e nas do projeto, com os quatro papéis em todas e nenhum na de termos.
- **Auditoria:** a mudança numa pasta padrão entra como as outras; a de uma pasta do projeto entra num registro próprio, com o projeto.
- **Demonstração:** o ERP-FIN esconde "01 Gestão do projeto" de quem não é da gestão nem líder técnico.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `documentos`:
  - as pastas padrão e as do projeto com quem vê;
  - a mudança no projeto pelo administrador;
  - as permissões por pasta;
  - a tela das pastas padrão e da aba Documentos.
- `dados-de-demonstracao`: a pasta da gestão escondida no projeto principal.

## Non-goals

- Liberar uma pasta para uma pessoa específica, ou só leitura numa pasta: não foram escolhidos.
- O gerente ajustar a visibilidade no projeto.
- Mudar as permissões do SharePoint (ADR-0030): quem entra direto nele vê pelas permissões dele.
- Mudar a matriz de enviar, aprovar e excluir (ADR-0045).

## Impact

- `src/Modules/Documents`:
  - `DefaultFolder` e `DocumentFolder` com quem vê;
  - a regra no `DocumentAccess` e no `DocumentAreas`;
  - os casos de uso das pastas padrão e o `ChangeFolderVisibility`, com o registro na auditoria pelo `IChangeAuditor`;
  - as rotas e a migração.
- `tests/`: as regras, os casos de uso e a API (por papel, a pasta de termos, o projeto encerrado e a auditoria).
- `web/src`:
  - o `DefaultFolderDialog` e a `DefaultFoldersPage` com quem vê;
  - a aba Documentos com "Quem vê esta pasta" (`FolderVisibilityDialog`) e a marca de restrita;
  - a Auditoria (`auditText.ts`), os textos, os tipos gerados, o simulador e os testes;
  - o plano da demonstração.
- Documentação: o ADR-0148, o ADR-0045 apontando para ele, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
