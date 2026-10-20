# dados-de-demonstracao Specification

## Purpose

Define os dados fictícios de demonstração do desenvolvimento: um comando que, pela própria API e pela entrada local, carrega pessoas, clientes, projetos, membros e cronogramas com datas relativas ao dia em que roda, sem nunca chegar à produção (ADR-0078, ADR-0089).

## Requirements

### Requirement: Carga dos dados de demonstração

O comando `npm run demo:data` MUST carregar, pela API indicada (padrão `http://localhost:5175`), as pessoas do diretório de demonstração com os papéis delas, clientes, projetos com gerente, líder técnico e membros, e os cronogramas com progresso, marcos atingidos e uma tarefa atrasada. Cada ação MUST ser feita por quem a faria na tela, e todo nome MUST ser fictício. Ao terminar, o comando MUST dizer para abrir as telas em `http://localhost:5173` (ADR-0002, ADR-0003, ADR-0089).

#### Scenario: Carregar dados de demonstração
- **WHEN** o comando roda numa instalação de desenvolvimento sem os dados
- **THEN** o administrador cria os clientes e os projetos, a gerente monta os cronogramas, cada responsável atualiza o progresso da própria tarefa, e o comando diz o que carregou e que as telas abrem em `http://localhost:5173`, com o `npm run dev`

#### Scenario: Dados já carregados
- **WHEN** o comando roda de novo e o projeto ERP-FIN já existe
- **THEN** o comando avisa que os dados já estão lá e onde abrir as telas, e não cria nem muda nada

#### Scenario: Sem entrada local
- **WHEN** o comando roda contra uma API com a entrada local desligada, como a de produção
- **THEN** o comando para com uma mensagem, sem criar nada

### Requirement: Datas da demonstração

As datas dos cronogramas MUST ser relativas ao dia em que o comando roda: o projeto principal começa na segunda-feira de duas semanas antes, para haver tarefas concluídas, uma atrasada, a linha de hoje e tarefas futuras. O servidor ajusta cada data aos dias úteis (ADR-0018, ADR-0089).

#### Scenario: Datas relativas a hoje
- **WHEN** o comando roda na segunda-feira 19/10/2026
- **THEN** a primeira tarefa do projeto principal começa em 05/10/2026, e a de um dia rodado uma semana depois começa uma semana depois

### Requirement: Linha de base na demonstração

O projeto principal MUST ter a linha de base salva pela gerente antes de uma tarefa ser adiada, para a diferença da linha de base e o caminho crítico aparecerem na demonstração (ADR-0021, ADR-0089).

#### Scenario: Linha de base na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a gerente salva a linha de base do ERP-FIN e depois adia "Parametrizar regras de ICMS", que empurra as seguintes e deixa o fim do projeto depois da linha de base

### Requirement: Modelos na demonstração

A carga de demonstração MUST deixar dois modelos de exemplo, fictícios, salvos pela gerente: "Implantação de ERP", do ERP-FIN, e "Revisão tributária de filial", do TRIB-RS (ADR-0017, ADR-0089).

#### Scenario: Modelos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a gerente salva os dois modelos, e o "Novo projeto" os oferece

### Requirement: Chamados na demonstração

A carga de demonstração MUST abrir chamados fictícios nos projetos, para os dois times, com prioridades diferentes e abertos por pessoas diferentes, entrando como cada uma delas (ADR-0089).

#### Scenario: Chamados na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados e a aba Chamados dos projetos mostram os chamados de exemplo, numerados em sequência

### Requirement: Conversa na demonstração

A carga de demonstração MUST deixar mensagens fictícias nos chamados de exemplo, de pessoas com papéis diferentes, com uma menção e um anexo, entrando como cada uma delas (ADR-0089).

#### Scenario: Conversa na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página do primeiro chamado mostra a conversa com as cores de cada papel, uma menção e um anexo

### Requirement: Status na demonstração

A carga de demonstração MUST deixar chamados de exemplo em status diferentes, com o responsável definido pela primeira resposta de quem atende, mudando o status pelas ações do fluxo, cada uma pela pessoa que pode (ADR-0024, ADR-0089).

#### Scenario: Status na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados mostra chamados Abertos, Em andamento, Aguardando solicitante e Resolvidos, com e sem responsável

### Requirement: Prazos na demonstração

A carga de demonstração MUST deixar chamados com prazos em dia, vencendo e pausados. Os chamados abrem no dia da carga, pela própria API, então nenhum nasce vencido (ADR-0025, ADR-0089).

#### Scenario: Prazos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados mostra prazos em dia, vencendo e pausados

### Requirement: Trocas na demonstração

A carga de demonstração MUST atribuir um chamado sem responsável pelo gerente e transferir um chamado para o outro time, cada um pela pessoa que pode (ADR-0068, ADR-0089).

#### Scenario: Trocas na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** um chamado aparece atribuído pelo gerente e outro transferido de time, com os eventos na conversa

### Requirement: Fornecedores na demonstração

A carga de demonstração MUST incluir dois fornecedores fictícios e registrar um chamado de exemplo num deles, com uma atualização de status (ADR-0002, ADR-0028, ADR-0089).

#### Scenario: Fornecedores na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a Administração mostra os dois fornecedores, e um chamado aparece na visão "Com fornecedor"
