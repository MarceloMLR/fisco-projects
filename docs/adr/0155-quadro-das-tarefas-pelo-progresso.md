---
status: aceita
date: 2026-11-10
decision-makers: Marcelo Ribeiro
---

# ADR-0155: Quadro das tarefas do cronograma, com as colunas pelo progresso

Completa o ADR-0071 (cronograma na tela), que tem a Linha do tempo e a Lista, e segue o ADR-0020 (progresso e atraso das tarefas) e o ADR-0086 ("Iniciar" pede o percentual).

## Contexto e problema

As tarefas do FiscoProjects ficam no cronograma, com responsável, datas, progresso, atraso, data-limite e notas, vistas na Linha do tempo e na Lista. Em 10/11/2026, ao comparar o produto com o Jira e o monday.com, Marcelo viu que quem conhece essas ferramentas espera um quadro: as tarefas em colunas, com o cartão movido quando o trabalho anda. Numa demonstração, a falta dele passa a impressão de produto incompleto, mesmo com o cronograma mais completo. Pediu o quadro como a primeira change do grupo 16 (Tarefas-1). De onde vêm as colunas, quais tarefas entram e o que mover o cartão muda?

## Fatores de decisão

- Uma tarefa só, vista de três jeitos: nada novo para manter em dia.
- O servidor continua a fonte da verdade das datas (ADR-0051), e o progresso continua a régua do andamento e do atraso (ADR-0020).
- Mover o cartão rápido no dia a dia, também pelo teclado e no celular.
- Sem campos personalizados (ADR-0016) e sem tela nova para aprender.

## Opções consideradas

- A. O quadro como mais uma visualização do cronograma, com as colunas pelo progresso
- B. Um status próprio da tarefa (A fazer, Em andamento, Bloqueada, Concluída), separado do progresso
- C. Colunas configuráveis em cada projeto
- D. Um quadro de tarefas avulsas, sem datas, fora do cronograma

## Resultado da decisão

Opção escolhida: **A**. O quadro mostra as mesmas tarefas da Lista e da Linha do tempo, e mover o cartão muda o progresso pelo mesmo pedido do detalhe. B cria uma segunda régua que pode discordar do progresso (Concluída a 40%); C é um campo personalizado (ADR-0016); D pede tarefas sem datas, contra o cálculo do cronograma no servidor (ADR-0051), e fica para depois, se um cliente pedir.

Regras:

- **A visualização.** "Quadro", ao lado de "Linha do tempo" e "Lista", na aba Cronograma, com `?view=board` no endereço, para todas as pessoas que veem o cronograma, no computador e no celular.
- **As colunas.** A fazer (0%), Em andamento (de 1% a 99%) e Concluída (100%), com a quantidade de tarefas em cada uma e o ícone e o tom dos status do produto: neutro, info e sucesso (ADR-0065).
- **O que entra.** As tarefas com datas próprias: a tarefa sem subtarefas e a subtarefa. A tarefa-resumo, cujo progresso sai das subtarefas (ADR-0088), e os marcos, que são atingidos e não andam por percentual (ADR-0086), ficam na Lista e na Linha do tempo.
- **O cartão.** O nome (com a marca da nota), a fase e a tarefa-mãe da subtarefa, o responsável (a pessoa, o cliente ou "Sem responsável", com o aviso de ausência), o fim ou os dias úteis de atraso, a barra do progresso em Em andamento e o dia da conclusão em Concluída. Escolher o cartão abre o detalhe da tarefa.
- **A ordem.** Em A fazer e Em andamento, pelo fim, do mais cedo ao mais tarde; em Concluída, pelo dia da conclusão, do mais recente ao mais antigo; no empate, a ordem do cronograma. Não há ordem escolhida à mão: o que vence antes fica no alto.
- **Mover.** Quem atualiza o progresso da tarefa (ADR-0043: o administrador e o gerente, qualquer tarefa; o líder técnico, o colaborador e o desenvolvedor, as tarefas de que são responsáveis) arrasta o cartão no computador ou usa "Mover", no cartão, em qualquer tela. Para Concluída, a tarefa vai a 100% na hora, como "Concluir"; para A fazer, volta a 0% na hora; para Em andamento, a tela pede o percentual, como "Iniciar" e "Atualizar progresso" (ADR-0086). O cartão fica na coluna nova enquanto o pedido não volta, e a recusa o devolve, com o aviso. Cada mudança é um passo do desfazer de quem edita o cronograma, como no detalhe (ADR-0091).
- **Filtros.** O filtro do cronograma (GanttA-3) vale também no quadro.

Fora:

| Item | Classificação |
| --- | --- |
| Tarefas avulsas, sem datas, fora do cronograma | depois, se um cliente pedir |
| Colunas configuráveis e status próprio da tarefa | fora (ADR-0016) |
| Limite de tarefas por coluna | fora |
| Raias por responsável ou por fase | depois |
| Ordem escolhida à mão dentro da coluna | fora: a ordem vem do fim |
| Marcos e tarefa-resumo no quadro | fora |

### Consequências

- Bom, porque o cliente que vem do Jira ou do monday.com reconhece o quadro, e a demonstração mostra o cronograma de três jeitos.
- Bom, porque nenhum dado novo: o quadro, a Lista, a Linha do tempo, o Meu trabalho e o atraso sempre concordam.
- Ruim, porque mover para Em andamento pede o percentual, um passo a mais que nos quadros por status.
- Ruim, porque quem quer anotar uma pendência sem data ainda não tem onde, fora das notas da tarefa.

### Confirmação

Testes de tela cobrindo: as colunas com as tarefas certas e as quantidades, sem a tarefa-resumo e os marcos; o cartão, a ordem, o detalhe pelo cartão, o filtro e o celular; mover para Concluída, para A fazer e para Em andamento, arrastando e pelo menu, com o pedido certo; a recusa que devolve o cartão; e os cartões que não se movem (o leitor e a tarefa de outra pessoa). Testes das regras: a coluna de cada progresso, a ordem e o que mover para cada coluna faz.

## Prós e contras das opções

### A. Visualização do cronograma, com as colunas pelo progresso

- Bom, porque é uma tela nova sobre os dados de sempre.
- Bom, porque as regras de quem muda o progresso e do atraso valem sem mudança.
- Ruim, porque as colunas são fixas.

### B. Status próprio da tarefa

- Bom, porque permite "Bloqueada" e outros estados que o percentual não diz.
- Ruim, porque o status e o progresso podem discordar, e o atraso, o andamento do projeto e o Meu trabalho teriam de escolher um dos dois.

### C. Colunas configuráveis

- Bom, porque cada equipe monta o próprio fluxo.
- Ruim, porque é um campo personalizado, fora do produto (ADR-0016), e pede configuração em cada projeto.

### D. Tarefas avulsas fora do cronograma

- Bom, porque cabe a pendência sem data.
- Ruim, porque cria um segundo tipo de tarefa, fora do cálculo das datas, do caminho crítico e da linha de base.
