---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0020: Progresso das tarefas em percentual com atalhos, ponderado pela duração

## Contexto e problema

No MVP, o responsável atualiza o progresso da própria tarefa, e tarefas atrasadas ficam em vermelho. O progresso do projeto e das fases é calculado (ADR-0016, ADR-0019). Falta definir como o progresso de cada tarefa é informado e como ele se soma no total.

## Fatores de decisão

- Progresso do projeto fiel ao andamento real.
- Atualização rápida no dia a dia, inclusive no celular (ADR-0012).
- Simplicidade.

## Opções consideradas

- A. Percentual de 0 a 100%, informado pelo responsável
- B. Só três situações: não iniciada, em andamento e concluída
- C. Percentual com atalhos "Iniciar" e "Concluir"

## Resultado da decisão

Opção escolhida: **C. Percentual com atalhos**, porque une o detalhe do percentual à agilidade de marcar início e conclusão com um toque.

Escopo:

| Item | Classificação |
| --- | --- |
| Progresso da tarefa em percentual, com atalhos "Iniciar" e "Concluir", atualizado pelo responsável ou pelo gestor do projeto (papéis exatos na matriz de permissões) | v1 |
| Um responsável por tarefa | v1 |
| Marco atingido ou não, marcado pelo gestor ou por um termo de aceite (a decidir no bloco de termos) | v1 |
| Progresso da fase e do projeto ponderado pela duração das tarefas | v1 |
| Tarefa atrasada (prazo passou sem chegar a 100%) destacada em vermelho | v1 |
| Data real de conclusão registrada quando a tarefa chega a 100% | v1 |
| Vários responsáveis por tarefa | depois |
| Comentários e anexos na própria tarefa | depois |
| Apontamento de horas trabalhadas | fora |

### Consequências

- Bom, porque tarefas longas pesam mais no progresso, refletindo melhor o andamento.
- Bom, porque os atalhos tornam a atualização rápida no celular.
- Ruim, porque o percentual continua subjetivo entre "Iniciar" e "Concluir".

### Confirmação

Testes cobrindo: "Iniciar" e "Concluir" ajustam o percentual; progresso da fase e do projeto ponderado pela duração; tarefa vencida abaixo de 100% aparece como atrasada; data real de conclusão é gravada ao chegar a 100%; marco só tem os estados atingido ou não.

## Prós e contras das opções

### A. Percentual

- Bom, porque dá detalhe.
- Ruim, porque é subjetivo e mais lento de atualizar.

### B. Três situações

- Bom, porque é simples e honesto.
- Ruim, porque o progresso do projeto anda aos saltos.

### C. Percentual com atalhos

- Bom, porque combina detalhe e agilidade.
- Ruim, porque exige um pouco mais de interface.
