## ADDED Requirements

### Requirement: Andamento previsto

A visão geral MUST mostrar o andamento do projeto contra o previsto para hoje: o previsto é a média, ponderada pela duração, da parte de cada tarefa com datas próprias que já deveria estar feita até o fim de hoje, em dias úteis do calendário da instalação, pelas datas da linha de base quando ela existe e pelas do cronograma quando não (ADR-0020, ADR-0021, ADR-0073).

#### Scenario: Previsto pelo cronograma
- **WHEN** hoje é segunda, 19/10/2026, e o projeto, sem linha de base, tem uma tarefa de 5 dias úteis que começou em 13/10 e outra de 5 dias úteis que começa em 26/10
- **THEN** o andamento previsto é 50%: a primeira deveria estar inteira e a segunda ainda não começou

#### Scenario: Previsto pela linha de base
- **WHEN** a linha de base foi salva com uma tarefa de 13/10 a 19/10, e depois a tarefa foi adiada para 21/10 a 27/10
- **THEN** o previsto para 19/10 é 100%, pelas datas da linha de base

#### Scenario: Previsto conta só dias úteis
- **WHEN** hoje é segunda, 02/11/2026, feriado nacional, e uma tarefa de 4 dias úteis começou na quinta, 29/10
- **THEN** a tarefa deveria estar em 50%: quinta e sexta, sem o fim de semana nem o feriado

### Requirement: Prazo final contra a linha de base

A visão geral MUST mostrar o fim do cronograma e, com a linha de base, o fim dela e o desvio em dias úteis, com ícone, cor e texto: depois da linha de base, atrasado; antes ou na data, no prazo (ADR-0021, ADR-0065, ADR-0073).

#### Scenario: Desvio do prazo final
- **WHEN** a linha de base termina em 11/11/2026 e o cronograma agora termina em 13/11/2026
- **THEN** a visão geral mostra 13/11/2026, "+2 dias úteis" e "linha de base 11/11"

#### Scenario: Prazo sem linha de base
- **WHEN** o projeto tem cronograma e não tem linha de base
- **THEN** a visão geral mostra o fim do cronograma e "Sem linha de base"

### Requirement: Chamados e próximo marco na visão geral

A visão geral MUST mostrar quantos chamados do projeto estão abertos, de quantos ao todo, quantos estão vencidos e quantos aguardam o solicitante, e o próximo marco não atingido, com a data e os dias úteis que faltam, ou há quantos está atrasado (ADR-0025, ADR-0073).

#### Scenario: Chamados na visão geral
- **WHEN** o projeto tem 9 chamados, 7 abertos, um deles com a resolução vencida e 2 aguardando o solicitante
- **THEN** a visão geral mostra "7 de 9", "1 vencido" e "2 aguardando solicitante"

#### Scenario: Próximo marco
- **WHEN** hoje é 19/10/2026 e o próximo marco não atingido é Homologação, em 03/11/2026
- **THEN** a visão geral mostra Homologação, 03/11 e "em 10 dias úteis"

### Requirement: Precisa de atenção

A visão geral MUST listar o que precisa de atenção no projeto: as tarefas atrasadas, com o responsável, o progresso e o que depende delas; os chamados abertos vencidos, que vencem até o próximo dia útil ou sem responsável; os pedidos de aceite pendentes que a pessoa vê (ADR-0045); e os chamados em fornecedores sem atualização há 5 dias úteis ou mais (ADR-0028). Só itens do projeto (ADR-0073).

#### Scenario: Tarefa atrasada na atenção
- **WHEN** a tarefa Validar cadastro de produtos, de Diego, em 80%, venceu e Configurar plano de contas depende dela
- **THEN** a lista mostra a tarefa com "Tarefa de Diego Torres · 80% · bloqueia Configurar plano de contas" e o atraso

#### Scenario: Chamados na atenção
- **WHEN** o projeto tem um chamado que vence amanhã, um sem responsável e um com prazo para daqui a uma semana
- **THEN** a lista mostra os dois primeiros e não o terceiro

#### Scenario: Aceites e fornecedores na atenção
- **WHEN** o projeto tem um pedido de aceite pendente e um chamado no fornecedor sem atualização há 6 dias úteis
- **THEN** a gerente vê os dois na lista, e um desenvolvedor do projeto vê só o chamado no fornecedor

#### Scenario: Só itens do projeto
- **WHEN** outro projeto da pessoa tem tarefas atrasadas
- **THEN** a visão geral deste projeto não as mostra

### Requirement: Equipe na visão geral

A visão geral MUST mostrar a equipe por time: Gestão (o gerente e o líder técnico), Funcional (os colaboradores) e Desenvolvimento (o líder técnico e os desenvolvedores), com as pessoas, quantas são e, nos dois times que atendem, quantos chamados abertos cada um tem, e o link para Membros (ADR-0068, ADR-0073).

#### Scenario: Equipe na visão geral
- **WHEN** o projeto tem 3 chamados abertos do time Funcional e 4 do Desenvolvimento
- **THEN** a equipe mostra "2 pessoas · 3 chamados abertos" em Funcional e "3 pessoas · 4 chamados abertos" em Desenvolvimento

### Requirement: Visão geral pela API

`GET /api/projects/{id}/overview` MUST responder, para quem vê o projeto, o andamento, o prazo final, os chamados, o próximo marco, as fases e os marcos com os dias não úteis do período, o que precisa de atenção e a equipe; para quem não vê o projeto, 404 (ADR-0043, ADR-0073).

#### Scenario: Visão geral pela API
- **WHEN** uma pessoa do projeto pede a visão geral
- **THEN** a API responde os números, as fases e os marcos, a atenção e a equipe

#### Scenario: Visão geral de quem não está no projeto
- **WHEN** quem não está no projeto pede a visão geral
- **THEN** a API responde 404

### Requirement: Visão geral pela tela

A aba Visão geral MUST mostrar, como o canvas ProjetoVisaoGeral, os quatro números, com a barra do andamento e a marca do previsto; as fases e os marcos numa linha do tempo menor que se ajusta ao espaço, com hoje, fins de semana, feriados e "Abrir cronograma"; a equipe; e "Precisa de atenção", com a contagem. Sem cronograma, os números de prazo MUST mostrar "Sem cronograma" (ADR-0016, ADR-0071, ADR-0073).

#### Scenario: Visão geral na tela
- **WHEN** a gerente abre a aba Visão geral do ERP-FIN
- **THEN** a tela mostra os quatro números, a linha do tempo das fases e dos marcos, a equipe e o que precisa de atenção

#### Scenario: Projeto sem cronograma na visão geral
- **WHEN** o projeto não tem tarefas nem marcos
- **THEN** o andamento, o prazo final e o próximo marco mostram "Sem cronograma", e a linha do tempo dá lugar a uma mensagem

#### Scenario: Nada precisa de atenção
- **WHEN** o projeto não tem nada que precise de atenção
- **THEN** a lista mostra uma mensagem de que está tudo em dia

#### Scenario: Visão geral no celular
- **WHEN** a pessoa abre a Visão geral no celular
- **THEN** os números, a atenção, a linha do tempo e a equipe aparecem um embaixo do outro
