---
status: proposta
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0088: Subtarefas na v1, em um nível, com a tarefa-resumo calculada e dependências só entre quem faz o trabalho

Substitui, no ADR-0019, a linha "Subtarefas (mais de dois níveis) | depois". O restante do ADR-0019 continua valendo.

## Contexto e problema

O ADR-0019 deixou o cronograma em dois níveis (fases e, dentro delas, tarefas e marcos) e as subtarefas para depois da v1. Marcelo decidiu trazê-las para a v1: uma entrega grande, como "Parametrizar regras fiscais", costuma ser quebrada em partes com responsáveis e prazos próprios, e o cronograma que vem do MS Project ou de planilhas já chega assim. Falta decidir quantos níveis, como fica a tarefa que tem subtarefas, onde ficam as dependências e o que pode ficar dentro de uma tarefa.

## Fatores de decisão

- Cronograma legível na Lista e na Linha do tempo, no computador e no celular.
- Datas coerentes, calculadas no servidor, sem duas fontes para a mesma data (ADR-0051).
- Compatível com o XML do MS Project da exportação (ADR-0021).
- Cálculo e testes proporcionais à v1.

## Opções consideradas

- Níveis: **A1.** um nível (fase › tarefa › subtarefa); **A2.** vários níveis, como no MS Project.
- Tarefa com subtarefas: **B1.** vira resumo, com datas e progresso calculados das subtarefas; **B2.** mantém datas próprias, e as subtarefas precisam caber dentro delas.
- Dependências: **C1.** só entre quem faz o trabalho (subtarefas, tarefas sem subtarefas e marcos); **C2.** também na tarefa-resumo, valendo para todas as subtarefas.
- Conteúdo: **D1.** tarefas e marcos dentro de uma tarefa; **D2.** só tarefas.

## Resultado da decisão

Opções escolhidas por Marcelo: **A1, B1, C1 e D1**.

Regras:

- Uma tarefa que está diretamente na fase pode ter subtarefas: tarefas e marcos. Subtarefa não tem subtarefas, e marco não tem subtarefas. A subtarefa fica sempre na fase da tarefa; mudar a tarefa de fase leva as subtarefas junto.
- A tarefa com subtarefas é a **tarefa-resumo**: o início, o fim e a duração em dias úteis vêm das subtarefas, como numa fase; o progresso é ponderado pela duração das subtarefas (ADR-0020); a conclusão é o dia em que a última subtarefa chegou a 100%; ela fica atrasada quando uma subtarefa está atrasada. Nela se mudam só o nome, a fase e o responsável. As datas e o progresso dela não se editam nem se arrastam.
- No progresso da fase e do projeto e na contagem de tarefas atrasadas, contam as subtarefas e as tarefas sem subtarefas; a tarefa-resumo não conta de novo.
- Dependências só entre subtarefas, tarefas sem subtarefas e marcos, inclusive entre tarefas e fases diferentes. A tarefa-resumo não depende de nada e ninguém depende dela.
  - Quando uma tarefa ganha a primeira subtarefa, as dependências dela, antes e depois, passam para essa subtarefa.
  - Quando a última subtarefa é excluída, as dependências dela voltam para a tarefa, que guarda as datas e o progresso que tinha como resumo. Se a última subtarefa sai da tarefa (vai para outra ou direto para a fase), ela leva as próprias dependências, e a tarefa fica sem nenhuma.
- Uma tarefa sem subtarefas pode ir para dentro de outra tarefa (e sair dela); uma tarefa-resumo não pode.
- Excluir a tarefa-resumo exclui as subtarefas e as dependências delas, sem mover as outras tarefas (ADR-0019).
- O responsável da tarefa-resumo responde pelo conjunto; quem atualiza o progresso de cada subtarefa segue o ADR-0043, pelo responsável dela.

### Consequências

- Bom, porque grandes entregas ficam quebradas em partes com responsável e prazo, sem perder a visão do conjunto.
- Bom, porque a tarefa-resumo funciona como a fase, que todos já conhecem, e as datas continuam com uma só fonte.
- Bom, porque um nível cabe na Lista, na Linha do tempo e no celular, e o XML do MS Project o representa pelos níveis de tópico.
- Ruim, porque o cálculo do cronograma ganha um passo (o resumo) e regras de passagem de dependências, com mais testes.
- Ruim, porque quem importar ou exportar cronogramas com mais níveis terá de achatá-los; a importação já é depois da v1 (ADR-0021).
- Neutro, porque o caminho crítico, a linha de base, o desfazer e a exportação (ADR-0021) passam a considerar as subtarefas desde o início.

### Confirmação

Testes cobrindo: incluir subtarefa e marco dentro de uma tarefa; recusar subtarefa de subtarefa, subtarefa de marco e tarefa-resumo dentro de outra; datas, duração, progresso, conclusão e atraso da tarefa-resumo; progresso da fase e do projeto sem contar a tarefa-resumo duas vezes; passagem das dependências na primeira e na última subtarefa; recusa de dependência com tarefa-resumo; empurrar subtarefas de tarefas diferentes; mudar a tarefa-resumo de fase; excluir a tarefa-resumo; recusa de progresso e de novas datas na tarefa-resumo; a Lista e a Linha do tempo com as subtarefas abaixo da tarefa.

## Prós e contras das opções

### A1. Um nível

- Bom, porque cobre a quebra de uma entrega em partes, que é o caso comum.
- Ruim, porque não representa estruturas mais profundas.

### A2. Vários níveis

- Bom, porque espelha o MS Project.
- Ruim, porque a tela fica difícil de ler, sobretudo no celular, e o cálculo e os testes crescem com a profundidade.

### B1. Tarefa-resumo calculada

- Bom, porque as datas têm uma só fonte e a tarefa se comporta como a fase.
- Ruim, porque a tarefa troca a duração e o progresso próprios pelos das subtarefas ao ganhar a primeira.

### B2. Datas próprias

- Bom, porque a tarefa mantém o prazo combinado.
- Ruim, porque cria duas fontes para o mesmo período e conflitos quando uma subtarefa é empurrada para fora dele.

### C1. Dependências só entre quem faz o trabalho

- Bom, porque o cálculo em cadeia continua o mesmo, só entre itens com datas próprias.
- Ruim, porque ligar "o conjunto" a outra tarefa exige ligar a subtarefa certa.

### C2. Dependências também na tarefa-resumo

- Bom, porque é como no MS Project.
- Ruim, porque uma dependência passa a valer para várias subtarefas e complica o empurrar e o caminho crítico.

### D1. Tarefas e marcos

- Bom, porque um ponto de controle, como "Layout aprovado", fica junto da entrega a que pertence.
- Ruim, porque o marco dentro da tarefa precisa aparecer também nos termos de aceite e na faixa de fases como os outros marcos.

### D2. Só tarefas

- Bom, porque é mais simples.
- Ruim, porque os marcos de uma entrega ficariam soltos na fase.
