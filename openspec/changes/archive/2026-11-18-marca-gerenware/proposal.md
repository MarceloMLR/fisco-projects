## Why

Marcelo escolheu a marca GerenWare e aprovou o manual de identidade visual em 16/11/2026. Em 17/11/2026, pediu o rebranding do produto inteiro, que virou o grupo 17 do roadmap. A marca muda decisões aceitas (o azul corporativo do ADR-0063, o marinho da entrada do ADR-0067), e o manual vive fora do repositório. Antes de mexer em telas e código, a decisão precisa estar num ADR, e os arquivos da marca, no repositório.

## What Changes

- ADR-0159, "Marca GerenWare, com o caminho em G, grafite e esmeralda": o nome, o símbolo e a logo, as cores (ação em esmeralda, neutros grafite, concluído em esmeralda, "em andamento" em azul-céu, entrada em grafite, líder técnico em azul na conversa), a Figtree só no nome da logo, o canto de 3 px das etiquetas de status e o nome certo do conjunto de ícones.
- Os ADRs 0063, 0064, 0065, 0067 e 0069 ganham a indicação no status e a nota no item substituído, e o índice (`docs/adr/README.md`) ganha o ADR novo.
- `docs/marca/`: os arquivos do pacote da marca (SVG, PNG e ícones do navegador e do celular) e um `README.md` com o resumo dos valores do manual.
- `docs/discovery.md`: o ADR-0159 nas tabelas de referência, a nota no nome do produto e a pendência dos ícones retirada.
- `docs/roadmap.md`: a Marca-1 com o status, e o registro de que, a pedido de Marcelo em 17/11/2026, as changes do grupo 17 seguem em sequência, com a aprovação nos PRs.

## Capabilities

### New Capabilities

### Modified Capabilities

Nenhuma: esta change só registra a decisão e guarda os arquivos. As specs mudam nas changes que aplicam a marca (Marca-2 a Marca-8).

## Non-goals

- Mudar tokens, telas, textos ou código: o produto continua com o nome e o visual atuais até a Marca-2 e as seguintes.
- Mudar o conteúdo dos ADRs aceitos além do status e da nota de substituição (ADR-0001).
- Trocar o nome em `CLAUDE.md`, no README e nos guias (Marca-8).
- Registrar domínio ou marca.

## Impact

- Só documentação: `docs/adr/`, `docs/marca/`, `docs/discovery.md` e `docs/roadmap.md`. Pela verificação do PR, só as specs rodam.
- Decisões tomadas na proposta: as etiquetas de status ficam com canto de 3 px, como no manual (hoje o código usa 6 px, e a troca fica para a Marca-2); o líder técnico passa ao azul que era do responsável, porque a primeira ideia, ameixa, ficava quase igual ao rosa do solicitante (ADR-0104).
