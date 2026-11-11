# minhas-tarefas Specification

## Purpose

Minhas tarefas: a página com todas as tarefas de que a pessoa é responsável nos projetos em aberto de que participa, agrupadas por projeto ou por data, em Lista e Quadro, com o detalhe da tarefa na própria página, sem mudar o que cada papel vê e altera nos projetos (ADR-0158).

## Requirements

### Requirement: Página Minhas tarefas

A barra lateral MUST ter "Minhas tarefas", entre Início e Projetos, para todos. A página MUST trazer as tarefas com datas próprias (a subtarefa entra, a tarefa-resumo não) de que a pessoa é responsável, nos projetos não concluídos nem cancelados de que ela participa, e MUST dizer quantas estão em aberto e em quantos projetos (ADR-0158).

#### Scenario: Tarefas de dois projetos
- **WHEN** a desenvolvedora tem tarefas em aberto em dois projetos e abre Minhas tarefas
- **THEN** a página traz as tarefas dela dos dois projetos, cada projeto num grupo, pelo código

#### Scenario: Só as tarefas da pessoa
- **WHEN** a pessoa abre Minhas tarefas num projeto com tarefas de outras pessoas, de ninguém e do cliente
- **THEN** só as tarefas de que ela é a responsável aparecem

#### Scenario: Projeto encerrado e tarefa-resumo
- **WHEN** a pessoa é responsável por uma tarefa de um projeto cancelado e por uma tarefa com subtarefas
- **THEN** nenhuma das duas aparece, e as subtarefas dela aparecem

### Requirement: Concluídas em Minhas tarefas

As tarefas a 100% MUST ficar fora de início; "Mostrar concluídas" MUST trazer as concluídas nos últimos 30 dias corridos, no fuso da instalação (ADR-0158).

#### Scenario: Mostrar concluídas
- **WHEN** a pessoa marca "Mostrar concluídas"
- **THEN** a tarefa concluída há 10 dias aparece, e a concluída há 40 dias não

### Requirement: Agrupar por data

Minhas tarefas MUST agrupar por projeto de início, com o nome do projeto e o atalho para o cronograma, e MUST poder agrupar por data: Atrasadas, Hoje, Esta semana, Próxima semana, Depois e Concluídas, pelo dia de hoje da instalação, com a semana de segunda a domingo e o código do projeto em cada linha. A escolha MUST ficar no endereço (ADR-0158).

#### Scenario: Agrupar por data
- **WHEN** a pessoa escolhe agrupar por data, numa quarta-feira, com tarefas atrasada, de hoje, de sexta, da semana seguinte e do mês seguinte
- **THEN** cada uma fica no seu grupo, na ordem Atrasadas, Hoje, Esta semana, Próxima semana e Depois

### Requirement: Quadro em Minhas tarefas

Minhas tarefas MUST ter a Lista e o Quadro, com as colunas A fazer, Em andamento e Concluída pelo progresso e o código do projeto em cada cartão; quem atualiza o progresso MUST mover o cartão como no cronograma (ADR-0155, ADR-0158).

#### Scenario: Concluir pelo Quadro
- **WHEN** a pessoa leva ao Concluída o cartão de uma tarefa dela em andamento
- **THEN** a tarefa fica a 100% no projeto dela

### Requirement: Detalhe em Minhas tarefas

A tarefa escolhida MUST abrir o detalhe na própria página, com o projeto e a fase, as datas, o progresso, a nota, os comentários e "Abrir no cronograma", que leva ao detalhe da tarefa na aba Cronograma (ADR-0157, ADR-0158).

#### Scenario: Detalhe com comentários
- **WHEN** a pessoa escolhe uma tarefa em Minhas tarefas
- **THEN** o detalhe abre ali, com os comentários da tarefa e "Abrir no cronograma"

### Requirement: Visibilidade nos projetos

Minhas tarefas MUST NOT mudar o que cada papel vê e altera nos projetos (ADR-0043): a API MUST devolver só as tarefas da pessoa na sessão, e o cronograma continua inteiro para todos do projeto (ADR-0158).

#### Scenario: Tarefas de outra pessoa
- **WHEN** a gerente abre Minhas tarefas
- **THEN** a página traz só as tarefas de que ela é a responsável, e o cronograma dos projetos continua com as tarefas de todos
