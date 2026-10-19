# Spec Delta

## MODIFIED Requirements

### Requirement: Lista de projetos

A lista MUST mostrar os projetos que a pessoa vê, em ordem alfabética de nome, com o nome, o código, o cliente (ou "Projeto interno"), o status, o cronograma, o prazo final e o gerente. Por padrão, MUST mostrar só os projetos ativos; o filtro de situação MUST oferecer Ativos, Concluídos e cancelados e Todos. A lista MUST ter busca por código ou nome, sem diferenciar maiúsculas e acentos, e filtro de cliente (ADR-0015, ADR-0016).

#### Scenario: Projetos na lista
- **WHEN** a pessoa abre Projetos
- **THEN** a lista mostra os projetos ativos que ela vê, em ordem alfabética, com o rodapé "N projetos"

#### Scenario: Projetos encerrados pelo filtro
- **WHEN** a pessoa escolhe "Concluídos e cancelados"
- **THEN** a lista mostra só os projetos concluídos e cancelados

#### Scenario: Buscar e filtrar projetos
- **WHEN** a pessoa busca "erp" ou escolhe um cliente
- **THEN** a lista mostra só os projetos que combinam

### Requirement: Cabeçalho do projeto

A página do projeto MUST ter o cabeçalho do canvas ProjetoCabecalho: o logo do cliente ou as iniciais, o nome, o status, o código, o cliente, o gerente, o líder técnico e as datas do cronograma, ou "Sem cronograma" sem tarefas nem marcos, e as abas Visão geral, Cronograma e Membros, esta com o número de pessoas. A aba Visão geral MUST mostrar a descrição e a equipe por time: Gestão, Funcional e Desenvolvimento (ADR-0016, ADR-0071, ADR-0072).

#### Scenario: Cabeçalho do projeto
- **WHEN** uma pessoa do projeto abre a página do projeto
- **THEN** o cabeçalho mostra o nome, o status, o código, o cliente, o gerente e o líder técnico, e a trilha mostra Projetos e o código

#### Scenario: Abas do projeto
- **WHEN** uma pessoa do projeto escolhe a aba Membros no cabeçalho
- **THEN** a aba Membros fica marcada, mostra o número de pessoas e abre as pessoas do projeto, e Visão geral volta à descrição e à equipe

#### Scenario: Datas do cronograma no cabeçalho
- **WHEN** o cronograma do projeto vai de 05/10/2026 a 13/11/2026
- **THEN** o cabeçalho mostra "05/10 – 13/11/2026" no lugar de "Sem cronograma"

## ADDED Requirements

### Requirement: Cronograma na lista de projetos

Na lista, a coluna Cronograma MUST mostrar a faixa de fases, com a parte concluída de cada fase, as fases atrasadas, os marcos e o dia de hoje, e embaixo o progresso com a fase atual: a primeira fase atrasada ou, sem atraso, a última que já começou; antes do início, "Começa em" e a data; com todas as tarefas a 100%, a data da conclusão. A coluna Prazo final MUST mostrar o fim do cronograma e as tarefas atrasadas, com ícone e texto. Sem tarefas nem marcos, "Sem cronograma" (ADR-0065, ADR-0071).

#### Scenario: Cronograma na lista
- **WHEN** hoje é 16/10/2026 e a pessoa abre Projetos com um projeto de três fases, a segunda em andamento, que termina em 13/11/2026 com duas tarefas atrasadas na segunda fase
- **THEN** a linha do projeto mostra a faixa com as três fases, a segunda atrasada, o progresso com o nome da segunda fase, "13/11/2026" e "2 tarefas atrasadas"

#### Scenario: Projeto que ainda não começou
- **WHEN** hoje é 16/10/2026 e o cronograma de um projeto começa em 26/10/2026
- **THEN** a linha do projeto mostra a faixa e "Começa em 26/10"

#### Scenario: Projeto sem cronograma na lista
- **WHEN** um projeto não tem tarefas nem marcos
- **THEN** a linha do projeto mostra "Sem cronograma" e fica sem prazo final
