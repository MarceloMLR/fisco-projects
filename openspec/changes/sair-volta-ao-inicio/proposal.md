# Proposta: Sair volta ao Início (Acesso-1.1)

## Why

Marcelo saiu do sistema e entrou com outra pessoa: a aplicação abriu a página em que a primeira pessoa estava. A página de entrada guarda a tela pedida para devolver a pessoa a ela depois de entrar, mas não distingue quem saiu por vontade própria de quem perdeu a sessão, nem quem volta de quem entra no lugar.

## What Changes

- **Depois de "Sair"**, a próxima entrada leva sempre ao Início, seja quem for.
- **Sessão expirada** (uma chamada que volta 401): a página de entrada guarda a tela e quem estava nela; se a mesma pessoa entrar, volta à tela; se for outra, vai ao Início.
- **Tela aberta sem sessão** (um link de aviso, por exemplo) continua levando à tela pedida depois de entrar.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `entrada-e-sessao`: a volta à tela pedida depende de como a sessão acabou e de quem entra; "Sair" leva ao Início na entrada seguinte.

## Impact

- `web/src/session` (`SessionProvider`, `SessionContext`, `RequireSession`) e `web/src/pages/SignInPage.tsx`, com os testes de tela.
- Nenhuma mudança na API.
