---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0025: Prazos dos chamados em dias úteis, com pausa ao aguardar o solicitante e prazo de primeira resposta

## Contexto e problema

No MVP, o prazo do chamado vem da prioridade (baixa 10 dias, média 5, alta 2, crítica 1). Pelo ADR-0003, os valores precisam ser configuráveis. Falta decidir como o prazo é contado, se ele para enquanto o chamado espera o solicitante e quais prazos existem.

## Fatores de decisão

- Justiça com quem atende: o tempo esperando o solicitante não deve contar.
- Reaproveitar o calendário da instalação (ADR-0018).
- Complexidade proporcional à v1.
- Resposta rápida ao solicitante, mesmo antes da solução.

## Opções consideradas

- A. Dias corridos a partir da abertura, sem pausa
- B. Dias úteis com o calendário da instalação, com pausa enquanto Aguardando solicitante
- C. Horas úteis, com horário de expediente configurável e pausas

## Resultado da decisão

Opção escolhida: **B. Dias úteis com pausa**, porque é justa com quem atende e reaproveita o calendário já decidido, sem a complexidade de controlar expediente em horas. Por decisão de Marcelo, o prazo de primeira resposta entra na v1.

Escopo:

| Item | Classificação |
| --- | --- |
| Prioridades baixa, média, alta e crítica, escolhidas por quem abre; quem atende pode alterar, com registro na linha do tempo | v1 |
| Prazo de resolução por prioridade, configurável na Administração (padrão: 10, 5, 2 e 1 dias úteis) | v1 |
| Prazo de primeira resposta por prioridade, configurável na Administração (padrão: 1 dia útil para todas); cumprido pela primeira mensagem de quem pode atender o destino (ADR-0023) | v1 |
| Prazos contados em dias úteis, com o calendário da instalação | v1 |
| Prazo de resolução pausado enquanto o chamado está Aguardando solicitante | v1 |
| Mudar a prioridade recalcula os prazos a partir da abertura | v1 |
| Barra de prazo na página do chamado e destaque para prazos vencendo e vencidos na lista (resolução e primeira resposta) | v1 |
| Prazos diferentes por destino ou fila | depois |
| Prazos em horas úteis | depois |

Os avisos de prazo por e-mail serão decididos no bloco de e-mail.

### Consequências

- Bom, porque fins de semana, feriados e o tempo esperando o solicitante não contam contra quem atende.
- Bom, porque o prazo de primeira resposta evita chamados abertos sem nenhum retorno.
- Ruim, porque o sistema precisa registrar as pausas e recalcular prazos.
- Ruim, porque, em dias, o prazo de primeira resposta é pouco preciso para chamados críticos; a precisão em horas fica para depois.

### Confirmação

Testes cobrindo: prazos somam dias úteis pulando fins de semana e feriados; o prazo de resolução não corre enquanto o chamado está Aguardando solicitante e é estendido pelos dias pausados; a primeira resposta é cumprida pela mensagem de quem atende o destino, e não pela do solicitante; mudar a prioridade recalcula os prazos a partir da abertura; valores alterados na Administração valem para chamados novos.

## Prós e contras das opções

### A. Dias corridos, sem pausa

- Bom, porque é o mais simples.
- Ruim, porque fins de semana, feriados e espera pelo solicitante contam contra quem atende.

### B. Dias úteis com pausa

- Bom, porque é justo e reaproveita o calendário.
- Ruim, porque exige registrar pausas e recalcular.

### C. Horas úteis

- Bom, porque é o mais preciso.
- Ruim, porque exige controlar expediente, feriados e fusos, sem ganho claro na v1.
