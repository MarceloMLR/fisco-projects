# Proposta: cor principal da instalação (Acesso-8)

## Why

Cada empresa pode trocar a cor principal da instalação (ADR-0003, ADR-0050). Com a nova cor mudam o botão principal, os links, os controles marcados, o anel de foco, o item ativo da barra lateral, o avatar do responsável e o painel da entrada (ADR-0063, ADR-0067, design system "Marca da instalação"). Hoje o azul vem fixo do `tokens.json`, e as Configurações gerais não têm a cor do canvas.

É a última das três partes da antiga Acesso-6, a pedido de Marcelo. Ela parte da Acesso-7 (fuso).

## What Changes

- **Decisão (ADR-0082, proposta):** os tokens da marca são derivados da cor escolhida em OKLCH, a partir do valor padrão de cada token.
  - Com a cor padrão, nada muda.
  - Os pares de contraste do design system são conferidos nos dois temas.
  - Uma cor sem 4,5:1 com o branco é recusada.
- **Cor principal nas configurações da instalação** (módulo `Administration`):
  - hexadecimal `#RRGGBB` ou vazio para a cor padrão, com a migração `InstallationMainColor`;
  - o servidor confere o formato e o contraste com o branco.
- **API:**
  - `GET /api/installation` passa a trazer `mainColor`, porque o painel da página de entrada usa a cor;
  - `PUT /api/installation` recebe `{ name, timeZone, mainColor }`.
- **Cores aplicadas em toda a aplicação:** as variáveis da lista do design system, nos temas claro e escuro, mais as do MUI (`primary`), são trocadas por uma folha de estilo gerada da cor. As cores de status nunca mudam.
- **Configurações gerais:** na seção Marca, como no canvas:
  - a cor principal, com a amostra, o campo hexadecimal e "Voltar ao padrão";
  - o selo "Contraste aprovado" ou "Contraste insuficiente";
  - a Prévia, que mostra a cor antes de salvar.
- **Auditoria:** a troca da cor entra como mudança das configurações, com o campo "Cor principal".

## Non-goals

- Escolher a cor de cada token, ou uma segunda cor (ADR-0082, opção C).
- Mudar o fundo marinho da página de entrada em volta do cartão (`signin-*`), que o design system não põe na troca.
- Gerar um ícone (favicon) na cor da instalação.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `configuracao-da-instalacao`:
  - novo requisito "Cor principal";
  - "Marca visível sem sessão" passa a incluir a cor;
  - "Tela Configurações gerais" ganha a cor na seção Marca;
  - "Mudança das configurações na auditoria" passa a citar a cor.

## Impact

- Backend (módulo `Administration`):
  - `MainColor` em `InstallationSettings`, com `ChangeMainColor` (formato e contraste) e a migração;
  - `UpdateInstallation` com a cor;
  - a cor em `GET /api/installation`;
  - testes.
- Contrato OpenAPI e tipos do frontend.
- Frontend:
  - `web/src/theme/brandColors.ts` (conversão OKLCH, derivação, contraste e a folha de estilo);
  - `BrandColors` na rota raiz;
  - a cor e a Prévia nas Configurações gerais;
  - "Cor principal" na Auditoria;
  - traduções e testes.
- Documentação: ADR-0082, `docs/roadmap.md` (Acesso-8), `docs/glossario.md` (cor principal) e o `CLAUDE.md`.
- ADRs: 0003, 0050, 0063, 0067 (cor da marca por instalação, contraste AA, o que acompanha a cor), 0046 e 0039.
