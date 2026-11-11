---
status: aceita
date: 2026-11-11
decision-makers: Marcelo Ribeiro
---

# ADR-0158: Página Minhas tarefas

Completa o ADR-0096 (Meu trabalho montado no servidor), sem mudar a fila por urgência, e segue o ADR-0043 (permissões no cronograma), o ADR-0155 (quadro das tarefas) e o ADR-0157 (comentários da tarefa).

## Contexto e problema

Quem trabalha em vários projetos só vê as próprias tarefas de duas formas: o Meu trabalho, que mostra só o que está atrasado ou vence nos próximos 7 dias (ADR-0096), e o filtro "Minhas tarefas" do cronograma, um projeto por vez. Para planejar a semana ou ver o que vem depois, a pessoa entra no cronograma de cada projeto. O Jira ("Assigned to me" da página "For you"), o Monday ("My Work") e o Planner ("Atribuídas a mim") têm uma página com as tarefas da pessoa de todos os projetos, e nenhum deles esconde por padrão as tarefas dos colegas: as permissões limitam a edição, não o que se vê. Em 11/11/2026, Marcelo pediu essa página e aprovou as decisões abaixo. Onde fica, o que traz e quem vê o quê?

## Fatores de decisão

- A pessoa vê todo o próprio trabalho num lugar só, sem abrir projeto por projeto.
- Nada muda no que cada papel vê e altera (ADR-0043).
- O mesmo detalhe, o mesmo quadro e os mesmos comentários do cronograma, sem uma segunda regra.

## Opções consideradas

- A. Página própria na barra lateral, agrupada por projeto ou por data, em Lista e Quadro
- B. Uma visão nova no Meu trabalho, ao lado de "Para mim"
- C. Restringir o cronograma para cada pessoa ver só as próprias tarefas

## Resultado da decisão

Opção escolhida: **A**, aprovada por Marcelo. A página própria é fácil de achar e não mistura a lista completa com a fila por urgência do Meu trabalho (B). Restringir o que se vê (C) esconderia as dependências e quem está segurando o quê, e é exceção configurada à mão no Jira e inexistente no Monday.

Regras:

| Item | Definição |
| --- | --- |
| Onde | A página "Minhas tarefas" na barra lateral, entre Início e Projetos, para todos |
| O que traz | As tarefas com datas próprias (a subtarefa entra, a tarefa-resumo não) de que a pessoa é responsável, nos projetos não concluídos nem cancelados de que ela participa; os marcos não têm responsável |
| Concluídas | Fora de início; "Mostrar concluídas" traz as concluídas nos últimos 30 dias corridos |
| Por projeto | O agrupamento de início: um grupo por projeto, pelo código, com o nome e o atalho para o cronograma; dentro, pelo fim, a mais próxima primeiro, e as concluídas pela conclusão mais recente |
| Por data | Atrasadas, Hoje, Esta semana, Próxima semana, Depois e Concluídas, pelo dia de hoje da instalação, com a semana de segunda a domingo, e o código do projeto em cada linha |
| Visualizações | Lista e Quadro (as colunas do ADR-0155), com o código do projeto em cada cartão; o Quadro move o cartão como no cronograma |
| Detalhe | Abre na própria página: o projeto e a fase, as datas, o progresso, a nota, os comentários e "Abrir no cronograma" |
| Visibilidade | Continua a do ADR-0043: todos do projeto veem o cronograma inteiro, e a página só recorta o que é da pessoa |
| Meu trabalho | Continua a fila por urgência do ADR-0096 |

### Consequências

- Bom, porque a pessoa planeja a semana de todos os projetos numa tela só.
- Bom, porque nenhuma permissão nova: a página mostra só o que a pessoa já via, e muda só o que ela já mudava.
- Ruim, porque a página monta o cronograma de cada projeto da pessoa para trazer os mesmos números da aba (atraso, data-limite, folga, comentários); com muitos projetos, ela demora um pouco mais.

### Confirmação

Testes da API: as tarefas da pessoa e só as dela, os projetos encerrados e os de fora, a tarefa-resumo e os marcos fora, as concluídas dos últimos 30 dias. Testes de tela: os dois agrupamentos, a Lista, o Quadro, o detalhe com os comentários e a barra lateral.

## Prós e contras das opções

### A. Página própria

- Bom, porque é a forma do Jira, do Monday e do Planner.
- Ruim, porque é mais um item na barra lateral.

### B. Visão no Meu trabalho

- Bom, porque não muda a barra lateral.
- Ruim, porque mistura a lista completa com a fila do que pede ação agora.

### C. Cronograma só com as tarefas da pessoa

- Bom, porque cada um vê menos coisa.
- Ruim, porque esconde as dependências e o trabalho dos colegas, que o time precisa ver.
