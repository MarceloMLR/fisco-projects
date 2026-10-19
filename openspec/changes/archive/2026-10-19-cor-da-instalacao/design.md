# Design: cor principal da instalação (Acesso-8)

## Context

- **Variáveis CSS:** os tokens viram variáveis no `AppThemeProvider` (`cssVariables()` de `tokens.ts`):
  - `:root, [data-theme="light"]` para o claro e `[data-theme="dark"]` para o escuro;
  - o atributo também é usado em elementos de dentro da tela, como as miniaturas de tema de Meu perfil.
- **MUI:** gera as próprias variáveis (`--mui-palette-primary-main` e seus canais) a partir do `palette.primary` do `theme.ts`, com os valores de `action`.
- **Regra do projeto:** nenhuma cor escrita direto no código (`source-rules.test.ts`); todas vêm do `tokens.json`.
- **A marca:** a Acesso-6 criou `GET /api/installation` (nome e logo, sem sessão) e a rota raiz `InstallationRoot`, que existe em todas as telas, inclusive na entrada. A Acesso-7 levou o formulário inteiro para `UpdateInstallation`.
- **Canvas:** na seção Marca, a "Cor principal" tem uma amostra (`fp-swatch`), o campo hexadecimal de 130 px, o botão ghost "Voltar ao padrão" e, embaixo, o selo "Contraste aprovado".

## Goals / Non-Goals

**Goals:**

- A conta das cores num lugar só (`web/src/theme/brandColors.ts`), testável sem tela.
- A cor padrão devolve exatamente o `tokens.json` (ADR-0082).

**Non-Goals:**

- Guardar no servidor os tokens derivados; ele guarda só a cor.

## Decisions

### 1. A cor no servidor: hexadecimal ou nada, conferido com o branco

- `InstallationSettings.MainColor`, texto `#RRGGBB` em maiúsculas, `null` para a cor padrão. A migração `InstallationMainColor` cria a coluna vazia.
- `ChangeMainColor` aceita `null`, ou `#RRGGBB` com contraste de pelo menos 4,5:1 com o branco (luminância relativa da WCAG 2.x).
  - A conta é curta e fica no domínio. É a mesma regra da tela, que avisa antes.
  - O servidor é quem decide (ADR-0082).
- `UpdateInstallation` recebe a cor junto com o nome e o fuso e devolve `InstallationField.MainColor` quando ela é inválida.
- `GET /api/installation` passa a trazer `mainColor` (`null` para a padrão). É da marca, porque a entrada usa a cor no painel.

### 2. Derivação no frontend (ADR-0082)

`brandColors.ts` tem:

- **Conversão:** hexadecimal ↔ sRGB linear ↔ OKLab ↔ OKLCH, com as matrizes publicadas do OKLab. Uma cor fora do sRGB tem a saturação reduzida até caber.
- **Contraste:** `contrast(a, b)`, a razão da WCAG com a luminância relativa, para as cores opacas.
- **Derivação:** `brandTokens(mainColor)` devolve, para o claro e o escuro, o valor de cada token da lista do design system.
  - A referência é a cor padrão (`action` do claro no `tokens.json`).
  - Cada token parte do seu valor padrão, com o matiz deslocado pela diferença de matiz e a saturação multiplicada pela razão de saturação.
  - Mudam a luminosidade só os tokens que são a cor no tema claro (`action`, `action-hover`, `action-pressed`, `action-text`, `control-selected`, `focus-ring`, `sidebar-indicator`, `avatar-owner` e `brand-panel-bg-2`). Eles recebem a diferença de luminosidade para a cor padrão.
  - Os tokens translúcidos e brancos do painel (`brand-panel-text`, `brand-panel-shape` e `brand-panel-fill`) não mudam.
- **Ajuste de contraste:** depois de derivar, os pares do design system têm a luminosidade empurrada até o mínimo, para longe do fundo, em passos de 0,01:
  - `on-action` sobre `action`, `action-hover` e `action-pressed` (4,5);
  - `action-text` sobre `bg-surface`, `bg-page` e `bg-subtle` (4,5);
  - `sidebar-text-active` sobre `sidebar-active-bg` (4,5);
  - `control-selected` e `focus-ring` sobre `bg-surface` (3);
  - iniciais brancas sobre `avatar-owner` (4,5);
  - `brand-panel-muted` sobre `brand-panel-bg` (4,5).

  A cor escolhida no tema claro não é ajustada: ela passa pela regra da decisão 1.
- **Conferência para a tela:** `checkContrast(mainColor)` devolve se a cor é aceita, com a mesma regra do servidor.
- **Folha de estilo:** `brandStyleSheet(mainColor)` gera as declarações dos dois temas, mais `--mui-palette-primary-main`, `--mui-palette-primary-dark` e `--mui-palette-primary-mainChannel`. Os seletores têm especificidade (0,2,0) para valer sobre os do tema, qualquer que seja a ordem:
  - `:root:not([data-theme="dark"]), [data-theme="light"][data-theme="light"]` para o claro;
  - `[data-theme="dark"][data-theme="dark"]` para o escuro.

### 3. Aplicar a cor em toda a aplicação

- `BrandColors` (na `InstallationRoot`) lê a marca e, com cor, desenha um `GlobalStyles` com a folha da decisão 2; sem cor, não desenha nada.
- A página de entrada também recebe a cor, porque está dentro da rota raiz.
- Até a marca chegar, vale a cor padrão. Na entrada, o painel pode passar do azul padrão para a cor da instalação na abertura.

### 4. Cor nas Configurações gerais

- **Campo:** a cor entra no formulário (Zod): vazio ou `#RRGGBB`, aceitando a digitação sem `#` e em minúsculas, guardado em maiúsculas.
  - A amostra mostra a cor digitada; com a cor padrão, mostra a do token `action` (claro).
  - "Voltar ao padrão" limpa o campo (cor padrão).
- **Contraste:** abaixo do campo fica o selo `DueDate` do canvas, com o ícone, a cor e o texto do status (ADR-0065):
  - "Contraste aprovado" (sucesso);
  - "Contraste insuficiente" (perigo), que também bloqueia Salvar com o motivo no campo.
- **Prévia:** recebe as variáveis da cor digitada como estilo do próprio cartão, no tema da tela (`useColorScheme`), e mostra a cor antes de salvar sem mexer no resto da aplicação.
- **Auditoria:** "Cor principal" na tela, com o valor vazio mostrado como "—".

## Risks / Trade-offs

- **[Painel da entrada na cor padrão por um instante]** Até a marca chegar, a entrada usa o azul padrão. → É uma resposta pequena. Guardar no navegador ficou fora (ADR-0080).
- **[Cor vibrante fora do sRGB]** Algumas cores derivadas no escuro saem do sRGB. → A saturação é reduzida até caber, e o teste confere que todo token derivado é um hexadecimal válido.
- **[Duas implementações da regra de contraste]** O servidor (C#) e a tela (TypeScript) conferem o contraste com o branco. → É a fórmula da WCAG, de uma linha, com os mesmos exemplos nos testes dos dois lados. A derivação, que é a parte grande, fica só no frontend.
- **[Componentes do MUI com cor própria]** Alguns componentes do MUI usam `palette.primary`. → A folha também troca as variáveis `--mui-palette-primary-*`.

## Migration Plan

- A migração `InstallationMainColor` acrescenta a coluna vazia: as instalações continuam na cor padrão.
- Para voltar atrás, reverta o PR antes de publicar.
