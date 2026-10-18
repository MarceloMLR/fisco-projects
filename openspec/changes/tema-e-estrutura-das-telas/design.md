# Design: tema e estrutura das telas (Fundação-5)

## Context

Motivação em proposal.md. O `web/` tem React 19, TypeScript estrito, react-i18next e uma página provisória sem estilo; o host serve o build. O design system FiscoProjects (versão 1792263038-205c, v3.1, de 17/10/2026) traz o `tokens.json`, o `README.md` com as regras e as versões de referência dos componentes (`bundle.css` e `bundle.js`); o canvas de telas mostra Meu trabalho, Projetos e o celular. Os valores desta change vêm desses arquivos.

## Goals / Non-Goals

**Goals:**

- Uma fonte só para as cores e medidas: a cópia do `tokens.json`. Tema do MUI e variáveis CSS saem dela.
- A estrutura (barra lateral, cabeçalho, celular) pronta para receber o sino, a conta, a Administração e Meus projetos sem mudar de forma.

**Non-Goals:**

- Componentes de conteúdo (tabela, etiqueta de status, faixa de fases): entram com a primeira tela que os usa.

## Decisions

### 1. Tokens

- `web/src/theme/tokens.json` é uma cópia exata do `project/tokens.json` do design system. Para atualizar, copia-se o arquivo de novo; o código não edita valores.
- `tokens.ts` lê a cópia, resolve os apelidos (`{bg-page}`) e expõe os valores de cada tema. `cssVariables.ts` gera as mesmas variáveis do `tokens.css` do design system (`--bg-page`, `--sidebar-bg`, `--space-6`, `--radius-lg` e as demais), para o tema claro e o escuro, e o `ThemeProvider` as injeta com `GlobalStyles`.
- Os componentes próprios usam essas variáveis pelo nome do design system (`var(--sidebar-bg)`), como pede o ADR-0050.

### 2. Tema do MUI

- `createTheme` com `cssVariables` e `colorSchemes.light` e `.dark`, seletor `[data-theme="%s"]`, o mesmo atributo do design system. O MUI aplica o atributo no `<html>`, e as variáveis do design system trocam junto.
- Paleta: `primary` = `action` (hover `action-hover`, texto `on-action`); `error` = `danger`; `success`, `warning`; `background.default` = `bg-page`, `paper` = `bg-surface`; `text` = `text`, `text-secondary`, `text-disabled`; `divider` = `border`; `action.hover` = `bg-hover`, `action.selected` = `bg-selected`.
- Forma e tipografia, conforme o "No código" do README do design system: `shape.borderRadius` 6 (10 em cartões, menus e diálogos); IBM Plex Sans 400 e 500, sem 600 ou 700; variantes `h1` (`page-title`), `h2` (`section-title`), `h3` (`card-title`), `body1` (`body`), `body2` (`body-long`), `caption` e mais `label`, `badge` e `code`; botões com `textTransform: 'none'` e `disableElevation`; `Paper` sem elevação, com borda `border`; sombras zeradas, exceto a do menu (`shadow-menu`) e a do diálogo (`shadow-dialog`).
- Base global igual à do design system: corpo em `bg-page` e `text`, 14/20; foco do teclado com contorno de 2px em `focus-ring` e 2px de afastamento; transições de 150ms `ease-out`; nenhuma transição com `prefers-reduced-motion`.
- O ponto de quebra do celular é o `md` do MUI (900px), o mesmo do design system.

### 3. Fontes

`@fontsource/ibm-plex-sans` e `@fontsource/ibm-plex-mono`, subconjunto latino (que cobre o português), pesos 400 e 500. O Vite empacota os arquivos `woff2` no build, servido pelo host (ADR-0064). Licença OFL-1.1 entra na lista permitida do `licenses`.

### 4. Tema automático

`ThemeProvider` com modo `system`: segue `prefers-color-scheme`. Sem seletor de tema até o menu da conta (Acesso-1) e o perfil (Acesso-5). O `index.html` declara `color-scheme: light dark`, para o fundo antes do React já seguir o sistema.

### 5. Estrutura (`web/src/shell`)

Valores das classes `fp-frame`, `fp-sidebar`, `fp-appbar`, `fp-topbar` e `fp-drawer` do `bundle.css` e do `fp-screens.css`:

- Computador: grade com a barra lateral (232px, `sidebar-bg`, borda à direita `sidebar-border`, rolagem própria) ocupando a altura toda; cabeçalho de 56px em `topbar-bg`, sem linha; conteúdo em `bg-page` com rolagem própria. A estrutura tem `100dvh` e não rola.
- Barra lateral: no topo, na altura do cabeçalho, o selo (28px, `radius-md`, `action`, ícone `ViewTimelineOutlined` de 18px) e o nome em 16/24 peso 500; itens de 38px, `radius-md`, peso 500, ícone de 20px em `sidebar-muted`; o atual em `sidebar-active-bg` com texto `sidebar-text-active`, ícone e filete de 3px em `sidebar-indicator`.
- Cabeçalho: trilha com os níveis anteriores em `text-secondary` como links e o último em `text` peso 500, separados por "/" em `text-disabled`. À direita, o espaço do sino e do avatar, preenchido nas changes deles.
- Celular (abaixo de 900px): barra superior de 56px com o botão de menu (44px, `MenuOutlined` de 24px) e o título da página em 16/24 peso 500, com linha `sidebar-border` embaixo; o menu é o `Drawer` temporário do MUI com 232px, `shadow-dialog`, fundo `overlay` e a barra lateral sem o nome, como no canvas. Fecha ao escolher um item.
- Página: cabeçalho da página com o título em `h1` (`page-title`, 24/32) e margens de 20px em cima e 24px nas laterais e embaixo (`fp-page`). No celular o título fica na barra superior, e o `h1` continua na página só para leitores de tela.
- Ícones: `@mui/icons-material`, variantes `…Outlined` (`HomeOutlined`, `FolderOutlined`, `ConfirmationNumberOutlined`).

### 6. Rotas (ADR-0077)

`createBrowserRouter` com a estrutura como rota pai e as páginas como filhas: `/` (Início, título "Meu trabalho", como no canvas), `/projects` e `/tickets`. Cada rota declara a chave de tradução do título no `handle`; a estrutura lê a rota atual para marcar o item da barra lateral, montar a trilha e o título do celular. Endereço desconhecido redireciona para `/`, sem tela própria de erro, que não está no canvas.

### 7. Testes

- Rodam no jsdom com `matchMedia` simulado (largura e tema do sistema), em `src/test`.
- "Cor escrita direto no código" lê o código-fonte com `import.meta.glob` e reprova cores em hexadecimal, `rgb()` e `hsl()` fora da cópia dos tokens.
- "Contraste mínimo" calcula a razão de contraste da WCAG nos pares de texto e fundo da estrutura, nos dois temas.
- Capturas de tela do computador e do celular, nos dois temas, conferidas com o Chromium do ambiente e enviadas no resumo; os testes de ponta a ponta continuam para depois.

## Risks / Trade-offs

- [A cópia do `tokens.json` pode ficar atrás do design system] → A versão de origem fica no `tokens.ts`; mudança no design system vira change que copia o arquivo de novo.
- [O título das páginas no canvas usa 26px (`fp-screens.css`), e o token `page-title` diz 24px] → Vale o token, já que nenhum valor fora dos tokens é usado; a diferença vai no resumo para Marcelo decidir.
- [Antes do React montar, o fundo é o padrão do navegador] → `color-scheme: light dark` deixa esse instante no tema do sistema.
- [MUI 9 e React Router 8 são versões recentes] → Usados só pelo que os seus guias documentam; os testes cobrem a estrutura.

## Migration Plan

Sem dados. Para desfazer, basta reverter o PR.

## Open Questions

Nenhuma.
