# membros-do-projeto Specification

## Purpose

Define as pessoas de cada projeto: quem inclui, troca o papel e remove pessoas, o último gerente protegido, o gerente com o papel no sistema, a inclusão pelo diretório, a aba Membros e a auditoria dessas mudanças (ADR-0005, ADR-0040, ADR-0042, ADR-0043, ADR-0085).

## Requirements

### Requirement: Quem gerencia as pessoas do projeto

O administrador e o gerente do projeto MUST poder incluir, trocar o papel e remover qualquer pessoa. O líder técnico MUST poder fazer o mesmo com quem não é gerente, e MUST NOT incluir, remover, rebaixar ou promover gerentes (403). Colaborador, desenvolvedor e leitor MUST receber 403. Para quem não está no projeto e não é administrador, o projeto não existe (404); sem sessão, 401 (ADR-0042, ADR-0043).

#### Scenario: Gerente inclui, troca o papel e remove
- **WHEN** o gerente inclui Diego Torres como desenvolvedor, troca o papel dele para colaborador e o remove
- **THEN** a API aceita as três mudanças, e Diego sai do projeto

#### Scenario: Líder técnico inclui um desenvolvedor
- **WHEN** o líder técnico inclui como desenvolvedor uma pessoa com o papel no sistema Líder técnico e depois troca o papel dela para líder técnico
- **THEN** a API aceita as duas mudanças

#### Scenario: Líder técnico e os gerentes
- **WHEN** o líder técnico tenta incluir um gerente, promover alguém a gerente, rebaixar ou remover um gerente
- **THEN** a API responde 403 e as pessoas do projeto continuam como estavam

#### Scenario: Colaborador, desenvolvedor e leitor não mexem nas pessoas
- **WHEN** um colaborador, um desenvolvedor ou um leitor tenta incluir, trocar o papel ou remover uma pessoa
- **THEN** a API responde 403

#### Scenario: Administrador fora do projeto
- **WHEN** o administrador, sem estar no projeto, inclui uma pessoa
- **THEN** a API aceita a inclusão

#### Scenario: Pessoas de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede as pessoas do projeto ou tenta mudá-las
- **THEN** a API responde 404

#### Scenario: Pessoas do projeto sem sessão
- **WHEN** alguém sem sessão pede as pessoas de um projeto
- **THEN** a API responde 401

### Requirement: Pessoas do projeto

Quem vê o projeto MUST poder ver as pessoas dele, cada uma com o nome, o e-mail, o papel no projeto, a data em que entrou e o último acesso ao sistema, vazio para quem nunca entrou (ADR-0040, ADR-0043).

#### Scenario: Pessoas do projeto pela API
- **WHEN** um leitor do projeto pede as pessoas do projeto
- **THEN** a resposta traz cada pessoa com o nome, o e-mail, o papel, a data de entrada no projeto e o último acesso

### Requirement: Um papel por pessoa

Cada pessoa MUST ter um só papel em cada projeto. Incluir quem já está no projeto MUST ser recusado com 409; para mudar o papel, troca-se o papel (ADR-0042).

#### Scenario: Pessoa que já está no projeto
- **WHEN** o gerente inclui de novo uma pessoa que já está no projeto
- **THEN** a API responde 409 e o papel da pessoa continua o mesmo

### Requirement: Último gerente

O projeto MUST ter sempre ao menos um gerente. Remover o último gerente ou trocar o papel dele MUST ser recusado com 409, mesmo com pedidos ao mesmo tempo (ADR-0042).

#### Scenario: Remover o último gerente
- **WHEN** o administrador tenta remover o único gerente do projeto
- **THEN** a API responde 409 e o gerente continua no projeto

#### Scenario: Rebaixar o último gerente
- **WHEN** o único gerente tenta trocar o próprio papel para desenvolvedor
- **THEN** a API responde 409 e ele continua gerente

#### Scenario: Outro gerente no projeto
- **WHEN** o projeto tem dois gerentes e um deles é removido
- **THEN** a API aceita, e o outro continua gerente

### Requirement: Gerente com o papel no sistema

Incluir uma pessoa como gerente ou trocar o papel de alguém para gerente MUST aceitar só quem está ativo e tem, naquele momento, o papel no sistema Gerente de projeto ou Administrador; os outros MUST receber 400 no papel. Trocar depois o papel no sistema não muda o papel nos projetos (ADR-0085).

#### Scenario: Promover a gerente
- **WHEN** o gerente troca para gerente o papel de uma pessoa com o papel no sistema Gerente de projeto
- **THEN** a API aceita, e o projeto passa a ter dois gerentes

#### Scenario: Gerente sem o papel no sistema
- **WHEN** o gerente inclui como gerente uma pessoa com o papel no sistema Usuário, ou uma pessoa que ainda não entrou
- **THEN** a API responde 400 no papel e a pessoa não entra no projeto

### Requirement: Incluir pessoa pelo diretório

Incluir MUST aceitar uma pessoa ativa do sistema ou uma pessoa do diretório da empresa que ainda não entrou; nesse caso, o sistema MUST criar o usuário, com o papel Usuário e sem último acesso, e incluí-lo. Pessoa que não está no diretório ou está inativa MUST receber 400 (ADR-0005, ADR-0007).

#### Scenario: Incluir quem nunca entrou
- **WHEN** o gerente inclui como colaboradora Carla Mendes, que está no diretório e nunca entrou
- **THEN** o sistema cria o usuário de Carla, com o papel Usuário e sem último acesso, e ela entra no projeto como colaboradora

#### Scenario: Pessoa fora do diretório ou inativa
- **WHEN** o gerente tenta incluir uma pessoa que não está no diretório ou um usuário inativo
- **THEN** a API responde 400 na pessoa e ninguém entra no projeto

### Requirement: Quem sai perde o acesso

Quem é removido do projeto MUST deixar de ver o projeto a partir da requisição seguinte; o que a pessoa já fez continua com o nome dela (ADR-0042).

#### Scenario: Pessoa removida
- **WHEN** o gerente remove um desenvolvedor e o desenvolvedor abre o projeto em seguida
- **THEN** a API responde 404 para o desenvolvedor

### Requirement: Pessoas de um projeto encerrado

Um projeto concluído ou cancelado é somente leitura: incluir, trocar o papel e remover pessoas MUST ser recusado com 409 até o projeto ser reaberto (ADR-0016).

#### Scenario: Incluir pessoa num projeto concluído
- **WHEN** o gerente tenta incluir uma pessoa num projeto concluído
- **THEN** a API responde 409 e a pessoa não entra no projeto

### Requirement: Tela Membros

A página do projeto MUST ter a aba Membros, com o número de pessoas, como no canvas ProjetoMembros: as pessoas por time (Gestão, Funcional, Desenvolvimento e Leitores) ou por nome, com o nome, o e-mail, o papel e o último acesso, e a busca. O líder técnico aparece em Gestão e em Desenvolvimento (ADR-0040, ADR-0068).

#### Scenario: Membros pela tela
- **WHEN** uma pessoa do projeto abre a aba Membros
- **THEN** a tela mostra as pessoas por time, cada uma com o nome, o e-mail, o papel e o último acesso, e a trilha mostra Projetos, o código e Membros

#### Scenario: Buscar e agrupar por nome
- **WHEN** a pessoa busca por parte de um nome e escolhe "Por nome"
- **THEN** a tela mostra só as pessoas correspondentes, em ordem alfabética, sem os times

### Requirement: Incluir pessoa pela tela

"Incluir pessoa" MUST aparecer só para quem gerencia as pessoas, com o projeto aberto, e pedir a pessoa, buscada no sistema e no diretório, e o papel. Para o líder técnico, o papel Gerente de projeto MUST NOT aparecer. Quem tem o papel no sistema Líder técnico MUST já vir com o papel Líder técnico, e o papel Líder técnico MUST ficar indisponível para quem não pode tê-lo (ADR-0042, ADR-0085, ADR-0097).

#### Scenario: Incluir pessoa pela tela
- **WHEN** o gerente escolhe "Incluir pessoa", busca uma pessoa que nunca entrou, escolhe o papel Colaborador e confirma
- **THEN** a pessoa aparece no time Funcional e uma mensagem confirma a inclusão

#### Scenario: Papel sugerido ao incluir
- **WHEN** o gerente escolhe em "Incluir pessoa" uma pessoa com o papel no sistema Líder técnico, e depois uma com o papel Usuário
- **THEN** o papel vem como Líder técnico para a primeira, e para a segunda o papel Líder técnico fica indisponível, com a explicação

#### Scenario: Membros sem gerenciar
- **WHEN** um desenvolvedor abre a aba Membros
- **THEN** a tela não mostra "Incluir pessoa" e o detalhe de cada pessoa não oferece trocar o papel nem remover

### Requirement: Detalhe da pessoa pela tela

Escolher uma pessoa MUST abrir o detalhe numa gaveta, com o nome, o e-mail, o papel no projeto, o time, a data de entrada e o último acesso. Para quem gerencia aquela pessoa, o detalhe MUST permitir trocar o papel e "Remover do projeto", com confirmação (ADR-0042, ADR-0081).

#### Scenario: Trocar o papel pela tela
- **WHEN** o gerente escolhe um desenvolvedor e troca o papel para colaborador
- **THEN** a pessoa passa ao time Funcional e uma mensagem confirma a troca

#### Scenario: Último gerente pela tela
- **WHEN** o único gerente tenta trocar o próprio papel pela tela
- **THEN** a tela explica que o projeto precisa de um gerente e o papel continua o mesmo

#### Scenario: Remover do projeto pela tela
- **WHEN** o gerente escolhe "Remover do projeto" no detalhe de uma pessoa e confirma
- **THEN** a pessoa sai da lista e uma mensagem confirma a remoção

#### Scenario: Remover a si mesmo pela tela
- **WHEN** um gerente, com outro gerente no projeto, remove a si mesmo e confirma
- **THEN** a tela volta para a lista de projetos, sem o projeto

### Requirement: Mudanças das pessoas na auditoria

Incluir, trocar o papel e remover uma pessoa MUST entrar na auditoria, com quem fez, a pessoa, o papel e o projeto pelo nome. O usuário criado pelo diretório MUST aparecer como criado por quem incluiu (ADR-0039, ADR-0084).

#### Scenario: Papel trocado e pessoa removida na auditoria
- **WHEN** o gerente troca o papel de Diego Torres para colaborador e depois o remove
- **THEN** a auditoria tem "Trocou o papel de uma pessoa num projeto", com o papel antes e depois, e "Removeu uma pessoa de um projeto", com Diego e o projeto

#### Scenario: Usuário criado pelo diretório na auditoria
- **WHEN** o gerente inclui uma pessoa que nunca entrou
- **THEN** a auditoria tem "Criou um usuário" e "Incluiu uma pessoa num projeto", com o gerente como autor

### Requirement: Tarefas de quem sai ou passa a leitor

Remover uma pessoa do projeto, ou trocar o papel dela para leitor, MUST deixar sem responsável as tarefas dela no cronograma, na mesma mudança; as datas e o progresso das tarefas não mudam. A confirmação da remoção pela tela MUST avisar que as tarefas da pessoa ficam sem responsável (ADR-0042, ADR-0043).

#### Scenario: Remover o responsável de tarefas
- **WHEN** o gerente remove do projeto um desenvolvedor responsável por duas tarefas
- **THEN** as duas tarefas ficam sem responsável, com as mesmas datas e o mesmo progresso, e a auditoria tem "Alterou uma tarefa" para cada uma

#### Scenario: Responsável passa a leitor
- **WHEN** o gerente troca para leitor o papel de uma colaboradora responsável por uma tarefa
- **THEN** a tarefa fica sem responsável e a colaboradora continua no projeto como leitora

### Requirement: Chamados no detalhe da pessoa

O detalhe da pessoa MUST mostrar os chamados em aberto do projeto em que ela é responsável ou solicitante, com a parte dela em cada um. Para o administrador e os gestores, MUST oferecer "Passar pendências", que passa esses chamados a outra pessoa, como responsável e como solicitante. A confirmação da remoção MUST avisar que os chamados de que a pessoa é responsável ficam sem responsável (ADR-0027, ADR-0042).

#### Scenario: Chamados da pessoa no detalhe
- **WHEN** o gerente escolhe um desenvolvedor responsável por dois chamados em aberto
- **THEN** o detalhe mostra os dois chamados, com "Responsável" em cada um

#### Scenario: Passar pendências
- **WHEN** o gerente escolhe "Passar pendências", escolhe outro desenvolvedor e confirma
- **THEN** os chamados passam ao outro desenvolvedor, e o detalhe fica sem chamados

### Requirement: Líder técnico com o papel no sistema

Incluir uma pessoa como líder técnico ou trocar o papel de alguém para líder técnico MUST aceitar só quem está ativo e tem, naquele momento, o papel no sistema Líder técnico ou Administrador; os outros MUST receber 400 no papel. Trocar depois o papel no sistema não muda o papel nos projetos (ADR-0097).

#### Scenario: Promover a líder técnico
- **WHEN** o gerente troca para líder técnico o papel de um desenvolvedor com o papel no sistema Líder técnico
- **THEN** a API aceita, e a pessoa passa ao papel Líder técnico

#### Scenario: Líder técnico sem o papel no sistema
- **WHEN** o gerente inclui como líder técnico uma pessoa com o papel no sistema Usuário ou uma pessoa que ainda não entrou, ou troca para líder técnico o papel de um desenvolvedor com o papel Usuário
- **THEN** a API responde 400 no papel e nada muda no projeto

#### Scenario: Líder técnico que perde o papel no sistema
- **WHEN** o administrador troca para Usuário o papel no sistema de quem é líder técnico num projeto
- **THEN** a pessoa continua líder técnico nesse projeto

### Requirement: Ausências das pessoas

Uma pessoa MAY ter ausências: o início e o fim de um período sem trabalho, de 1 a 366 dias corridos, sem se sobrepor a outra ausência dela e sem motivo. A ausência MUST NOT ser editada: apaga-se e cadastra-se de novo (ADR-0058, ADR-0141).

#### Scenario: Incluir ausência
- **WHEN** a gerente inclui uma ausência da Carla de 03/11 a 14/11 pelo projeto dela
- **THEN** a ausência aparece nas ausências da Carla, em todos os projetos de que ela participa

#### Scenario: Ausência inválida
- **WHEN** a gerente inclui uma ausência com o fim antes do início ou de mais de 366 dias
- **THEN** a API recusa com 400 no campo das datas, e nada é incluído

#### Scenario: Ausência sobreposta
- **WHEN** a gerente inclui uma ausência que se cruza com outra da mesma pessoa
- **THEN** a API recusa com 409, e nada é incluído

#### Scenario: Apagar ausência
- **WHEN** a gerente apaga uma ausência da Carla
- **THEN** a ausência sai das ausências da Carla

### Requirement: Quem cadastra as ausências

Incluir e apagar as ausências de uma pessoa de um projeto em aberto MUST ser permitido ao administrador e ao gerente do projeto e negado, com 403, ao líder técnico e às outras pessoas do projeto; quem não está no projeto MUST receber 404, e a pessoa que não está no projeto também (ADR-0043, ADR-0141).

#### Scenario: Permissões nas ausências por papel
- **WHEN** cada papel do projeto tenta ver, incluir e apagar uma ausência de uma pessoa do projeto
- **THEN** o administrador e o gerente conseguem tudo; o líder técnico, o colaborador, o desenvolvedor e o leitor veem e recebem 403 ao incluir e ao apagar; quem está fora do projeto recebe 404

#### Scenario: Ausência pelo projeto encerrado
- **WHEN** a gerente tenta incluir ou apagar uma ausência pelo projeto cancelado
- **THEN** a API recusa com 409 de projeto encerrado, e as ausências continuam visíveis

### Requirement: Ausências na lista de Membros

A lista de Membros MUST trazer, de cada pessoa, a ausência atual ou a próxima, e a tela MUST mostrar "Ausente até" com o fim, quando a pessoa está ausente hoje, ou "Ausente de" com o período, quando a ausência começa nos próximos 30 dias (ADR-0141).

#### Scenario: Ausente hoje na lista
- **WHEN** alguém do projeto abre Membros com a Carla ausente até 14/11 e o Diego com férias a partir da semana que vem
- **THEN** a linha da Carla diz "Ausente até 14/11" e a do Diego diz o período das férias dele

### Requirement: Ausências no detalhe da pessoa pela tela

O detalhe da pessoa em Membros MUST ter a seção "Ausências", com cada ausência guardada, e, para quem cadastra, "Incluir ausência", que abre o diálogo com o início e o fim digitados, e o apagar de cada uma, com a confirmação (ADR-0141).

#### Scenario: Incluir ausência pela tela
- **WHEN** a gerente abre o detalhe da Carla, escolhe "Incluir ausência", digita 03/11/2026 e 14/11/2026 e confirma
- **THEN** a ausência aparece na seção, e a tela confirma

#### Scenario: Ausência sobreposta pela tela
- **WHEN** a gerente inclui uma ausência que se cruza com outra
- **THEN** o diálogo continua aberto e diz que a pessoa já tem ausência nesse período

#### Scenario: Ausências de quem só vê
- **WHEN** o líder técnico abre o detalhe da Carla
- **THEN** a seção mostra as ausências, sem "Incluir ausência" nem o apagar

### Requirement: Ausências na auditoria

A inclusão e a exclusão de uma ausência MUST ir para a auditoria, com a pessoa e quem fez pelos nomes atuais, sem as datas (ADR-0039, ADR-0141).

#### Scenario: Ausência na auditoria
- **WHEN** a gerente inclui uma ausência da Carla
- **THEN** a auditoria tem a inclusão, pela gerente, com o nome da Carla e as datas só como alteradas
