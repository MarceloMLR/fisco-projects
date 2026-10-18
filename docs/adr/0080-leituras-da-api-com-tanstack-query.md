---
status: proposta
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0080: Leituras da API no frontend com TanStack Query, guardadas só na memória da sessão

## Contexto e problema

O frontend fala com a API pelo cliente gerado do contrato (ADR-0076). Hoje cada tela lê a API no próprio `useEffect` e guarda o resultado num estado local. Nada fica guardado: voltar a uma lista ou a um filtro já visto espera de novo pela API e mostra o carregamento outra vez. Uma alteração também não atualiza as outras telas que mostram o mesmo item. O padrão se repete em cada tela (Usuários, Auditoria, a contagem da Administração, o filtro de pessoa), e as próximas telas (Meu trabalho, projetos, chamados, sino) vão precisar dele. Como as telas devem ler a API?

## Fatores de decisão

- Voltar a uma tela já vista sem esperar, sem mostrar dado velho por mais que uma resposta.
- Atualizar as listas afetadas depois de uma alteração, num lugar só.
- Nenhum dado no armazenamento do navegador. Lá só fica o cookie de sessão `HttpOnly` (ADR-0054), e os dados de uma pessoa não podem ficar para a próxima (ADR-0058).
- Licença MIT ou equivalente, sem dependência comercial (ADR-0061).
- Menos código próprio para manter (reuse, não reinvente).

## Opções consideradas

- A. TanStack Query (`@tanstack/react-query`)
- B. Hook próprio, com um cache em memória escrito no projeto
- C. SWR
- D. RTK Query

## Resultado da decisão

Opção escolhida: **A. TanStack Query**. É a biblioteca mais usada para isso no React, com licença MIT. Ela já traz o que as telas precisam: cache por chave, atualização em segundo plano, dados anteriores durante a troca de filtro, invalidação por prefixo, cancelamento, novas tentativas e um pedido só para consultas iguais.

Regras:

- As telas leem a API por consultas do TanStack Query, definidas num arquivo só (`web/src/api/queries.ts`). Cada consulta usa o cliente gerado do contrato (ADR-0076) e uma chave que começa pelo recurso (`['users', ...]`, `['audit', ...]`).
- Os dados ficam só na memória da página. Não se usa *persister* nem armazenamento do navegador.
- O cache é descartado quando a sessão termina, ao sair ou por um 401.
- As alterações continuam chamando o cliente da API. Quando dão certo, invalidam as consultas dos recursos afetados.
- A sessão (`/api/session`) continua fora do cache, no `SessionProvider`.

### Consequências

- Bom, porque voltar a uma tela já vista mostra a lista na hora e a atualiza em seguida.
- Bom, porque as próximas telas não repetem o controle de carregamento e de respostas antigas.
- Bom, porque consultas iguais viram um pedido só, inclusive o pedido em dobro do `StrictMode` no desenvolvimento.
- Ruim, porque acrescenta cerca de 13 kB comprimidos ao pacote e um conceito novo (chaves e invalidação) para quem escreve telas.
- Neutro, porque uma lista revisitada mostra por um instante o que estava guardado, até a resposta nova chegar.

### Confirmação

Testes de tela cobrindo:

- a volta a uma lista já vista, que aparece sem esperar e é pedida de novo;
- uma alteração que pede de novo as listas afetadas;
- a saída seguida da entrada de outra pessoa, sem dados da sessão anterior;
- nenhum dado no `localStorage` ou no `sessionStorage`.

A lista de licenças do `frontend.sh` aceita a dependência.

## Prós e contras das opções

### A. TanStack Query

- Bom, porque cobre cache, invalidação, cancelamento e deduplicação, testados por uma comunidade grande.
- Bom, porque não grava nada no navegador sem um *persister* explícito.
- Ruim, porque é mais uma dependência a atualizar.

### B. Hook próprio

- Bom, porque não acrescenta dependência.
- Ruim, porque o projeto teria de escrever, testar e manter cache, invalidação, cancelamento e novas tentativas.

### C. SWR

- Bom, porque é pequeno e também tem licença MIT.
- Ruim, porque a invalidação por prefixo de chave e os dados provisórios durante a troca de filtro exigem mais código nas telas.

### D. RTK Query

- Bom, porque é completo.
- Ruim, porque exige Redux, que o projeto não usa e não precisa.
