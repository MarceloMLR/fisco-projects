## Context

A marca da instalação chega por `GET /api/installation` (nome, logos e cores). Sem logo, `Sidebar`, `SignInPage` e `ColorsPreview` desenham um selo: o ícone `ViewTimelineOutlined` sobre `--action`. A aba do navegador recebe, depois de a marca chegar, o logo ou o mesmo selo desenhado em SVG (`favicon.ts`, ADR-0108). O `index.html` não tem ícone. Os arquivos da marca estão em `docs/marca/` (Marca-1).

## Goals / Non-Goals

**Goals:** a logo do manual nas telas sem redesenho; nenhum efeito para quem enviou logo; ícone de aba e de celular desde o HTML.

**Non-Goals:** nome padrão e textos (Marca-4); ícone do Teams; manifesto por instalação.

## Decisions

- **Um componente, `ProductMark`.** Recebe o nome da instalação, se o fundo é escuro e a altura. Com o nome do produto (`productName`, em `brand.ts`), mostra a logo horizontal, com o nome no texto alternativo; com outro nome, o símbolo, decorativo. Quem chama decide se escreve o nome ao lado pelo `showsProductLogo`. Alternativa descartada: perguntar à API se o nome é o padrão; o frontend só precisa comparar com o nome do produto.
- **Arquivos como módulos do Vite.** Os três SVG usados nas telas ficam em `web/src/assets/brand/` e entram pelo `import`, com o nome com hash em `/assets`. Os ícones do navegador ficam em `web/public/`, que o Vite copia para a raiz do build com os nomes padrão da web. As duas pastas são cópias de `docs/marca/`; o `brand.test.tsx` compara os bytes.
- **Ícone da aba no HTML.** O `index.html` aponta `/favicon.svg`; o `faviconOf` devolve esse endereço sem logo, e o `showFavicon` troca o `href` pelo logo e tira o `type`, porque o logo pode ser PNG. Some o selo desenhado (`badgeIcon`); os traços do ícone do cronograma ficam, porque o pacote do Teams ainda os usa.
- **Sem `theme-color` no HTML.** A regra de cores reprova cor escrita no `index.html`; o manifesto já traz a cor do tema.
- **Tamanhos.** 28 px na barra lateral (a logo horizontal fica com 142 px de largura, dentro dos 232 px), 32 px na entrada e 24 px na prévia, todos acima dos mínimos do manual (24 px para a horizontal, 16 px para o símbolo).

## Risks / Trade-offs

- Cópias dos arquivos em três lugares → o teste das cópias falha quando uma muda sozinha.
- O manifesto tem o nome GerenWare fixo → aceito no ADR-0160; um manifesto por instalação pediria uma rota sem sessão a mais.
