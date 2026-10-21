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

A lista MUST ter as visões "Atribuídos a mim", "Sem responsável nos meus times", "Abertos por mim", "Que acompanho" e "Todos", com a contagem dos chamados em aberto de cada uma, e os filtros por projeto, time, situação e pessoa (responsável ou solicitante). A busca MUST achar pelo número, pelo título e pela descrição. A ordem MUST ser pela prioridade, a mais alta primeiro, ou pelo número. Cada chamado MUST mostrar o número de interações (ADR-0027, ADR-0029, ADR-0068).

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

#### Scenario: Chamados de uma pessoa
- **WHEN** o gerente filtra a lista por um desenvolvedor
- **THEN** a lista traz os chamados de que o desenvolvedor é responsável ou solicitante

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

Quem manda mensagem MUST poder anexar vários arquivos a ela, e o solicitante, à descrição logo depois de abrir o chamado. Cada arquivo MUST ter até 15 MB (configurável). Executáveis e scripts MUST ser recusados com 400. Os arquivos ficam na pasta do chamado e só quem vê o chamado MUST conseguir abri-los e baixá-los; imagens e PDF abrem no navegador, e MUST poder ser baixados com o nome original. Todos os anexos de um chamado MUST poder ser baixados de uma vez, num `.zip` (ADR-0026, ADR-0030).

#### Scenario: Anexar arquivos a uma mensagem
- **WHEN** alguém manda uma mensagem com uma imagem e um XML
- **THEN** os dois ficam na mensagem e em "Anexos da conversa", e a imagem abre no navegador

#### Scenario: Anexo recusado
- **WHEN** alguém anexa um arquivo acima do limite ou um executável
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Anexo fora do acesso
- **WHEN** alguém que não está no projeto pede um anexo de um chamado dele
- **THEN** a API responde 404

#### Scenario: Baixar um anexo
- **WHEN** quem vê o chamado pede uma imagem anexada como download
- **THEN** a API entrega o arquivo como download, com o nome original

#### Scenario: Baixar todos os anexos
- **WHEN** quem vê o chamado pede todos os anexos de um chamado com dois arquivos de mesmo nome e um PDF
- **THEN** a API entrega um `.zip` com os três, na ordem da lista de anexos, cada um com o nome que tem no chamado

#### Scenario: Baixar todos sem anexos ou sem acesso
- **WHEN** alguém pede todos os anexos de um chamado sem anexos, ou de um chamado que não vê
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

### Requirement: Prazos da instalação

Os prazos de primeira resposta e de resolução de cada prioridade MUST vir de uma tabela, com os padrões do ADR-0025 (primeira resposta de 1 dia útil; resolução de 10, 5, 2 e 1 dias úteis). Só o administrador MUST ver e mudar os prazos, de 1 a 365 dias úteis, com a mudança na auditoria; os valores novos MUST valer para os chamados abertos depois (ADR-0025, ADR-0046).

#### Scenario: Prazos padrão
- **WHEN** o administrador abre "Prazos dos chamados" numa instalação nova
- **THEN** a primeira resposta é de 1 dia útil em todas as prioridades, e a resolução é de 10, 5, 2 e 1 dias úteis

#### Scenario: Mudar os prazos
- **WHEN** o administrador muda a resolução da prioridade média para 3 dias úteis
- **THEN** um chamado de prioridade média aberto depois vence em 3 dias úteis, e o aberto antes continua com 5

#### Scenario: Prazo fora do limite
- **WHEN** o administrador grava um prazo de 0 dias úteis
- **THEN** a API responde 400 e nada muda

#### Scenario: Prazos só do administrador
- **WHEN** um gerente de projeto pede os prazos da Administração
- **THEN** a API responde 403

### Requirement: Prazos do chamado

Cada chamado MUST vencer em dias úteis do calendário da instalação, contados da data de abertura no fuso da instalação, pulando fins de semana e feriados: a primeira resposta e a resolução, com os dias da prioridade. A primeira resposta MUST ser cumprida pela primeira mensagem de quem atende o time do chamado, nunca pela do solicitante (ADR-0025, ADR-0068).

#### Scenario: Prazo em dias úteis
- **WHEN** um chamado de prioridade alta é aberto numa sexta-feira
- **THEN** a resolução vence na terça-feira seguinte, e a primeira resposta, na segunda

#### Scenario: Primeira resposta cumprida
- **WHEN** o solicitante e depois um desenvolvedor respondem a um chamado de Desenvolvimento
- **THEN** a primeira resposta fica cumprida na hora da mensagem do desenvolvedor

### Requirement: Prazo pausado

O prazo de resolução MUST parar enquanto o chamado está Aguardando solicitante e, quando o chamado sai desse status, MUST ser estendido pelos dias úteis pausados (ADR-0025).

#### Scenario: Pausa estende o prazo
- **WHEN** um chamado fica Aguardando solicitante de segunda a quarta-feira
- **THEN** enquanto aguarda, o prazo aparece pausado, e depois da resposta a resolução vence dois dias úteis mais tarde

### Requirement: Mudar a prioridade

Quem atende MUST poder mudar a prioridade de um chamado em aberto; os prazos MUST ser recalculados a partir da abertura com os dias da nova prioridade, e a conversa MUST registrar a mudança. Os outros MUST receber 403 (ADR-0025, ADR-0044).

#### Scenario: Subir a prioridade
- **WHEN** o gerente muda a prioridade de um chamado de média para alta
- **THEN** a resolução vence em 2 dias úteis contados da abertura, e a conversa mostra a mudança

#### Scenario: Solicitante não muda a prioridade
- **WHEN** o solicitante tenta mudar a prioridade do próprio chamado
- **THEN** a API responde 403 e nada muda

### Requirement: Prazos pela tela

O cabeçalho do chamado MUST mostrar o prazo de resolução com ícone, cor e texto (em dia, vencendo hoje, vencido ou pausado); o painel MUST mostrar a seção Prazos, com os dias úteis usados, a resolução, a primeira resposta e o tempo pausado; a lista MUST mostrar o prazo embaixo do status; "Mudar prioridade" MUST ficar no "⋮" de quem atende; e a Administração MUST ter "Prazos dos chamados" (ADR-0025, ADR-0065).

#### Scenario: Prazo vencido na lista
- **WHEN** a lista mostra um chamado cuja resolução venceu há dois dias úteis
- **THEN** a linha mostra "Venceu há 2 dias úteis" em vermelho, com o ícone de alerta, embaixo do status

#### Scenario: Prazos no chamado
- **WHEN** alguém abre um chamado Aguardando solicitante
- **THEN** o cabeçalho mostra "Prazo pausado", e o painel mostra a resolução, a primeira resposta e o tempo pausado

#### Scenario: Mudar a prioridade pela tela
- **WHEN** quem atende escolhe "Mudar prioridade" no "⋮", escolhe Alta e confirma
- **THEN** o chamado mostra a prioridade Alta e o novo prazo

#### Scenario: Prazos na Administração
- **WHEN** o administrador muda um prazo em "Prazos dos chamados" e salva
- **THEN** a tela confirma que os prazos foram salvos e valem para os chamados novos

### Requirement: Trocar o responsável

Só o administrador, o gerente e o líder técnico MUST trocar o responsável de chamados em aberto, um a um ou em lote. O novo responsável MUST ser alguém do projeto que atende o time do chamado ou o gerente do projeto; passa a acompanhar o chamado, e a conversa registra a troca. Os outros MUST receber 403, e um responsável que não serve, 400 (ADR-0027, ADR-0044, ADR-0068).

#### Scenario: Atribuir um chamado
- **WHEN** o gerente atribui a um desenvolvedor um chamado de Desenvolvimento sem responsável
- **THEN** o desenvolvedor passa a ser o responsável e a acompanhar o chamado, e a conversa mostra a troca

#### Scenario: Atribuir em lote
- **WHEN** o líder técnico marca três chamados de Desenvolvimento e os atribui a um desenvolvedor
- **THEN** os três passam a ter o desenvolvedor como responsável

#### Scenario: Responsável fora do time
- **WHEN** o gerente tenta atribuir um chamado de Desenvolvimento a uma colaboradora do Funcional
- **THEN** a API responde 400 e o responsável não muda

#### Scenario: Quem atende não troca o responsável
- **WHEN** um desenvolvedor tenta atribuir um chamado a outro desenvolvedor
- **THEN** a API responde 403 e o responsável não muda

#### Scenario: Opções de responsável
- **WHEN** alguém pede as opções de responsável de chamados dos dois times do mesmo projeto
- **THEN** a API devolve só o gerente do projeto, que serve para os dois

### Requirement: Trocar o solicitante

O administrador e os gestores MUST trocar o solicitante de chamados não fechados, um a um ou em lote, e o próprio solicitante MUST trocar o dos chamados dele, um a um. O novo solicitante MUST ser alguém do projeto que abre chamados; o anterior continua acompanhando, e a conversa registra a troca (ADR-0027, ADR-0044).

#### Scenario: Solicitante passa o chamado
- **WHEN** a solicitante passa o próprio chamado para outra colaboradora
- **THEN** a outra colaboradora passa a ser a solicitante, a anterior continua acompanhando, e a conversa mostra a troca

#### Scenario: Solicitante não troca em lote
- **WHEN** a solicitante tenta trocar o solicitante de dois chamados de uma vez
- **THEN** a API responde 403 e nada muda

#### Scenario: Solicitante leitor
- **WHEN** o gerente tenta passar um chamado para um leitor do projeto
- **THEN** a API responde 400 e o solicitante não muda

### Requirement: Transferir de time

Quem atende o chamado e os gestores MUST transferir um chamado em aberto para o outro time, se ele tem alguém para atender. O chamado MUST ficar sem responsável, e a primeira resposta de alguém do novo time define o próximo. A conversa registra a transferência (ADR-0044, ADR-0068).

#### Scenario: Transferir para o Funcional
- **WHEN** o desenvolvedor responsável transfere um chamado de Desenvolvimento para o Funcional
- **THEN** o chamado fica no Funcional e sem responsável, e a primeira resposta de uma colaboradora a torna responsável

#### Scenario: Time sem ninguém
- **WHEN** alguém tenta transferir um chamado para um time sem ninguém no projeto
- **THEN** a API responde 400 e o time não muda

### Requirement: Chamados de quem sai

Quando alguém sai do projeto, ou muda para um papel que não pode atender o time de um chamado em aberto do qual é responsável, o chamado MUST ficar sem responsável, e a conversa registra o motivo. Os chamados de que a pessoa é solicitante continuam com ela (ADR-0042, ADR-0068).

#### Scenario: Responsável sai do projeto
- **WHEN** o gerente remove do projeto o desenvolvedor responsável por dois chamados
- **THEN** os dois chamados ficam sem responsável, e a conversa mostra o motivo

#### Scenario: Responsável muda de time
- **WHEN** o gerente troca para colaborador o papel do desenvolvedor responsável por um chamado de Desenvolvimento
- **THEN** o chamado fica sem responsável

### Requirement: Trocas pela tela

A lista MUST permitir marcar os chamados em que a pessoa pode trocar o responsável ou o solicitante, com a barra "N selecionados", "Atribuir a…", "Trocar solicitante" e "Limpar seleção", e o link "Atribuir" embaixo de "Sem responsável". O "⋮" do chamado MUST ter "Trocar responsável", "Trocar solicitante" e "Transferir para …", só para quem pode (ADR-0027, ADR-0068).

#### Scenario: Atribuir em lote pela tela
- **WHEN** o gerente marca dois chamados, escolhe "Atribuir a…", escolhe um desenvolvedor e confirma
- **THEN** os dois chamados mostram o desenvolvedor como responsável e a seleção é limpa

#### Scenario: Transferir pela tela
- **WHEN** quem atende escolhe "Transferir para Funcional" no "⋮" e confirma
- **THEN** o chamado mostra o time Funcional e "Sem responsável"

### Requirement: Fornecedores

O administrador MUST incluir, editar, desativar e reativar fornecedores, com nome único e o endereço do portal de suporte opcional (http ou https). Fornecedor inativo MUST não aparecer para novos registros. Nenhum fornecedor fica fixo no código, e as mudanças entram na auditoria (ADR-0002, ADR-0028, ADR-0046).

#### Scenario: Incluir fornecedor
- **WHEN** o administrador inclui o fornecedor "Emissor Exemplo" com o portal de suporte
- **THEN** o fornecedor aparece ativo na lista e nas opções de registro

#### Scenario: Nome repetido
- **WHEN** o administrador inclui um fornecedor com o nome de outro
- **THEN** a API responde 409 e nada é gravado

#### Scenario: Fornecedor inativo
- **WHEN** o administrador desativa um fornecedor
- **THEN** ele some das opções de novos registros, e os registros que já existem continuam com o nome dele

#### Scenario: Fornecedores só do administrador
- **WHEN** um gerente de projeto tenta incluir um fornecedor
- **THEN** a API responde 403

### Requirement: Chamado no fornecedor

Quem atende o chamado MUST registrar nele um ou mais chamados abertos em fornecedores ativos, inclusive de fornecedores diferentes, com o número, a data de abertura, o status (Aberto no fornecedor, Em análise, Aguardando nosso retorno, Resolvido ou Fechado), o link e a observação, e MUST atualizar o status com uma anotação. O registro e cada atualização MUST entrar na conversa como eventos. O nosso prazo continua correndo. Os outros MUST receber 403 (ADR-0028, ADR-0044).

#### Scenario: Registrar no fornecedor
- **WHEN** o desenvolvedor responsável registra o chamado FX-20931 aberto no fornecedor "Emissor Exemplo"
- **THEN** o chamado mostra o registro, e a conversa mostra o evento

#### Scenario: Fornecedores diferentes
- **WHEN** quem atende registra no mesmo chamado um chamado em outro fornecedor
- **THEN** o chamado mostra os dois registros

#### Scenario: Atualizar o status no fornecedor
- **WHEN** quem atende muda o status do registro para "Aguardando nosso retorno", com uma anotação
- **THEN** o registro mostra o novo status, e a conversa mostra o evento com a anotação

#### Scenario: Solicitante não registra no fornecedor
- **WHEN** o solicitante tenta registrar um chamado no fornecedor
- **THEN** a API responde 403 e nada é gravado

### Requirement: Fornecedor parado

Um registro ainda em aberto no fornecedor e sem atualização há `Tickets:VendorStaleBusinessDays` dias úteis (5) do calendário da instalação MUST ser marcado com os dias úteis sem retorno (ADR-0028).

#### Scenario: Sem retorno do fornecedor
- **WHEN** um registro em análise fica 6 dias úteis sem atualização
- **THEN** o chamado mostra "Sem retorno do fornecedor há 6 dias úteis"

### Requirement: Fornecedores pela tela

O painel do chamado MUST ter a seção "No fornecedor", com cada registro numa linha (fornecedor, número, status por ícone, cor e texto, e o destaque de parado), que abre o diálogo do registro com os dados e o histórico de status, e "Registrar" e, no diálogo, "Atualizar" para quem atende. A página Chamados MUST ter a visão "Com fornecedor", com a contagem, e a lista MUST mostrar o fornecedor de cada chamado. A Administração MUST ter "Fornecedores" (ADR-0028, ADR-0065).

#### Scenario: Registrar pela tela
- **WHEN** quem atende escolhe "Registrar" em "No fornecedor", preenche o fornecedor e o número e confirma
- **THEN** a seção mostra o registro, e a conversa, o evento

#### Scenario: Registro pela tela
- **WHEN** alguém escolhe um registro em "No fornecedor", depois de duas mudanças de status
- **THEN** o diálogo mostra o link, a data de abertura no fornecedor, quem registrou, a observação e o histórico com o registro e as duas mudanças, cada uma com quem fez e a anotação, sem "Atualizar" para quem não atende

#### Scenario: Atualizar pelo registro
- **WHEN** quem atende escolhe "Atualizar" no diálogo do registro, muda o status e confirma
- **THEN** a seção mostra o novo status

#### Scenario: Com fornecedor
- **WHEN** alguém escolhe a visão "Com fornecedor"
- **THEN** a lista traz só os chamados com registro em fornecedor

#### Scenario: Fornecedores na Administração
- **WHEN** o administrador inclui um fornecedor em "Fornecedores"
- **THEN** a lista mostra o fornecedor ativo, com o portal

### Requirement: Chamados de um projeto cancelado

O chamado fechado pelo cancelamento do projeto MUST ficar Fechado em qualquer status em que estava, com o evento "projeto cancelado" na conversa, com quem cancelou e o status de antes. Os prazos MUST parar: o resolvido guarda o prazo da resolução, e os outros ficam sem prazo de resolução, como um chamado cancelado (ADR-0025, ADR-0043).

#### Scenario: Fechado pelo cancelamento do projeto
- **WHEN** o projeto é cancelado com um chamado Aguardando solicitante
- **THEN** o chamado fica Fechado, a conversa mostra "fechou o chamado · projeto cancelado · Aguardando solicitante → Fechado" e o chamado fica sem prazo de resolução

### Requirement: Iniciar atendimento assume o chamado

"Iniciar atendimento" num chamado sem responsável MUST tornar responsável quem fez a ação, se essa pessoa pode ser responsável pelo chamado (quem atende o time ou o gerente do projeto), e fazê-la acompanhar o chamado, com o evento na conversa. Com responsável, o responsável MUST continuar o mesmo. "Iniciar triagem" MUST NOT mudar o responsável (ADR-0068, ADR-0098).

#### Scenario: Iniciar atendimento sem responsável
- **WHEN** um desenvolvedor inicia o atendimento de um chamado de Desenvolvimento Em triagem e sem responsável
- **THEN** o chamado fica Em andamento, o desenvolvedor passa a ser o responsável e a acompanhar o chamado, e a conversa registra a atribuição

#### Scenario: Iniciar atendimento com responsável
- **WHEN** o líder técnico inicia o atendimento de um chamado Em triagem que já tem Diego como responsável
- **THEN** o chamado fica Em andamento e Diego continua o responsável

#### Scenario: Administrador de fora inicia o atendimento
- **WHEN** o administrador, sem estar no projeto, inicia o atendimento de um chamado sem responsável
- **THEN** o chamado fica Em andamento e continua sem responsável

#### Scenario: Iniciar triagem não assume
- **WHEN** um desenvolvedor inicia a triagem de um chamado Aberto sem responsável
- **THEN** o chamado fica Em triagem e continua sem responsável

### Requirement: Baixar anexos pela tela

Cada anexo, na conversa e em "Anexos da conversa", MUST ter o ícone "Baixar" ao lado do nome, que continua abrindo o arquivo. "Anexos da conversa" MUST ter "Baixar todos" quando há anexos (ADR-0026).

#### Scenario: Baixar pela tela
- **WHEN** alguém abre um chamado com anexos
- **THEN** cada anexo mostra o nome, que abre o arquivo, e "Baixar", que baixa com o nome original, e "Anexos da conversa" mostra "Baixar todos"

#### Scenario: Sem anexos para baixar
- **WHEN** alguém abre um chamado sem anexos
- **THEN** "Anexos da conversa" diz que não há anexos, sem "Baixar todos"
