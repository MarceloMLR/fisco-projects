---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0050: Material UI (MUI) como biblioteca de componentes, só com pacotes gratuitos

## Contexto e problema

O frontend é React com TypeScript (ADR-0049). O design system será criado no Claude Design, com temas claro e escuro (ADR-0014), e a marca exibida (nome, logo, cores) precisa ser configurável por instalação (ADR-0003). Marcelo já usa o MUI em projetos internos, com um padrão conhecido: tema central (`theme.ts`) com a paleta da marca, Data Grid encapsulada num componente próprio com estilo e pt-BR, ícones do MUI e telas próprias com variáveis CSS seguindo a mesma paleta. Qual biblioteca de componentes usar?

## Fatores de decisão

- Conseguir dar a identidade visual da empresa (e de futuros compradores).
- Experiência prévia de Marcelo e padrões já testados.
- Componentes prontos para listas, formulários e datas em português.
- Custo de licença.

## Opções consideradas

- A. shadcn/ui (Radix com Tailwind)
- B. Mantine
- C. MUI (Material UI)
- D. Fluent UI (Microsoft)

## Resultado da decisão

Opção escolhida: **C. MUI**, porque Marcelo já domina a biblioteca e tem um padrão funcionando, a Data Grid e os seletores de data gratuitos cobrem listas e formulários em pt-BR, e a identidade visual é resolvida pelo tema.

Regras:

- Pacotes: `@mui/material`, `@mui/icons-material`, MUI X Data Grid e MUI X Date Pickers, somente nas versões gratuitas (MIT). Um pacote pago (Pro ou Premium) só entra com um ADR.
- Um tema central com os tokens do design system, no modo de variáveis CSS do MUI, cobrindo os temas claro e escuro (ADR-0014).
- Cor principal e logo vindos da configuração da instalação (ADR-0003, ADR-0009).
- Wrappers próprios para componentes repetidos (por exemplo, a tabela padrão com estilo e localização pt-BR).
- Telas próprias, quando necessárias, usam as mesmas variáveis CSS do tema.
- Complementos: React Hook Form com Zod (formulários), Tiptap (editor dos chamados com menções, ADR-0026) e react-i18next (textos em arquivos de tradução, ADR-0013).

### Consequências

- Bom, porque aproveita a experiência e os padrões que Marcelo já usa.
- Bom, porque tabela e seletores de data gratuitos já trazem o português.
- Bom, porque a marca é trocada por configuração, sem mexer em componentes.
- Ruim, porque o visual do Material Design precisa ser sobrescrito no tema para ganhar identidade própria.
- Ruim, porque recursos avançados da Data Grid são pagos e ficam de fora.

### Confirmação

O `package.json` não tem pacotes Pro ou Premium do MUI; existe um único tema central; componentes repetidos usam os wrappers; os testes visuais cobrem os temas claro e escuro.

## Prós e contras das opções

### A. shadcn/ui

- Bom, porque o visual é totalmente controlável e os componentes ficam no projeto.
- Ruim, porque exige montar tabelas e datas com bibliotecas complementares e não aproveita a experiência de Marcelo.

### B. Mantine

- Bom, porque tem muitos componentes prontos e temas claro e escuro.
- Ruim, porque personalizar a fundo dá mais trabalho e não há experiência prévia.

### C. MUI

- Bom, porque é maduro, conhecido por Marcelo e completo nas versões gratuitas.
- Ruim, porque carrega o visual do Material Design, que precisa ser ajustado no tema.

### D. Fluent UI

- Bom, porque tem o visual do Microsoft 365.
- Ruim, porque a identidade do produto ficaria parecida com a da Microsoft e há menos componentes.
