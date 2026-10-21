---
status: aceita (cor do solicitante substituída pelo ADR-0104)
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0069: Conversa do chamado com cores por papel, rolagem própria e interações antigas recolhidas

Complementa o ADR-0026 e substitui, nele, o "arrastando e soltando" dos anexos na abertura do chamado.

## Contexto e problema

O ADR-0026 definiu o que a conversa do chamado contém (mensagens visíveis a todos que veem o chamado, anexos e menções), mas não como ela é lida. Um chamado pode passar de 50 interações antes de fechar. Na revisão das telas, Marcelo apontou que não ficava claro quem era quem nas respostas: a mensagem do responsável e a de um desenvolvedor tinham a mesma cor. Pediu também que a conversa tenha a sua própria barra de rolagem, sem rolar a página inteira, e gostou de recolher as interações antigas. Na abertura, pediu só o botão "Escolher arquivos", sem área de arrastar e soltar, para sobrar espaço à descrição. Como a conversa é apresentada?

## Fatores de decisão

- Saber de relance quem fala, mesmo em conversas longas.
- Cabeçalho do chamado e caixa de resposta sempre à vista.
- Acessibilidade: cor nunca é a única informação (ADR-0065).

## Opções consideradas

- A. Lista única, no mesmo estilo para todos
- B. Duas colunas (solicitante à esquerda, equipe à direita), com uma cor só para a equipe
- C. Duas colunas, com uma cor por papel

## Resultado da decisão

Opção escolhida: **C. Duas colunas, com uma cor por papel**, aprovada por Marcelo na revisão do design.

Regras:

| Item | Definição |
| --- | --- |
| Posição | Solicitante à esquerda; todas as outras pessoas à direita |
| Cores por papel | Solicitante em cinza; responsável pelo chamado no azul da marca, mesmo sendo de um dos times; gerente do projeto em violeta; líder técnico em verde-azulado; time de Desenvolvimento em ocre; time Funcional em oliva. Valores no design system, com contraste AA nos dois temas |
| Identificação | Cada mensagem traz nome, selo com o papel e "(você)" na própria pessoa; o painel "Pessoas" do chamado repete as cores nos avatares e serve de legenda |
| Rolagem | A conversa rola por dentro, entre o cabeçalho do chamado e a caixa de resposta, que fica fixa embaixo; a página não rola (ADR-0066) |
| Interações antigas | Acima de 10 interações, as antigas ficam recolhidas em "Mostrar N interações anteriores", com o número de mensagens e de eventos |
| Organização | Separador por dia; filtro "Tudo", "Mensagens" ou "Histórico"; eventos do sistema numa linha centralizada e discreta |
| Anexos | Todos os anexos da conversa reunidos no painel lateral |
| Abertura | Anexos só pelo botão "Escolher arquivos", que aceita vários arquivos de uma vez, sem área de arrastar e soltar |

### Consequências

- Bom, porque uma conversa de 50 interações fica legível de relance.
- Bom, porque cabeçalho, prazo e resposta continuam à vista enquanto se lê o histórico.
- Ruim, porque seis cores de papel precisam passar no contraste AA nos dois temas e não podem ser confundidas com as cores de status; o selo com o papel garante que a cor não é a única informação.
- Neutro, porque a cor de uma pessoa muda se ela passar a ser a responsável.

### Confirmação

Testes de interface cobrindo: a mensagem do solicitante fica à esquerda e as demais à direita; a cor e o selo correspondem ao papel de quem escreveu e ao responsável atual; acima de 10 interações as antigas aparecem recolhidas; a conversa rola sem rolar a página; a abertura aceita vários arquivos no mesmo "Escolher arquivos". O design system registra os tokens `msg-*`, `role-*` e `avatar-*` com contraste verificado.

## Prós e contras das opções

### A. Lista única

- Bom, porque é a mais simples.
- Ruim, porque não mostra quem é quem.

### B. Duas colunas com uma cor

- Bom, porque separa o solicitante da equipe.
- Ruim, porque responsável, desenvolvedores e gerente ficam iguais.

### C. Duas colunas com cor por papel

- Bom, porque cada papel é reconhecido de relance.
- Ruim, porque exige mais cuidado com cores e contraste.
