---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0101: Cores das mensagens dos chamados configuráveis por papel, com os tons calculados

Complementa o ADR-0069 (conversa do chamado, com as cores de cada papel) e o ADR-0100 (cor secundária).

## Contexto e problema

Pelo ADR-0069, cada mensagem da conversa do chamado tem a cor do papel de quem fala: o solicitante em cinza neutro, o responsável no azul da marca, o gerente em violeta, o líder técnico em verde-azulado, o desenvolvimento em ocre e o funcional em oliva, com o fundo e o contorno do cartão, o selo do papel (que também colore o nome) e o avatar. Marcelo pediu que a cor do cartão das respostas seja configurável. São quatro ou cinco tons por papel, em dois temas: o que o administrador escolhe, e como os tons ficam legíveis?

## Fatores de decisão

- Uma escolha simples: uma cor por papel.
- Os cartões continuam claros no tema claro e escuros no escuro, como o design system, e o texto sobre eles legível.
- O selo e o nome têm 4,5:1, e as iniciais brancas do avatar também.
- Voltar ao padrão a qualquer momento.

## Opções consideradas

- A. Uma cor por papel, com os tons do cartão, do selo e do avatar calculados dela
- B. Cada tom (fundo, contorno, selo, texto, avatar) escolhido à mão, por tema
- C. Só o matiz por papel, sem a força da cor

## Resultado da decisão

Opção escolhida: **A**, porque uma escolha por papel basta e o cálculo garante o contraste nos dois temas.

Regras:

- Seis cores opcionais nas Configurações gerais, uma por papel da conversa (solicitante, responsável, gerente do projeto, líder técnico, desenvolvimento e funcional), cada uma em hexadecimal, qualquer cor, com "Restaurar padrão". Sem cor, o papel usa os tokens do design system.
- Cada tom (fundo e contorno do cartão, fundo e texto do selo) mantém a claridade do design system e ganha o matiz e a força da cor escolhida, medidos contra o avatar do papel (o do responsável, para o solicitante, que é neutro). O texto do selo e do nome é empurrado até 4,5:1 sobre o selo, o cartão e a superfície. O avatar é a própria cor no tema claro, escurecida até as iniciais brancas terem 4,5:1, e um tom dela no escuro.
- O solicitante muda só o cartão; o selo e o avatar dele continuam neutros, como no ADR-0069.
- A cor do responsável vale sobre a cor principal no avatar do responsável.
- As cores vêm na marca da instalação (`GET /api/installation`), lida por todos, e a auditoria registra cada uma.
- A tela mostra uma prévia de uma mensagem de cada papel antes de salvar; o seletor de cores do ADR-0100 vale aqui também.

### Consequências

- Bom, porque a empresa ajusta as cores da conversa sem perder a legibilidade.
- Bom, porque sem cores nada muda.
- Ruim, porque a cor escolhida aparece suavizada nos cartões, para o texto continuar legível.

### Confirmação

Testes cobrindo: sem cores, nenhum token muda; com qualquer cor, em todos os papéis e nos dois temas, o selo e o nome têm 4,5:1 e as iniciais do avatar também, e os cartões continuam claros no tema claro e escuros no escuro; cor fora do formato recusada pela API; a prévia e a conversa mudam com a cor.

## Prós e contras das opções

### A. Uma cor por papel, tons calculados

- Bom, porque é simples e sempre legível.
- Ruim, porque o tom exato do cartão não é escolhido.

### B. Cada tom à mão

- Bom, porque dá controle total.
- Ruim, porque são dezenas de cores e o contraste fica a cargo de quem escolhe.

### C. Só o matiz

- Bom, porque é ainda mais simples.
- Ruim, porque ignora uma cor mais suave ou mais forte que a empresa queira.
