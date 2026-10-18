# Proposta: listas mais rápidas (Acesso-2.1)

## Why

Nas telas Usuários e Auditoria, Marcelo percebe uma espera ao abrir a lista e a cada troca de filtro, "como se estivesse fazendo a requisição", mesmo com poucos dados. Medido na máquina dele, a API responde em 7 a 18 ms. O tempo vai no frontend:

- A tabela redesenha todas as células a cada mudança da tela, mesmo sem linhas novas.
- O menu do filtro leva cerca de 250 ms sumindo por cima da tabela.
- O esqueleto de carregamento pisca mesmo quando a resposta já chegou.
- Nada fica guardado: voltar a uma lista ou a um filtro já visto espera de novo.

Como as próximas telas (Meu trabalho, projetos, chamados) usam o mesmo padrão, convém corrigir agora, antes que ele se repita.

## What Changes

- **Leituras da API pelo TanStack Query** (ADR-0080, novo). As respostas ficam guardadas na memória da página durante a sessão, e cada lista ou filtro já visto volta na hora. A API é consultada de novo em seguida para atualizar a lista. Isso vale para Usuários, Auditoria, a contagem da Administração e o filtro de pessoa.
- **Alterações atualizam as listas afetadas.** Trocar o papel, desativar ou reativar alguém pede de novo Usuários, a contagem e a Auditoria.
- **Dados descartados ao sair** ou quando a sessão termina.
- **Espera nas listas:**
  - Durante a troca de filtro ou de página, a tabela continua mostrando as linhas atuais.
  - Se a espera passar de 300 ms, aparece uma barra de progresso no topo da tabela.
  - Na primeira abertura, a tabela mostra o cabeçalho e só mostra o esqueleto se a espera passar de 300 ms. Nunca mostra a mensagem de lista vazia antes da resposta.
- **Menus fecham na hora** ao escolher uma opção, sem animação de saída. Isso vale para os menus e campos de seleção. A abertura continua com a animação de hoje.
- **Tabela sem redesenho inútil.** O `DataTable` passa ao Data Grid propriedades estáveis, então uma mudança na tela que não troca as linhas não redesenha as células.

## Non-goals

- Mudar a API, as consultas ao banco ou o tempo de resposta do servidor.
- Guardar dados no armazenamento do navegador (`localStorage`, `sessionStorage`, IndexedDB) ou entre sessões.
- Atualização automática por tempo ou avisos em tempo real (o sino vem na change dele).
- Dividir o pacote do frontend em partes carregadas sob demanda.
- Tirar o `StrictMode` do desenvolvimento.
- Mudar a sessão (`SessionProvider`), que continua com o próprio pedido à API.
- Levar o item Administração direto para `/admin/users`. O ganho medido é pequeno, e o item precisa continuar marcado em todas as páginas da Administração.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `aplicacao-web`: novos requisitos comuns a todas as telas:
  - dados da API guardados durante a sessão;
  - espera nas listas;
  - menus que fecham na hora.

## Impact

- Frontend, arquivos novos:
  - `web/src/api/queries.ts`, com as consultas e as chaves.
  - `web/src/api/queryClient.ts`, com a configuração do cache.
- Frontend, arquivos alterados:
  - `web/src/App.tsx` e `web/src/session/SessionProvider.tsx`, que limpa o cache ao sair;
  - as telas `UsersPage`, `AuditPage`, `AdminLayout` e o `PersonFilter`;
  - o `DataTable`;
  - o tema (menus);
  - `web/src/test/renderApp.tsx` e os testes.
- Dependência nova: `@tanstack/react-query` 5.104.1, licença MIT, cerca de 13 kB no pacote.
- Documentação:
  - ADR-0080 novo e a linha no `docs/adr/README.md`;
  - `CLAUDE.md` (§6, frontend);
  - `docs/roadmap.md` (Acesso-2.1).
- ADRs tocados: 0049 (frontend React), 0050 (MUI e Data Grid), 0054 (nada além do cookie de sessão no navegador) e 0066 (telas na altura da janela), sem mudar as decisões. O ADR-0080 completa o ADR-0076, que deixou o cliente HTTP para depois.
