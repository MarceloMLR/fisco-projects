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
- **THEN** o comando avisa que os dados já estão lá, diz como apagar o banco local e os arquivos para carregar de novo e onde abrir as telas, e não cria nem muda nada

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

A carga de demonstração MUST abrir chamados fictícios em todos os projetos, para os dois times, com prioridades diferentes e abertos por pessoas diferentes, entrando como cada uma delas; no projeto concluído, os chamados MUST terminar fechados ou cancelados antes da conclusão (ADR-0043, ADR-0089).

#### Scenario: Chamados na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados e a aba Chamados de cada projeto mostram os chamados de exemplo, numerados em sequência

### Requirement: Conversa na demonstração

A carga de demonstração MUST deixar conversas fictícias nos chamados de exemplo, várias com mais de uma mensagem, de pessoas com papéis diferentes, com menções e anexos em XML, PDF, imagem e planilha, inclusive na descrição, todos feitos pelo comando, entrando como cada pessoa (ADR-0026, ADR-0069, ADR-0089).

#### Scenario: Conversa na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página do primeiro chamado mostra uma conversa longa com as cores de cada papel, menções e anexos de tipos diferentes, e outro chamado mostra o anexo da descrição

### Requirement: Status na demonstração

A carga de demonstração MUST deixar chamados de exemplo em todos os status do fluxo e um chamado reaberto, com o responsável definido pela primeira resposta de quem atende, mudando o status pelas ações do fluxo, cada uma pela pessoa que pode, na ordem em que aconteceriam (ADR-0024, ADR-0089).

#### Scenario: Status na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados mostra chamados Abertos, Em triagem, Em andamento, Aguardando solicitante, Resolvidos, Fechados e Cancelados, com e sem responsável, e um chamado reaberto com o evento na conversa

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

A carga de demonstração MUST incluir dois fornecedores fictícios e registrar dois chamados de exemplo neles, um com o link e várias atualizações de status, com mensagens entre elas (ADR-0002, ADR-0028, ADR-0089).

#### Scenario: Fornecedores na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a Administração mostra os dois fornecedores, e dois chamados aparecem na visão "Com fornecedor", um deles com as atualizações na conversa

### Requirement: Documentos na demonstração

A carga de demonstração MUST enviar documentos fictícios para as pastas do projeto principal, por quem os enviaria na tela, com uma nova versão, status diferentes, um documento obsoleto e vínculos com tarefas do cronograma (ADR-0002, ADR-0031, ADR-0089).

#### Scenario: Documentos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Documentos do ERP-FIN mostra documentos em mais de uma pasta, um deles na versão 2, aprovados, em revisão e em rascunho, vinculados a tarefas, e um obsoleto que só aparece com o filtro "Todos"

### Requirement: Links na demonstração

A carga de demonstração MUST incluir links fictícios do projeto principal em quatro categorias, cada um incluído por uma pessoa do projeto que cadastra links (ADR-0002, ADR-0032, ADR-0089).

#### Scenario: Links na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Links do ERP-FIN mostra links em Ambientes, Sistemas, Painéis e Repositórios, incluídos por pessoas diferentes

### Requirement: Acessos na demonstração

A carga de demonstração MUST incluir acessos fictícios do projeto principal em três ambientes, cada um incluído por uma pessoa do projeto que cadastra acessos, com senhas fictícias (ADR-0002, ADR-0034, ADR-0089).

#### Scenario: Acessos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Acessos do ERP-FIN mostra acessos em Produção, Homologação e Fornecedores, com as senhas mascaradas

### Requirement: Termos na demonstração

A carga de demonstração MUST incluir termos de aceite do projeto principal: um assinado pela pessoa do pedido, um anexado pela gerente, um recusado e um pendente, com PDFs fictícios feitos pelo comando (ADR-0002, ADR-0035, ADR-0089).

#### Scenario: Termos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Termos de aceite do ERP-FIN mostra termos assinados, recusado e pendente, e a pasta de termos tem os termos assinados

### Requirement: Meu trabalho na demonstração

A carga de demonstração MUST deixar itens na fila das pessoas da demonstração, com datas relativas ao dia da carga: tarefas atrasadas, de hoje e da semana, chamados aguardando o solicitante e sem responsável, e um pedido de aceite pendente (ADR-0070, ADR-0089).

#### Scenario: Meu trabalho na demonstração
- **WHEN** o comando carrega os dados de demonstração e Marina Costa entra
- **THEN** o Meu trabalho de Marina mostra itens em Atrasado, Aguardando você e nos próximos dias, os projetos dela e os próximos marcos

### Requirement: Prioridade e acompanhamento na demonstração

A carga de demonstração MUST mudar a prioridade de um chamado de exemplo, pela gerente, e fazer o administrador acompanhar outro, com os eventos na conversa (ADR-0025, ADR-0026, ADR-0089).

#### Scenario: Prioridade e acompanhamento na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** o primeiro chamado mostra a troca de prioridade feita pela gerente, e o administrador aparece entre quem acompanha outro chamado
