## Context

**Backend.** A cor principal fica em `InstallationSettings.MainColor` (`#RRGGBB` ou nulo), no módulo `Administration`.

- O `TryNormalizeMainColor` recusa a cor sem 4,5:1 com o branco (`ContrastWithWhite`).
- O `UpdateInstallation` grava a cor junto com o nome, o fuso e as regras pelo `PUT /api/installation`.
- O `GET /api/installation`, sem sessão, devolve o nome, os logos e a cor.

**Frontend.** O `theme/brandColors.ts` deriva os tokens da marca da cor principal, em OKLCH (ADR-0082).

- `brandTokens`, `brandVariables` (também as variáveis do primário do MUI) e `brandStyleSheet`, com seletores que pesam 0,2,0.
- O `BrandColors` aplica a folha de estilo na rota raiz.
- O `favicon.ts` desenha o selo na cor.
- O `Sidebar` mostra o logo escuro quando `sidebarIsDark(scheme)` (só o tema escuro).
- O `MainColorField` usa o seletor do navegador e diz se o contraste passou (`hasContrast`).
- O `SettingsPage` tem a cor na seção Marca e a `BrandPreview` ao lado.
- O protótipo aprovado por Marcelo em 01/11/2026 tem as regras de cor, o seletor e a seção Cores, e serve de tela aprovada (ADR-0146).

## Goals / Non-Goals

**Goals:**

- Três cores opcionais, qualquer uma aceita, com o texto e os contornos calculados para o contraste AA nos dois temas.
- O seletor próprio, pelo ponteiro e pelo teclado, com os formatos HEX, RGB e HSL.
- A seção Cores com a prévia grande e a gravação própria.
- Sem nenhuma cor, os valores exatos do `tokens.json`.

**Non-Goals:**

- As cores do logo, o conta-gotas, as cores da conversa e as dos e-mails.
- Mudar o design system publicado.
- Cores na demonstração: o plano continua com as cores padrão.

## Decisions

### Domínio e banco

- **Entidade:** `InstallationSettings` ganha `SecondaryColor` e `SidebarColor` (texto de até 7, nulos).
- **Validação:** `TryNormalizeColor(value, out color)` só confere o formato e guarda em maiúsculas (nulo ou vazio volta ao padrão). O `ChangeColors(main, secondary, sidebar)` grava as três.
- **O que sai:** o `MinimumContrastWithWhite`, o `ContrastWithWhite`, o `TryNormalizeMainColor` e o `ChangeMainColor`.
- **Migração:** `InstallationColors`, com as duas colunas.
- **Auditoria:** automática, pelo contexto base, como a cor principal hoje.

### Casos de uso e API

- **`ChangeInstallationColors`:**
  - recebe `InstallationColors(Main, Secondary, Sidebar)`;
  - devolve os campos inválidos (`InstallationColorField`);
  - grava as três de uma vez, ou nada.
- **`UpdateInstallation`:** perde a cor (`InstallationChanges` e `InstallationField` sem `MainColor`).
- **`GetInstallation`:** devolve as três.
- **Rota nova:** `PUT /api/installation/colors` (`ChangeInstallationColors`, `SystemPolicies.Administrator`, `X-CSRF` como toda alteração).
  - Corpo `{ mainColor, secondaryColor, sidebarColor }`.
  - Responde 204, ou 400 com o problema em cada campo inválido.
- **Contratos:** `InstallationResponse` ganha `secondaryColor` e `sidebarColor`; `UpdateInstallationRequest` perde `mainColor`.

### Derivação (`brandColors.ts`, um lugar só)

**Escolha e tokens:**

- `BrandColorChoice = { main, secondary, sidebar }`, cada um `#RRGGBB` ou `null`.
- `brandTokens(choice)` devolve, por tema:
  - os tokens da lista atual;
  - `on-action`, `on-control-selected` e `bg-selected`;
  - a família `sidebar-*`: `bg`, `hover-bg`, `active-bg`, `text`, `text-active`, `muted`, `indicator`, `divider` e `border`;
  - a variável `--sidebar-title`, que só existe com a cor da barra lateral; sem ela, vale o `--text`.
- Sem nenhuma cor, os valores do `tokens.json`.
- `textOn(fundo)`: o branco quando ele tem 4,5:1 sobre o fundo; senão o `text` do design system, o que tiver mais contraste.

**Principal, no tema claro:**

- `action` = a cor;
- `on-action` = `textOn(action)`;
- `action-hover` e `action-pressed` = passos de luminosidade, com o `on-action` mantendo 4,5:1;
- `action-text` até 4,5:1 sobre `bg-surface` e `bg-page`;
- `control-selected` até 3:1 sobre `bg-surface`, com `on-control-selected` = `textOn`;
- `focus-ring` até 3:1 sobre `bg-page`;
- `avatar-owner` mais escuro até o `on-avatar-role` ter 4,5:1;
- `sidebar-active-bg` e `sidebar-text-active` com o matiz da cor, o texto até 4,5:1;
- `sidebar-indicator` até 3:1 sobre o fundo da barra.

**Principal, no tema escuro:**

- `action` = a cor, mais clara só até 3:1 com `bg-page`;
- `action-text` até 4,5:1 sobre as superfícies escuras;
- os demais da lista com a claridade do design system e o matiz e a saturação da cor, como no ADR-0082.

**Secundária:**

- `bg-selected` com a claridade do design system e o matiz da cor, com a razão de saturação limitada a 1,6;
- `brand-panel-bg` recolorido;
- `brand-panel-bg-2`: a cor, no claro, ou o padrão recolorido, no escuro, mais escuro até o texto branco ter 4,5:1;
- `brand-panel-muted` até 4,5:1.
- Sem a secundária, o painel sai da principal, como hoje, e `bg-selected` fica a do design system.

**Barra lateral:**

- **Fundo:**
  - no claro, a cor;
  - no escuro, OKLCH com L 0,215, croma até 0,04 e o matiz da cor;
  - "escura" quando a luminância é menor que 0,18.
- **Textos:** `sidebar-text`, `sidebar-muted` e `--sidebar-title` até 4,5:1 sobre o fundo.
- **Ponteiro e divisórias:** `sidebar-hover-bg`, `sidebar-divider` e `sidebar-border` como passos de luminosidade do fundo.
- **Item ativo:** `sidebar-active-bg` é a mistura do fundo com a principal, e `sidebar-text-active` fica até 4,5:1 sobre ele.
- **Filete:** `sidebar-indicator` até 3:1 sobre o fundo.

**O que mais muda:**

- **Avisos dos ajustes:** `colorNotes(choice)` diz, por campo:
  - o texto escuro nos botões;
  - os links mais escuros;
  - o botão mais claro no escuro;
  - o painel mais escuro;
  - os textos claros ou escuros na barra;
  - o filete com outro tom.
- **Barra escura:** `sidebarIsDark(choice, scheme)` usa o fundo efetivo; o `Sidebar` e as prévias o usam para o logo escuro.
- **MUI:** `brandVariables` passa a pôr também `--mui-palette-primary-contrastText`.
- **Texto que não é sobre a cor principal:** quem usa `--on-action` sobre outra cor passa para o token certo. Por exemplo, o avatar com o tom do papel passa ao `--on-avatar-role`.
- **Cores de status:** nenhum token de status entra na lista.

### Leitura dos formatos (`theme/colorFormats.ts`)

- `parseColor(texto)` aceita:
  - `#RRGGBB`, `RRGGBB`, `#RGB` e `RGB`;
  - `rgb(r, g, b)`, `rgba(...)` sem o alfa e `r, g, b`;
  - `hsl(h, s%, l%)`, `hsla(...)` sem o alfa.
- Devolve `#RRGGBB`, ou `undefined`.
- `formatColor(hex, 'hex' | 'rgb' | 'hsl')` escreve a cor no formato pedido.
- As conversões HSV e HSL ficam ao lado.

### Seletor (`components/ColorPicker.tsx`)

- **Onde abre:**
  - no computador, um `Popover` do MUI preso à amostra;
  - em telas estreitas (até 600px, como o protótipo), uma folha presa ao pé da tela, sobre o fundo escurecido, com as margens da área segura.
- **Área de cor:** `role="slider"`, com o nome "Saturação e brilho" e o `aria-valuetext` dos dois. As setas mudam 1%, e com Shift 10%.
- **Barra de matiz:** `role="slider"`. As setas mudam 1°, e com Shift 10°.
- **Ponteiro:** os eventos de ponteiro com captura.
- **Formato e valor:** "Atual" e "Nova", o formato no `SegmentedControl` e o campo do valor pelo `parseColor`.
- **Fechar:** "Cancelar" e Esc devolvem a cor de antes; "Usar esta cor" a entrega, e o foco volta à amostra.
- **Movimento:** pelo `motion.ts`.

### Campo de cor (`admin/ColorField.tsx`, no lugar do `MainColorField`)

- A amostra (o botão que abre o seletor), o campo de valor, "Voltar ao padrão", a linha de onde a cor aparece e os avisos (`colorNotes`).
- **Avisos:** cada um com o ícone de informação e o tom `info`. Sem ajuste, a confirmação no tom `success`.
- **Valor:** aceita qualquer formato e guarda em `#RRGGBB` ao sair do campo ou com Enter. O valor que não é cor mostra o erro, com os exemplos, e não muda a cor.
- **Prévia:** muda enquanto se digita uma cor válida.

### Seção Cores (`admin/ColorsPage.tsx`, `/admin/colors`)

- **Na Administração:** `Page` com o título "Cores" e o subtítulo, no item novo da coluna (`PaletteOutlined`) logo depois de Configurações gerais.
- **Formulário:** com o `Form` e a barra de ações com "Descartar" e "Salvar alterações", como Configurações gerais; o envio pelo `PUT /api/installation/colors`, invalidando a marca e a auditoria, com o aviso pelo `useNotify`.
- **Layout:** os campos à esquerda (até 380px) e a prévia (`ColorsPreview`) à direita, presa enquanto a área rola. Abaixo de 900px, a prévia vai para baixo dos campos.
- **Prévia:**
  - redesenha, em pequeno e só com os tokens, a barra lateral, o cabeçalho, as abas, os botões, a lista com a linha selecionada, a menção e o painel da entrada;
  - o Claro e o Escuro dela, no `SegmentedControl`, não mudam o tema da pessoa;
  - as variáveis do tema escolhido e da cor sendo editada vão no próprio elemento;
  - as abas que não cabem saem, sem rolagem.

### Configurações gerais

- O formulário perde a cor, e o pedido vai sem `mainColor`.
- A `BrandPreview` recebe as cores salvas (`brandVariables(choice, scheme)`).
- O logo da prévia segue o `sidebarIsDark(choice, scheme)`.

### O que mais muda na tela

- O `BrandColors`, o `favicon.ts` (`action` e `on-action` do tema claro) e o `Sidebar` leem as três cores da marca.
- A Auditoria descreve "Cor secundária" e "Cor da barra lateral" (`auditText.ts`).

## Risks / Trade-offs

- **[Texto escuro no botão de uma cor clara surpreende]:** o aviso embaixo do campo e a prévia mostram antes de salvar.
- **[Um `--on-action` usado sobre outra cor ficaria escuro]:** os usos são conferidos um a um, e cada um passa ao token certo, com teste.
- **[Muitas combinações de três cores]:** testes de contraste em lote (claras, escuras, branco, preto, saturadas, nos dois temas) para todos os pares.
- **[Arrastar na área de cor não roda no jsdom]:** os testes de tela usam o teclado e o campo de valor; o arraste é conferido no navegador.
- **[Contrato muda no `PUT /api/installation`]:** só as telas o usam; os tipos são gerados de novo, e os testes da API passam a mandar o corpo novo.
