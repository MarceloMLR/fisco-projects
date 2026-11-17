## Why

O produto tem logo desde o ADR-0159, mas as telas ainda mostram o selo com o ícone do cronograma onde a instalação não enviou logo, e a página não traz ícone de aba nem de celular antes de a marca chegar. Esta change põe a logo GerenWare nesses lugares e deixa a página de entrada como no manual.

## What Changes

- Sem logo da instalação, a barra lateral, o painel da página de entrada e a prévia das cores mostram a marca do produto no lugar do selo: a logo GerenWare inteira quando a instalação tem o nome do produto, e o símbolo ao lado do nome quando tem outro nome (ADR-0160). Na barra escura e no painel da entrada, a logo para fundo escuro.
- A aba do navegador mostra o símbolo da GerenWare desde o HTML e, quando a marca chega com um logo, o logo da instalação. O selo desenhado na cor principal deixa de existir.
- Ícones para a tela do celular e manifesto da marca, servidos na raiz do site.
- Página de entrada com a frase e o texto de apoio do manual.
- Os arquivos usados pelas telas são cópias de `docs/marca/`, conferidas por um teste.
- **BREAKING** para quem não tem logo e escolheu uma cor principal: o selo na cor principal dá lugar ao símbolo, que tem as cores da marca.

## Capabilities

### New Capabilities

### Modified Capabilities

- `configuracao-da-instalacao`: a marca do produto sem logo da instalação, o ícone da aba e os requisitos que citavam o selo.
- `estrutura-das-telas`: o topo da barra lateral.

## Non-goals

- Trocar o nome padrão da instalação e os textos das telas para GerenWare (Marca-4); até lá, a instalação sem nome próprio mostra o símbolo ao lado de "FiscoProjects".
- O ícone do aplicativo do Teams (Marca-4).
- Um ícone de aba ou um manifesto por instalação.

## Impact

- `web/`: `ProductMark` e `brand.ts` em `src/installation`, `Sidebar`, `SignInPage`, `ColorsPreview`, `favicon.ts`, `index.html`, `public/` (novo), `src/assets/brand/` (novo) e o texto da entrada em `pt-BR.json`.
- ADR-0160 e a nota no ADR-0108. O `tokens.json` muda só a nota de uso de `brand-panel-accent`, igual no design system.
- Nenhuma mudança na API nem no banco: os arquivos de `public/` saem no build e a API já serve os arquivos estáticos.
