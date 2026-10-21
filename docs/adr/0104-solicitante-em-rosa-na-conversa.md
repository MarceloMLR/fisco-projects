---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0104: Solicitante em rosa na conversa do chamado

Substitui a cor do solicitante do ADR-0069 (cinza neutro). As outras cores por papel continuam as do ADR-0069.

## Contexto e problema

Pelo ADR-0069, cada papel da conversa do chamado tem a sua cor, menos o solicitante, que fica em cinza neutro. Ao rever a conversa, Marcelo notou que todos os papéis têm uma cor de destaque e o solicitante parece apagado, e pediu uma cor própria para ele. Qual cor, e o que acontece com quem hoje divide o cinza com o solicitante (o administrador fora do projeto e quem não tem papel nele)?

## Fatores de decisão

- Uma cor que não se confunda com as outras cinco (azul, violeta, verde-azulado, ocre e oliva) nem com o vermelho de erro e de prazo vencido.
- O mesmo peso das outras: a mesma claridade e intensidade, com o contraste de 4,5:1 do selo, do nome e das iniciais do avatar nos dois temas.
- A cor nunca é a única informação: o selo diz o papel (ADR-0065).

## Opções consideradas

1. Magenta.
2. Rosa.
3. Rosa-avermelhado.

## Resultado da decisão

Opção 2, escolhida por Marcelo entre as três prévias. O rosa sai do violeta do gerente, com a mesma claridade e intensidade e só o tom trocado (OKLCH, matiz 350), e fica longe dos outros papéis e do vermelho de erro. O design system ganha os tokens `role-requester-bg`, `role-requester-text` e `avatar-requester`, e `msg-requester-bg` e `msg-requester-border` passam a ser rosa. O selo, o nome, o avatar e o cartão do solicitante seguem a cor, também na descrição do chamado e no painel "Pessoas".

O administrador fora do projeto e quem não tem papel nele continuam no cinza neutro, com os tokens novos `msg-neutral-bg` e `msg-neutral-border` (os valores de antes do solicitante), o selo neutro e o avatar neutro.

### Consequências

- Boa, porque o solicitante passa a ser achado de relance, como os outros papéis.
- Boa, porque quem não tem papel no chamado continua discreto.
- Neutra, porque são seis cores de papel; o selo continua dizendo o papel.
