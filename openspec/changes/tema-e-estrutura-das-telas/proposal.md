# Proposta: tema e estrutura das telas (Fundação-5)

## Why

As telas da v1 seguem o design system FiscoProjects e o canvas de telas aprovados em 17/10/2026 (ADR-0062 a ADR-0067), com MUI (ADR-0050). Hoje o frontend só tem uma página provisória sem estilo. Antes das telas de cada módulo, o projeto precisa do tema com os tokens do design system, da IBM Plex servida pela aplicação e da estrutura comum a todas as telas: barra lateral, cabeçalho invisível, conteúdo na altura da janela e a versão do celular (ADR-0012, ADR-0066).

## What Changes

- MUI com um tema central em variáveis CSS, temas claro e escuro, montado a partir de uma cópia exata do `tokens.json` do design system. As demais variáveis do design system (`--sidebar-bg`, `--status-*` e as outras) ficam disponíveis com os mesmos nomes para os componentes próprios.
- IBM Plex Sans e IBM Plex Mono nos pesos 400 e 500, servidas pela aplicação a partir dos pacotes `@fontsource`.
- Tema automático: segue o tema do sistema operacional, o padrão do ADR-0014. A escolha da pessoa entra com o menu da conta e o perfil.
- Estrutura das telas no computador: barra lateral fixa de 232px com o selo e o nome do produto, Início, Projetos e Chamados; cabeçalho invisível de 56px com a trilha da página; conteúdo na altura da janela, que não rola como um todo.
- No celular (abaixo de 900px): barra superior com o botão de menu e o título da página, e o menu que abre por cima com a navegação.
- Rotas do frontend com React Router (ADR-0077): Início (`/`), Projetos (`/projects`) e Chamados (`/tickets`), cada uma por enquanto só com o título da página; endereço desconhecido leva ao Início.
- Testes de que as cores vêm só dos tokens, de contraste mínimo, do tema automático, da barra lateral, da trilha, do menu no celular e da altura da janela.

## Non-goals

- Sino e painel de notificações (Avisos-3).
- Avatar e menu da conta (Meu perfil, Tema e Sair) e a página de entrada (Acesso-1); tema salvo no perfil (Acesso-5).
- Item Administração, só para administradores (Acesso-2), contador de chamados (Chamados) e "Meus projetos" (Projetos e Síntese).
- Nome, logo e cor da instalação (Acesso-5); até lá, o nome do produto aparece no topo da barra lateral.
- Conteúdo das páginas Início, Projetos e Chamados (changes de cada área).
- Testes de ponta a ponta com Playwright.

## Capabilities

### New Capabilities

- `estrutura-das-telas`: o tema visual comum (cores, fontes, temas claro e escuro) e a estrutura que envolve todas as telas no computador e no celular.

### Modified Capabilities

Nenhuma.

## Impact

- Código novo: `web/src/theme` (cópia do `tokens.json`, variáveis CSS e tema do MUI), `web/src/shell` (estrutura, barra lateral, cabeçalho e menu do celular), `web/src/pages` e as rotas.
- Código alterado: `web/src/App.tsx`, `web/src/main.tsx`, traduções em `pt-BR.json` e a lista de licenças permitidas (OFL-1.1, das fontes).
- Dependências novas, para o navegador: `@mui/material` e `@mui/icons-material` 9 (MIT), `@emotion/react` e `@emotion/styled` (MIT), `react-router` 8 (MIT), `@fontsource/ibm-plex-sans` e `@fontsource/ibm-plex-mono` (SIL Open Font License).
- ADR novo: 0077 (React Router). ADRs implementados: 0011 e 0066 (estrutura), 0012 (celular), 0014 (automático como padrão), 0050 (MUI e tema), 0062 a 0065 e 0067 (identidade visual), 0064 (fonte pela aplicação).
