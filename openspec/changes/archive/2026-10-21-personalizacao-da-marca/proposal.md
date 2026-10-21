## Why

Marcelo pediu mais personalização da marca: além da cor principal, uma cor secundária, que ele quer na barra lateral, nos destaques de apoio e no painel da entrada; um seletor de cores ao clicar na cor; e um segundo logo, para o tema escuro (Acesso-8.1, a pedido de Marcelo, ADR-0063, ADR-0082).

## What Changes

- ADR-0100: cor secundária opcional, em qualquer cor, com os textos calculados para ter contraste; logo opcional para o tema escuro.
- Configurações gerais: o campo "Cor secundária" (hexadecimal, "Sem cor secundária" e a amostra), "Logo para o tema escuro" (enviar, trocar e remover, num quadro escuro) e, nas duas cores, o seletor de cores do navegador ao clicar na amostra. A Prévia mostra a cor secundária e o logo escuro antes de salvar.
- Com a cor secundária, nos dois temas: a barra lateral usa a cor (no tema escuro, um tom escuro dela), com textos, contadores, item ativo e nome da instalação com contraste; a linha selecionada, a notificação não lida e a menção ganham o matiz dela; o painel da entrada sai dela no lugar da cor principal.
- A barra lateral escura (tema escuro ou cor secundária escura) mostra o logo escuro, quando há.
- API: `secondaryColor` e `darkLogoUrl` em `GET /api/installation`; `secondaryColor` em `PUT /api/installation`; `GET`, `PUT` e `DELETE /api/installation/logo/dark`, com as regras do logo e só do administrador.
- Auditoria: a cor secundária e o logo escuro com os nomes "Cor secundária" e "Logo para o tema escuro".

## Capabilities

### New Capabilities

### Modified Capabilities
- `configuracao-da-instalacao`: a cor secundária, o logo para o tema escuro e o seletor de cores.

## Impact

- `Administration` (domínio, casos de uso, rotas e a migração `InstallationSecondaryColorAndDarkLogo`), `web/src/theme/brandColors.ts`, Configurações gerais e Prévia, barra lateral, textos e testes.
