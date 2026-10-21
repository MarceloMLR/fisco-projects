## Context

O `InstallationRoot`, em volta de todas as telas, já põe o nome da instalação no título da aba. A marca (`GET /api/installation`) traz o endereço do logo e a cor principal; a barra lateral mostra o logo ou, sem ele, o selo (o ícone da linha do tempo sobre `action`, 28px, cantos `radius-md`).

## Goals / Non-Goals

**Goals:**
- O logo da instalação na aba; sem logo, o selo.

**Non-Goals:**
- Ícones de tamanhos diferentes, manifesto ou ícone para a tela inicial do celular.

## Decisions

- **`favicon.ts`**: `badgeIcon(cor principal)` desenha o selo em SVG (a caixa de 24 com os cantos proporcionais ao `radius-md` do selo de 28px, o ícone a 18/28 do tamanho) com `action` da cor principal e `on-action`, como endereço `data:`; `faviconOf(logo, cor)` escolhe; `showFavicon` põe o endereço no `<link rel="icon">`, criando-o se não existe.
- **`InstallationRoot`**: depois de a marca chegar, põe o ícone, e de novo quando o logo ou a cor muda.

## Risks / Trade-offs

- [Logo largo fica pequeno na aba] → o navegador reduz o logo ao quadrado; quem quiser outro ícone pode enviar um logo quadrado.
