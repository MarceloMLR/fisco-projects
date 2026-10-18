---
status: aceita (cantos substituídos pelo ADR-0067)
date: 2026-10-16
decision-makers: Marcelo Ribeiro
---

# ADR-0065: Forma sóbria e confortável, com status por ícone, cor e texto

## Contexto e problema

Com a direção sóbria (ADR-0062), as cores (ADR-0063) e a tipografia (ADR-0064), falta a forma dos componentes: cantos, separação, densidade, estilo de status e ícones. Marcelo gostou da opção sóbria, mas a achou compacta demais, e o status só com um ponto colorido obrigava a ler o texto para entender o significado.

## Fatores de decisão

- Status reconhecível de relance, sem depender só da cor (acessibilidade, WCAG).
- Conforto de leitura e bom aproveitamento da tela em listas e no Gantt.
- Distância do visual genérico (pílulas pastel, sombras em tudo).
- Coerência com a direção sóbria.

## Opções consideradas

- A. Sóbria e compacta, com status por ponto colorido
- A2. Sóbria e confortável, com status em etiqueta suave retangular, com ícone, cor e texto
- A3. Sóbria e confortável, com status em etiqueta com contorno, com ícone, cor e texto
- B. Arredondada e espaçosa, com cartões, sombras e pílulas
- C. Padrão do Material (MUI sem ajustes)

## Resultado da decisão

Opção escolhida: **A2. Sóbria e confortável, com etiqueta suave**.

Regras:

| Item | Definição |
| --- | --- |
| Cantos | Pequenos (cerca de 4 px) em botões, campos, cartões e etiquetas. Substituído pelo ADR-0067: cantos que crescem com a peça (3, 6, 10 e 12 px) |
| Separação | Bordas finas em vez de sombras; sombra só em menus, diálogos e no Gantt ao arrastar |
| Status | Etiqueta retangular suave (fundo levemente tingido) com ícone, cor e texto; nunca só cor |
| Densidade | Confortável por padrão; tabelas podem ser compactadas pelo controle de densidade |
| Ícones | Material Symbols no estilo contornado (outlined), do `@mui/icons-material` |
| Botões | Texto em minúsculas com inicial maiúscula, sem sombra |

### Consequências

- Bom, porque o status é entendido de relance e atende quem tem daltonismo.
- Bom, porque a tela respira sem perder a densidade de informação.
- Ruim, porque o tema do MUI precisa de ajustes (cantos, sombras, botões) para fugir do padrão Material.

### Confirmação

O design system no Claude Design define as etiquetas de status com ícone, cor e texto nos dois temas; a revisão de acessibilidade confirma que nenhuma informação depende só de cor.

## Prós e contras das opções

### A. Sóbria e compacta com ponto

- Bom, porque cabe mais informação.
- Ruim, porque ficou apertada e o status exige leitura.

### A2. Etiqueta suave com ícone

- Bom, porque o status salta aos olhos em listas longas.
- Ruim, porque coloca um pouco mais de cor na tela.

### A3. Etiqueta com contorno

- Bom, porque é mais leve.
- Ruim, porque destaca menos o status.

### B. Arredondada e espaçosa

- Bom, porque parece moderna.
- Ruim, porque é a estética de template e mostra cerca de metade das linhas.

### C. Padrão do Material

- Bom, porque é o mais rápido.
- Ruim, porque dá cara de Google e confunde etiquetas com botões.
