---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0054: Autenticação do React na API pelo padrão BFF, com cookie de sessão

## Contexto e problema

O frontend React (ADR-0049) é servido pelo mesmo host da API (ADR-0047). O login é pelo Entra ID, com página de entrada própria e sessão de 8 horas sem uso (ADR-0009); a cada requisição o sistema confere se o usuário está ativo (ADR-0008) e lê os papéis do banco (ADR-0006). Como o React se autentica na API?

## Fatores de decisão

- Segurança: tokens fora do alcance do JavaScript da página.
- Aderência às decisões de sessão, usuário ativo e papéis.
- Simplicidade de configuração.

## Opções consideradas

- A. Backend para o frontend (BFF), com cookie de sessão
- B. Tokens no navegador (MSAL.js)

## Resultado da decisão

Opção escolhida: **A. BFF com cookie de sessão**: o ASP.NET Core faz o login com a Microsoft e entrega ao navegador apenas um cookie de sessão protegido; os tokens da Microsoft nunca chegam ao navegador.

Regras:

- React e API no mesmo endereço.
- Cookie HttpOnly, Secure e SameSite; proteção contra requisições forjadas (CSRF) em toda alteração.
- Sessão com expiração deslizante de 8 horas sem uso (ADR-0009); "Sair" encerra só a sessão do FiscoProjects.
- A cada requisição, o sistema confere se o usuário está ativo e carrega os papéis do banco.
- Chamadas à API com sessão expirada recebem 401, e o React leva a pessoa à página de entrada.
- Nenhum token em `localStorage`, `sessionStorage` ou cookies legíveis por JavaScript.
- Autenticação por token para outros clientes (aplicativo, integrações) só no futuro, com ADR próprio.

### Consequências

- Bom, porque um ataque de script na página não encontra token para roubar.
- Bom, porque sessão, usuário ativo e papéis ficam controlados no servidor.
- Ruim, porque a API atende só o próprio frontend até que se acrescente autenticação por token.

### Confirmação

Testes cobrindo: login cria o cookie com os atributos corretos; requisição de alteração sem token anti-CSRF é recusada; sessão expirada retorna 401; usuário desativado é recusado na requisição seguinte; nenhum token é exposto ao navegador.

## Prós e contras das opções

### A. BFF com cookie

- Bom, porque é o modelo mais seguro para aplicações web e se encaixa nas decisões de sessão.
- Ruim, porque não atende outros clientes sem mudança.

### B. Tokens no navegador

- Bom, porque é comum em React e serve outros clientes.
- Ruim, porque expõe tokens ao JavaScript e exige mais configuração no Entra e no React.
