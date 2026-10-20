# chamados Specification

## Purpose

Chamados dos projetos: o número único na instalação, a abertura para os times Funcional e Desenvolvimento, quem abre e quem vê, a lista com visões e filtros, a página do chamado e a auditoria (ADR-0022, ADR-0029, ADR-0044, ADR-0068).

## Requirements

### Requirement: Número do chamado

Cada chamado MUST receber um número sequencial único na instalação, como #104, que não muda e não se repete, mesmo com aberturas simultâneas. As telas mostram o número com o código do projeto ao lado (ADR-0029).

#### Scenario: Números em sequência
- **WHEN** duas pessoas abrem chamados em projetos diferentes, um depois do outro
- **THEN** o segundo recebe o número seguinte ao do primeiro

#### Scenario: Aberturas simultâneas
- **WHEN** dez chamados são abertos ao mesmo tempo
- **THEN** cada um recebe um número diferente, sem repetição

### Requirement: Abertura do chamado

Quem abre MUST informar o projeto, o time (Funcional ou Desenvolvimento), a prioridade (baixa, média, alta ou crítica), o título (até 200 caracteres) e a descrição (até 10.000). O chamado MUST nascer Aberto, sem responsável, com quem abriu como solicitante. Não existe chamado sem projeto nem campo de responsável na abertura (ADR-0022, ADR-0068).

#### Scenario: Abrir chamado
- **WHEN** uma colaboradora abre um chamado para o time de Desenvolvimento do projeto
- **THEN** o chamado é criado Aberto, sem responsável, com ela como solicitante

#### Scenario: Chamado sem projeto
- **WHEN** alguém tenta abrir um chamado sem projeto
- **THEN** a API responde 400 e nada é criado

#### Scenario: Campos obrigatórios do chamado
- **WHEN** alguém tenta abrir um chamado sem time, prioridade, título ou descrição
- **THEN** a API responde 400 com cada campo que falta

### Requirement: Quem abre chamados

O administrador, o gerente, o líder técnico, o colaborador e o desenvolvedor do projeto MUST poder abrir chamados nele. O leitor MUST receber 403, quem está fora do projeto 404, e o projeto concluído ou cancelado 409, até ser reaberto (ADR-0022, ADR-0044).

#### Scenario: Leitor não abre chamado
- **WHEN** um leitor do projeto tenta abrir um chamado
- **THEN** a API responde 403 e nada é criado

#### Scenario: Fora do projeto não abre chamado
- **WHEN** alguém que não está no projeto tenta abrir um chamado nele
- **THEN** a API responde 404

#### Scenario: Projeto encerrado não recebe chamado
- **WHEN** alguém tenta abrir um chamado num projeto concluído
- **THEN** a API responde 409, e depois de reaberto o projeto volta a receber chamados

### Requirement: Times dos chamados

Só os times Funcional e Desenvolvimento MUST receber chamados, sem filas. Funcional é atendido pelos colaboradores; Desenvolvimento, pelo líder técnico e pelos desenvolvedores. Um time sem ninguém para atender MUST ficar fora da abertura, e a API MUST recusar com 400 um chamado para ele (ADR-0068).

#### Scenario: Times na abertura
- **WHEN** um projeto tem desenvolvedores e nenhum colaborador
- **THEN** a abertura oferece só Desenvolvimento

#### Scenario: Time sem quem atenda
- **WHEN** alguém tenta abrir um chamado para o time Funcional de um projeto sem colaboradores
- **THEN** a API responde 400 no campo do time e nada é criado

### Requirement: Quem vê os chamados

Todos os membros do projeto, inclusive os leitores, MUST ver todos os chamados do projeto, e o administrador MUST ver os de todos os projetos. Quem está fora do projeto MUST não ver os chamados dele na lista e MUST receber 404 no chamado (ADR-0044).

#### Scenario: Leitor vê os chamados do projeto
- **WHEN** um leitor do projeto abre a lista de chamados
- **THEN** a lista traz todos os chamados do projeto, abertos por qualquer pessoa

#### Scenario: Fora do projeto não vê os chamados
- **WHEN** alguém que não está no projeto pede a lista ou um chamado dele
- **THEN** a lista não traz os chamados do projeto, e o chamado responde 404

#### Scenario: Administrador vê todos os chamados
- **WHEN** o administrador, sem estar no projeto, abre a lista
- **THEN** a lista traz os chamados de todos os projetos

### Requirement: Visões e filtros da lista

A lista MUST ter as visões "Atribuídos a mim", "Sem responsável nos meus times", "Abertos por mim", "Que acompanho" e "Todos", com a contagem dos chamados em aberto de cada uma, e os filtros por projeto, time e situação. A busca MUST achar pelo número, pelo título e pela descrição. A ordem MUST ser pela prioridade, a mais alta primeiro, ou pelo número. Cada chamado MUST mostrar o número de interações (ADR-0029, ADR-0068).

#### Scenario: Sem responsável nos meus times
- **WHEN** um desenvolvedor abre a visão "Sem responsável nos meus times" num projeto com chamados sem responsável para os dois times
- **THEN** a visão traz só os do time de Desenvolvimento, e a do gerente traz os dos dois times

#### Scenario: Abertos por mim
- **WHEN** uma colaboradora abre a visão "Abertos por mim"
- **THEN** a visão traz só os chamados que ela abriu

#### Scenario: Busca nos chamados
- **WHEN** alguém busca pelo número de um chamado, por uma palavra do título ou por uma palavra da descrição
- **THEN** a lista traz o chamado correspondente

#### Scenario: Filtros da lista
- **WHEN** alguém filtra por projeto, por time ou pelos encerrados
- **THEN** a lista traz só os chamados correspondentes, dentro do acesso da pessoa

#### Scenario: Interações na lista
- **WHEN** um chamado tem três mensagens
- **THEN** a lista mostra 3 nas interações dele

### Requirement: Página do chamado

O chamado MUST ter uma página no próprio número, com o código do projeto, o time, o título, o status, a prioridade, quem abriu e quando, a descrição e as pessoas: solicitante, responsável, gerente e líder técnico do projeto (ADR-0029, ADR-0044).

#### Scenario: Página do chamado
- **WHEN** um membro do projeto abre o chamado #1
- **THEN** a página mostra o cabeçalho, a descrição e as pessoas, com "Sem responsável"

### Requirement: Chamados pela tela

A página Chamados MUST ter as visões ao lado da lista, os filtros e "Abrir chamado". A aba Chamados do projeto MUST ter "Todos", "Em aberto", "Sem responsável" e "Abertos por mim". O "Abrir chamado" MUST aparecer só a quem abre, na página Chamados e na aba, com o projeto já preenchido; nunca na tela inicial (ADR-0068).

#### Scenario: Lista de chamados pela tela
- **WHEN** a gerente abre a página Chamados e escolhe "Sem responsável nos meus times"
- **THEN** a tabela traz os chamados sem responsável dos times dela, com a contagem na visão

#### Scenario: Abrir chamado pela aba do projeto
- **WHEN** uma colaboradora escolhe "Abrir chamado" na aba Chamados do projeto, escolhe o time, digita o título e a descrição e confirma
- **THEN** o projeto já vem preenchido, não há campo de responsável, e a página do chamado novo abre com o número

#### Scenario: Leitor sem abrir chamado pela tela
- **WHEN** um leitor abre a aba Chamados do projeto
- **THEN** a aba mostra os chamados, sem "Abrir chamado"

### Requirement: Chamados na auditoria

A abertura de um chamado MUST entrar na auditoria como "Chamado", identificada pelo título, com o número entre os campos, no contexto do projeto (ADR-0039).

#### Scenario: Abertura na auditoria
- **WHEN** alguém abre um chamado
- **THEN** a auditoria registra a criação do chamado pelo título, com o número, no projeto dele

### Requirement: Mensagens do chamado

O administrador, o gerente, o líder técnico, quem atende o time do chamado e o solicitante MUST poder mandar mensagens com negrito, listas e links, visíveis a todos que veem o chamado. Os outros membros MUST receber 403, e ninguém manda mensagem num chamado Fechado (409). Mensagens não se editam nem se apagam, e cada uma atualiza o chamado (ADR-0024, ADR-0026, ADR-0044).

#### Scenario: Mandar mensagem
- **WHEN** o solicitante manda uma mensagem com um trecho em negrito
- **THEN** a mensagem aparece na conversa para todos que veem o chamado, com o negrito, e o chamado fica atualizado

#### Scenario: Leitor não manda mensagem
- **WHEN** um leitor do projeto tenta mandar uma mensagem
- **THEN** a API responde 403 e nada é gravado

#### Scenario: Texto formatado inválido
- **WHEN** alguém manda uma mensagem vazia ou com um elemento fora do negrito, das listas, dos links e das menções
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Chamado fechado sem mensagens
- **WHEN** alguém tenta mandar uma mensagem num chamado Fechado
- **THEN** a API responde 409 e nada é gravado

### Requirement: Menções no chamado

Quem manda mensagem MUST poder mencionar os gestores do projeto e os envolvidos no chamado: o solicitante, quem atende o time e o responsável. Menção a outra pessoa MUST receber 400. Quem é mencionado MUST passar a acompanhar o chamado (ADR-0026).

#### Scenario: Mencionar quem atende
- **WHEN** a solicitante menciona um desenvolvedor num chamado de Desenvolvimento
- **THEN** a mensagem é gravada e o desenvolvedor passa a acompanhar o chamado

#### Scenario: Menção fora dos envolvidos
- **WHEN** alguém menciona um colaborador num chamado de Desenvolvimento
- **THEN** a API responde 400 e nada é gravado

### Requirement: Acompanhar o chamado

O solicitante MUST acompanhar o chamado desde a abertura. Qualquer pessoa que vê o chamado MUST poder acompanhar e deixar de acompanhar. A lista MUST ter a visão "Que acompanho", com a contagem (ADR-0026, ADR-0068).

#### Scenario: Acompanhar um chamado
- **WHEN** um leitor do projeto escolhe acompanhar um chamado
- **THEN** o chamado entra na visão "Que acompanho" dele, e ele aparece em "Acompanham"

#### Scenario: Deixar de acompanhar
- **WHEN** quem acompanha escolhe deixar de acompanhar
- **THEN** o chamado sai da visão "Que acompanho" dessa pessoa

### Requirement: Anexos do chamado

Quem manda mensagem MUST poder anexar vários arquivos a ela, e o solicitante, à descrição logo depois de abrir o chamado. Cada arquivo MUST ter até 15 MB (configurável). Executáveis e scripts MUST ser recusados com 400. Os arquivos ficam na pasta do chamado e só quem vê o chamado MUST conseguir abri-los; imagens e PDF abrem no navegador (ADR-0026, ADR-0030).

#### Scenario: Anexar arquivos a uma mensagem
- **WHEN** alguém manda uma mensagem com uma imagem e um XML
- **THEN** os dois ficam na mensagem e em "Anexos da conversa", e a imagem abre no navegador

#### Scenario: Anexo recusado
- **WHEN** alguém anexa um arquivo acima do limite ou um executável
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Anexo fora do acesso
- **WHEN** alguém que não está no projeto pede um anexo de um chamado dele
- **THEN** a API responde 404

### Requirement: Conversa pela tela

A página do chamado MUST mostrar a descrição no topo e a conversa em duas colunas, com o solicitante à esquerda e os demais à direita. Cada mensagem MUST ter a cor e o selo do papel de quem escreveu e "(você)" na própria pessoa. A conversa MUST rolar por dentro, com a resposta fixa embaixo, o separador por dia e as interações recolhidas acima de 10 (ADR-0069).

#### Scenario: Cores e lados por papel
- **WHEN** a gerente abre um chamado com mensagens do solicitante, do líder técnico e dela
- **THEN** a do solicitante fica à esquerda e as outras à direita, cada uma com o selo e a cor do papel, e a dela com "(você)"

#### Scenario: Interações antigas recolhidas
- **WHEN** um chamado tem 14 mensagens
- **THEN** a conversa mostra as 10 últimas e "Mostrar 4 interações anteriores", que mostra as outras

#### Scenario: Responder pela tela
- **WHEN** quem atende digita uma resposta com uma menção e um anexo e escolhe "Enviar"
- **THEN** a mensagem aparece no fim da conversa, e o anexo, em "Anexos da conversa"

#### Scenario: Sem resposta para quem só vê
- **WHEN** um leitor abre o chamado
- **THEN** a conversa aparece sem a caixa de resposta, com "Acompanhar" no painel

### Requirement: Fluxo de status em tabela

Os status e as transições MUST vir de uma tabela, com o fluxo do MVP: a ação, o status de origem e o de destino, quem pode e se exige mensagem. O servidor MUST conferir a integridade do fluxo lido: todo status, menos Fechado, tem saída, e nenhuma ação se repete no mesmo status. Não existe caminho para reabrir um chamado Fechado (ADR-0024).

#### Scenario: Fluxo padrão
- **WHEN** alguém pede o fluxo de chamados
- **THEN** a API devolve as transições do MVP, de Aberto a Fechado, com quem pode e se exige mensagem, e nenhuma sai de Fechado

#### Scenario: Fluxo quebrado
- **WHEN** o fluxo lido tem um status sem saída, além de Fechado
- **THEN** o fluxo é recusado como inválido

### Requirement: Mudar o status

Cada ação do fluxo MUST ser feita só por quem pode: "quem atende" é o administrador, os gestores e quem atende o time; "solicitante" é quem abriu o chamado; "gestor" é o gerente e o líder técnico. A ação que exige mensagem MUST receber o texto, gravado como mensagem. Ação de quem não pode MUST receber 403, e ação que não sai do status atual, 409 (ADR-0024, ADR-0044).

#### Scenario: Resolver o chamado
- **WHEN** quem atende marca como resolvido um chamado Em andamento
- **THEN** o chamado fica Resolvido, e a conversa registra a mudança

#### Scenario: Pedir informações sem mensagem
- **WHEN** quem atende pede informações sem escrever a mensagem
- **THEN** a API responde 400 e o status não muda

#### Scenario: Ação de quem não pode
- **WHEN** o solicitante tenta marcar o próprio chamado como resolvido
- **THEN** a API responde 403 e o status não muda

#### Scenario: Ação fora do status
- **WHEN** alguém tenta confirmar e fechar um chamado Aberto
- **THEN** a API responde 409 e o status não muda

#### Scenario: Cancelar o chamado
- **WHEN** o solicitante cancela, com uma mensagem, o próprio chamado Aberto
- **THEN** o chamado fica Fechado, e ninguém manda mais mensagens nele

### Requirement: Primeira resposta

A primeira mensagem de alguém que atende o time num chamado sem responsável MUST torná-lo responsável e fazê-lo acompanhar o chamado; num chamado Aberto, o status MUST ir a Em andamento. Respostas do solicitante, do gerente e de quem só acompanha MUST não definir responsável, e uma resposta num chamado com responsável nunca o troca. A resposta do solicitante num chamado Aguardando solicitante MUST levá-lo a Em andamento (ADR-0024, ADR-0068).

#### Scenario: Primeira resposta define o responsável
- **WHEN** um desenvolvedor responde a um chamado Aberto de Desenvolvimento sem responsável
- **THEN** ele passa a ser o responsável e a acompanhar o chamado, e o chamado fica Em andamento

#### Scenario: Resposta que não define responsável
- **WHEN** o gerente e depois o solicitante respondem a um chamado sem responsável
- **THEN** o chamado continua sem responsável e Aberto

#### Scenario: Responsável não muda
- **WHEN** o líder técnico responde a um chamado que já tem um desenvolvedor como responsável
- **THEN** o responsável continua o mesmo

#### Scenario: Resposta do solicitante retoma o chamado
- **WHEN** o solicitante responde a um chamado Aguardando solicitante
- **THEN** o chamado volta a Em andamento

### Requirement: Fechamento automático

Um chamado Resolvido sem retorno do solicitante MUST fechar sozinho depois de 5 dias úteis do calendário da instalação, valor configurável em `Tickets:AutoCloseBusinessDays`, com o sistema como autor da mudança (ADR-0024).

#### Scenario: Fechar resolvidos parados
- **WHEN** passam 5 dias úteis desde que um chamado foi resolvido, sem resposta
- **THEN** o chamado fica Fechado, e a conversa registra o fechamento automático

### Requirement: Histórico na conversa

As mudanças de status e a definição do responsável MUST aparecer na conversa como eventos, com quem fez e quando, numa linha discreta. A conversa MUST ter o filtro "Tudo", "Mensagens" e "Histórico" (ADR-0069).

#### Scenario: Filtrar o histórico
- **WHEN** alguém escolhe "Histórico" num chamado com mensagens e mudanças de status
- **THEN** a conversa mostra só os eventos, e "Mensagens" mostra só as mensagens

### Requirement: Status pela tela

O cabeçalho do chamado MUST mostrar a ação principal que a pessoa pode fazer e as outras no "⋮", só as do status atual. A ação que exige mensagem MUST abrir um diálogo para o texto. A página "Fluxo de chamados" MUST mostrar as transições lidas da tabela (ADR-0024).

#### Scenario: Resolver pela tela
- **WHEN** quem atende escolhe "Marcar como resolvido" no cabeçalho
- **THEN** o chamado mostra Resolvido, e a conversa, o evento

#### Scenario: Pedir informações pela tela
- **WHEN** quem atende escolhe "Pedir informações" no "⋮", digita a mensagem e confirma
- **THEN** o chamado mostra Aguardando solicitante, com a mensagem na conversa

#### Scenario: Página do fluxo
- **WHEN** alguém abre "Fluxo de chamados" na página Chamados
- **THEN** a página mostra cada transição com a ação, quem pode e se exige mensagem
