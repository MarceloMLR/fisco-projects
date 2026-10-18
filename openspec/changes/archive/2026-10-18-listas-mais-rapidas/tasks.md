# Tasks

## 1. Tabela e menus

- [x] 1.1 No `DataTable`:
  - deixar estáveis `pageSizeOptions`, `paginationModel`, `sx`, `onRowClick`, `onCellKeyDown`, `onPaginationModelChange` e `getRowClassName`;
  - na tela Usuários, passar `isRowMuted` como função fora do componente;
  - testar que desenhar a tela de novo com as mesmas linhas não desenha as células.

  Verificar com `npm test` e `npm run lint`.
- [x] 1.2 No `DataTable`, trocar o overlay de carregamento pelo `DelayedLoadingOverlay`:
  - nada nos primeiros 300 ms, depois a barra (com linhas) ou o esqueleto (sem linhas);
  - fundo transparente;
  - testar "Resposta demorada com indicador" e "Primeira abertura da lista" com uma tabela isolada.

  Verificar com `npm test`.
- [x] 1.3 No tema, `MuiMenu` sem animação de saída. Testar "Escolha num campo de seleção" e "Escolha num menu de ações". Verificar com `npm test`.

## 2. Leituras pelo TanStack Query

- [x] 2.1 Preparar o TanStack Query:
  - adicionar `@tanstack/react-query` 5.104.1 com versão fixa;
  - criar `queryClient.ts` (configuração e novas tentativas) e `queries.ts` (`dataOf`, `ApiError`, `usersQuery`, `auditQuery`);
  - colocar o `QueryClientProvider` no `App` e no `renderApp`, com um cliente novo por teste;
  - limpar o cache no `SessionProvider` quando a sessão termina.

  Verificar com `npm run typecheck`, `npm test` e o `licenses` do `frontend.sh`.
- [x] 2.2 Passar `UsersPage`, `AdminLayout`, `AuditPage` e `PersonFilter` para as consultas, com `keepPreviousData` nas listas, o `loading` da regra do design e a invalidação de `['users']` e `['audit']` depois das ações. Testar:
  - "Volta a uma lista já vista";
  - "Alteração atualiza as listas";
  - "Saída descarta os dados guardados";
  - "Dados só na memória";
  - "Resposta rápida sem indicador".

  Os testes atuais das telas continuam passando. Verificar com `npm test`.

## 3. Verificação final

- [x] 3.1 Medir no navegador, com o build de produção, a troca de filtro da Auditoria e a volta para Usuários, antes e depois, nos temas claro e escuro e no celular. Conferir que não há pisca nem menu por cima da tabela.
- [x] 3.2 Documentação e verificações:
  - atualizar o `CLAUDE.md` (§6, leituras pelo TanStack Query) e o roadmap (Acesso-2.1);
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Acesso-2.1 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, ADR-0080 como aceita, roadmap como "Concluída" e merge.
