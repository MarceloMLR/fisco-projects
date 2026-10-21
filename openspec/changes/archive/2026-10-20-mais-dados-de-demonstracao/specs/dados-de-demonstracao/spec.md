## MODIFIED Requirements

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

### Requirement: Fornecedores na demonstração

A carga de demonstração MUST incluir dois fornecedores fictícios e registrar dois chamados de exemplo neles, um com o link e várias atualizações de status, com mensagens entre elas (ADR-0002, ADR-0028, ADR-0089).

#### Scenario: Fornecedores na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a Administração mostra os dois fornecedores, e dois chamados aparecem na visão "Com fornecedor", um deles com as atualizações na conversa

## ADDED Requirements

### Requirement: Prioridade e acompanhamento na demonstração

A carga de demonstração MUST mudar a prioridade de um chamado de exemplo, pela gerente, e fazer o administrador acompanhar outro, com os eventos na conversa (ADR-0025, ADR-0026, ADR-0089).

#### Scenario: Prioridade e acompanhamento na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** o primeiro chamado mostra a troca de prioridade feita pela gerente, e o administrador aparece entre quem acompanha outro chamado
