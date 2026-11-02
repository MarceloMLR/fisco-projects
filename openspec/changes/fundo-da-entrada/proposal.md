## Why

Pela seção Cores (ADR-0146), a cor secundária pinta o painel da página de entrada, mas o fundo em volta do cartão continua o azul-marinho do design system. Ao testar com uma secundária vinho, Marcelo viu o painel vinho sobre o fundo azul e pediu, em 02/11/2026, que o fundo também mudasse (ADR-0147). Acesso-8.5.1.

## What Changes

- O fundo da página de entrada e o brilho dele (`signin-bg` e `signin-glow`) passam a seguir a cor secundária ou, sem ela, a principal, como o painel.
- Os dois mantêm a claridade escura do design system, nos dois temas, com o matiz e a saturação da cor.
- A prévia da seção Cores mostra o painel sobre o fundo da entrada, e o texto de onde a secundária aparece cita o fundo.
- Sem nenhuma cor, a página fica a do design system.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `configuracao-da-instalacao`: o fundo da entrada na cor secundária (ou na principal), na marca sem sessão e na prévia da seção Cores.

## Non-goals

- Uma cor própria para o fundo da entrada.
- Mudar o cartão, o formulário ou os textos da página de entrada.
- Mudar a regra de contraste: nenhum texto fica direto sobre o fundo.

## Impact

- `web/src/theme/brandColors.ts`: `signin-bg` e `signin-glow` entre os tokens derivados.
- `web/src/admin/ColorsPreview.tsx`: o painel sobre o fundo da entrada.
- Textos (`pt-BR.json`), testes de `brandColors.test.ts`.
- Documentação: o ADR-0147, o ADR-0146 apontando para ele, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
