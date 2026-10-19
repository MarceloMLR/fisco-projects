# Spec Delta

## Purpose

Define os dados fictícios de demonstração do desenvolvimento: um comando que, pela própria API e pela entrada local, carrega pessoas, clientes, projetos, membros e cronogramas com datas relativas ao dia em que roda, sem nunca chegar à produção (ADR-0078, ADR-0089).

## ADDED Requirements

### Requirement: Carga dos dados de demonstração

O comando `npm run demo:data` MUST carregar, pela API indicada (padrão `http://localhost:5175`), as pessoas do diretório de demonstração com os papéis delas, clientes, projetos com gerente, líder técnico e membros, e os cronogramas com progresso, marcos atingidos e uma tarefa atrasada. Cada ação MUST ser feita por quem a faria na tela, e todo nome MUST ser fictício (ADR-0002, ADR-0003, ADR-0089).

#### Scenario: Carregar dados de demonstração
- **WHEN** o comando roda numa instalação de desenvolvimento sem os dados
- **THEN** o administrador cria os clientes e os projetos, a gerente monta os cronogramas, cada responsável atualiza o progresso da própria tarefa, e o comando diz o que carregou

#### Scenario: Dados já carregados
- **WHEN** o comando roda de novo e o projeto ERP-FIN já existe
- **THEN** o comando avisa que os dados já estão lá e não cria nem muda nada

#### Scenario: Sem entrada local
- **WHEN** o comando roda contra uma API com a entrada local desligada, como a de produção
- **THEN** o comando para com uma mensagem, sem criar nada

### Requirement: Datas da demonstração

As datas dos cronogramas MUST ser relativas ao dia em que o comando roda: o projeto principal começa na segunda-feira de duas semanas antes, para haver tarefas concluídas, uma atrasada, a linha de hoje e tarefas futuras. O servidor ajusta cada data aos dias úteis (ADR-0018, ADR-0089).

#### Scenario: Datas relativas a hoje
- **WHEN** o comando roda na segunda-feira 19/10/2026
- **THEN** a primeira tarefa do projeto principal começa em 05/10/2026, e a de um dia rodado uma semana depois começa uma semana depois
