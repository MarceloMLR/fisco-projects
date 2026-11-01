# dados-de-demonstracao Specification

## Purpose

Define os dados fictícios de demonstração do desenvolvimento: um comando que, pela própria API e pela entrada local, carrega pessoas, clientes, projetos, membros e cronogramas com datas relativas ao dia em que roda, sem nunca chegar à produção (ADR-0078, ADR-0089).

## Requirements

### Requirement: Carga dos dados de demonstração

O comando `npm run demo:data` MUST carregar, pela API indicada (padrão `http://localhost:5175`), as pessoas do diretório de demonstração com os papéis delas, clientes, projetos com gerente, líder técnico e membros, e os cronogramas, cada ação por quem a faria na tela e todo nome fictício. Projetos que já existem MUST ficar como estão: o comando carrega só os que faltam. Ao terminar, MUST dizer para abrir as telas em `http://localhost:5173` (ADR-0002, ADR-0003, ADR-0089).

#### Scenario: Carregar dados de demonstração
- **WHEN** o comando roda numa instalação de desenvolvimento sem os dados
- **THEN** o administrador cria os clientes e os projetos, a gerente de cada projeto monta o cronograma, cada responsável atualiza o progresso da própria tarefa, e o comando diz o que carregou e que as telas abrem em `http://localhost:5173`, com o `npm run dev`

#### Scenario: Só os dados que faltam
- **WHEN** o comando roda numa demonstração que já tem parte dos projetos do plano
- **THEN** o comando carrega só os projetos que faltam, com os clientes e fornecedores que faltam, e diz quais ficaram como estavam

#### Scenario: Categoria, pasta ou modelo desativado
- **WHEN** o administrador desativou uma categoria de links, uma pasta padrão ou um modelo que o plano usa
- **THEN** o link vai para a categoria Outros (ou a primeira ativa), o documento para a primeira pasta que não é a de termos e o projeto do modelo nasce em branco, e o comando diz cada troca

#### Scenario: Dados já carregados
- **WHEN** o comando roda de novo e todos os projetos do plano já existem
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

A carga de demonstração MUST incluir termos de aceite do projeto principal pelo provedor local, com signatários fictícios de fora da empresa: um assinado por dois signatários, um anexado pela gerente, um recusado com o motivo e um pendente com um de dois signatários já assinado, com PDFs fictícios feitos pelo comando (ADR-0002, ADR-0089, ADR-0129).

#### Scenario: Termos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Termos de aceite do ERP-FIN mostra termos assinados, recusado e pendente, a gaveta do pendente mostra um signatário assinado e o link local do outro, e a pasta de termos tem os termos assinados

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

### Requirement: Atendimento iniciado na demonstração

A carga de demonstração MUST levar um chamado sem responsável à triagem e depois ao atendimento pelas ações do fluxo, por quem atende o time, que passa a ser o responsável (ADR-0089, ADR-0098).

#### Scenario: Atendimento iniciado na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** um chamado aparece Em andamento, com quem iniciou o atendimento como responsável e o evento na conversa

### Requirement: Demonstração mais rica

A carga de demonstração MUST incluir seis clientes e projetos em todos os status (Planejamento, Em andamento, Pausado, Concluído e Cancelado), um deles interno e um criado a partir de um modelo, projetos de uma segunda gerente e chamados, documentos, links, acessos e termos de aceite também fora do projeto principal (ADR-0017, ADR-0043, ADR-0089).

#### Scenario: Projetos em todos os status
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Projetos mostra projetos em Planejamento, Em andamento, Pausado, Concluído e Cancelado, de seis clientes e internos, todos com chamados

#### Scenario: Projeto a partir de um modelo
- **WHEN** o comando carrega os dados de demonstração
- **THEN** o administrador cria um projeto a partir do modelo "Implantação de ERP", com o cronograma gerado pelo servidor a partir do dia escolhido

#### Scenario: Projeto cancelado com chamados
- **WHEN** o comando cancela um projeto com chamados ainda abertos
- **THEN** os chamados ficam fechados com o projeto, com o evento na conversa

#### Scenario: Segunda gerente
- **WHEN** o comando carrega os dados de demonstração e Beatriz Lima entra
- **THEN** os projetos dela aparecem com os cronogramas montados por ela, e o Meu trabalho dela mostra uma tarefa atrasada, uma da semana e um chamado aguardando a resposta dela

### Requirement: Demonstração no Codespaces

Um Codespace criado do repositório MUST se preparar sozinho, com o SDK do .NET e o Node das versões fixadas, o banco local com uma senha só dele, os pacotes e as telas compiladas. Ao ser aberto, MUST subir o banco e a API com as telas na porta 5175, carregar os dados de demonstração que faltam, tornar o endereço público e mostrá-lo com as pessoas para entrar, sem pedir nenhum segredo além da senha do banco que ele mesmo cria (ADR-0089, ADR-0113, ADR-0134).

#### Scenario: Demonstração no Codespaces
- **WHEN** Marcelo cria um Codespace do repositório e o abre
- **THEN** em alguns minutos a API está no ar com os dados de demonstração, e o terminal mostra o endereço público e as pessoas para entrar

#### Scenario: Codespace aberto de novo
- **WHEN** Marcelo abre de novo um Codespace com a demonstração já no ar
- **THEN** o terminal mostra o endereço, sem subir uma segunda API

#### Scenario: Versões do Codespace
- **WHEN** a versão do SDK do .NET, a do Node ou a porta da API mudam
- **THEN** o Codespace usa as mesmas do `global.json`, do `.nvmrc` e do `launchSettings.json`, e a verificação reprova a configuração que ficar diferente

### Requirement: Horas na demonstração

A carga de demonstração MUST incluir horas apontadas nas duas últimas semanas, cada apontamento pela pessoa que o fez, em mais de um projeto, com apontamentos só no projeto, em tarefas e em chamados, sem passar de 24 horas por pessoa num dia (ADR-0089, ADR-0128).

#### Scenario: Horas na demonstração
- **WHEN** o comando carrega os dados de demonstração e a gerente do projeto principal abre Minhas horas
- **THEN** a semana atual e a anterior mostram horas dela no projeto principal e em outro projeto, com tarefas e chamados

### Requirement: Saldo na demonstração

A carga de demonstração MUST incluir horas contratadas e horas adicionais, cadastradas pelo administrador, de modo que Horas dos projetos mostre um projeto acima do contratado, um perto do limite, um dentro e um sem horas contratadas (ADR-0089, ADR-0128).

#### Scenario: Saldo na demonstração
- **WHEN** o comando carrega os dados de demonstração e o administrador abre Horas dos projetos
- **THEN** a visão Em aberto mostra os quatro estados do saldo, e o projeto principal tem horas adicionais com o motivo

### Requirement: Projeto importado na demonstração

A carga de demonstração MUST criar um projeto fictício a partir do XML do MS Project que a exportação grava de outro projeto da demonstração, pela prévia e pela criação da API (ADR-0089, ADR-0135).

#### Scenario: Projeto importado na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a gerente exporta o cronograma de um projeto, cria um projeto novo a partir do arquivo, ligando os recursos às pessoas da demonstração, e ele nasce com as mesmas fases, itens, progresso e responsáveis

### Requirement: Horas previstas na demonstração

A carga de demonstração MUST pôr, pelo administrador, horas previstas em tarefas do projeto principal com horas apontadas, de modo que o cronograma e Horas dos projetos mostrem uma tarefa acima das previstas, uma perto das previstas e uma dentro (ADR-0089, ADR-0138).

#### Scenario: Horas previstas na demonstração
- **WHEN** o comando carrega os dados de demonstração e o administrador abre o detalhe do projeto principal em Horas dos projetos
- **THEN** a seção "Por tarefa" mostra uma tarefa acima das previstas, uma perto e uma dentro

### Requirement: Notas na demonstração

A carga de demonstração MUST escrever, pela gerente, notas em itens do projeto principal, como a tarefa que espera os arquivos do cliente (ADR-0089, ADR-0139).

#### Scenario: Notas na demonstração
- **WHEN** o comando carrega os dados de demonstração e alguém abre a Lista do projeto principal
- **THEN** pelo menos uma tarefa e um marco mostram o ícone de nota

### Requirement: Ausências na demonstração

A carga de demonstração MUST incluir, pela gerente, ausências de pessoas do projeto principal, uma delas durante uma tarefa em aberto do responsável (ADR-0089, ADR-0141).

#### Scenario: Ausências na demonstração
- **WHEN** o comando carrega os dados de demonstração e alguém abre o cronograma do projeto principal
- **THEN** pelo menos uma tarefa mostra o aviso do responsável ausente, e Membros mostra a ausência

### Requirement: Carga na demonstração

A carga de demonstração MUST incluir horas previstas em tarefas de mais de um projeto de uma mesma pessoa, de modo que a Carga da equipe mostre uma pessoa acima da capacidade numa semana de ausência dela (ADR-0089, ADR-0142).

#### Scenario: Carga na demonstração
- **WHEN** o comando carrega os dados de demonstração e o administrador abre Carga da equipe
- **THEN** pelo menos uma pessoa está acima da capacidade numa semana com ausência, com tarefas de dois projetos
