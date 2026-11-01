---
status: aceita (item "Subtarefas" substituído pelo ADR-0088; tipos e intervalo das dependências, pelo ADR-0137)
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0019: Cronograma em dois níveis, com dependências que empurram as tarefas seguintes

## Contexto e problema

O MVP tem fases, tarefas, marcos e setas de dependência no Gantt. Falta definir quantos níveis o cronograma tem e o que acontece com as tarefas seguintes quando uma tarefa anterior muda de data. As datas seguem os dias úteis e feriados (ADR-0018).

## Fatores de decisão

- Datas coerentes sem trabalho manual repetitivo.
- Controle do gestor sobre o cronograma.
- Complexidade de cálculo e de testes proporcional à v1.

## Opções consideradas

- A. Dependências só visuais, com destaque de conflito
- B. Dependências que empurram as tarefas seguintes para frente
- C. Agendamento automático completo (tarefas seguintes sempre o mais cedo possível)

## Resultado da decisão

Opção escolhida: **B. Dependências que empurram as tarefas seguintes para frente**, porque mantém as datas coerentes sem trabalho manual e deixa as antecipações sob controle do gestor.

Escopo:

| Item | Classificação |
| --- | --- |
| Dois níveis: fases e, dentro delas, tarefas e marcos | v1 |
| Datas e progresso da fase calculados a partir das tarefas dela | v1 |
| Marco como um ponto no tempo (uma data, sem duração) | v1 |
| Dependência do tipo "termina → começa": a tarefa seguinte só começa depois que a anterior termina | v1 |
| Atrasar ou mover uma tarefa para depois empurra as seguintes (respeitando dias úteis e feriados); antecipar não as puxa | v1 |
| Dependência circular recusada | v1 |
| Subtarefas (mais de dois níveis) | depois (substituído pelo ADR-0088: um nível de subtarefas na v1) |
| Outros tipos de dependência (começa junto, termina junto) e intervalo entre tarefas | depois |
| Dependências entre projetos diferentes | fora |

### Consequências

- Bom, porque o cronograma continua coerente quando algo atrasa.
- Bom, porque o gestor decide quando antecipar tarefas.
- Ruim, porque o cálculo em cadeia exige testes cuidadosos.
- Ruim, porque uma mudança pode alterar as datas de várias tarefas; a forma de avisar por e-mail será decidida no bloco de e-mail.

### Confirmação

Testes cobrindo: atrasar uma tarefa empurra todas as seguintes da cadeia, pulando fins de semana e feriados; antecipar uma tarefa não move as seguintes; dependência circular é recusada; datas e progresso da fase refletem as tarefas; marco não tem duração.

## Prós e contras das opções

### A. Dependências só visuais

- Bom, porque é o mais simples e dá controle total.
- Ruim, porque o cronograma fica incoerente até ajuste manual.

### B. Empurrar para frente

- Bom, porque mantém as datas coerentes e preserva o controle sobre antecipações.
- Ruim, porque exige cálculo em cadeia e pode mudar várias datas de uma vez.

### C. Agendamento automático completo

- Bom, porque é o mais automático.
- Ruim, porque tira o controle do gestor e surpreende quem não está acostumado.
