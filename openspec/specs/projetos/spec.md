# projetos Specification

## Purpose

Define os projetos da instalação: os dados e o status de cada projeto, quem cria, vê e altera, a criação com gerente e líder técnico, o projeto encerrado somente leitura, as telas Projetos, Novo projeto e a página do projeto, e a auditoria dessas mudanças (ADR-0015, ADR-0016, ADR-0040, ADR-0043, ADR-0072).

## Requirements

### Requirement: Quem cria e vê projetos

O administrador e o gerente de projeto (papel no sistema) MUST poder criar projetos; o usuário MUST receber 403. O administrador MUST ver todos os projetos, e as outras pessoas só os projetos em que estão; para elas, os outros projetos não existem (404). Quem não tem sessão MUST receber 401 (ADR-0040, ADR-0043).

#### Scenario: Administrador cria e vê projetos
- **WHEN** o administrador cria um projeto e lista os projetos
- **THEN** a criação é aceita e a lista mostra todos os projetos, inclusive aqueles em que ele não está

#### Scenario: Gerente de projeto cria um projeto
- **WHEN** um gerente de projeto cria um projeto
- **THEN** a criação é aceita e ele vê o projeto

#### Scenario: Usuário não cria projetos
- **WHEN** um usuário pede para criar um projeto
- **THEN** a API responde 403 e nada é criado

#### Scenario: Projeto de outras pessoas
- **WHEN** uma pessoa que não está num projeto lista os projetos ou pede o projeto
- **THEN** o projeto não aparece na lista e o pedido recebe 404

#### Scenario: Projetos sem sessão
- **WHEN** alguém sem sessão lista, cria, abre ou altera projetos
- **THEN** a API responde 401 e nada muda

### Requirement: Quem altera o projeto

O administrador, o gerente e o líder técnico do projeto MUST poder alterar o código, o nome, a descrição e o cliente. Só o administrador e o gerente do projeto MUST poder mudar o status, inclusive reabrir. O colaborador, o desenvolvedor e o leitor MUST receber 403 nas duas ações, e o líder técnico MUST receber 403 ao mudar o status (ADR-0043).

#### Scenario: Gerente altera o projeto
- **WHEN** o gerente do projeto altera os dados e muda o status
- **THEN** as duas ações são aceitas

#### Scenario: Líder técnico altera os dados
- **WHEN** o líder técnico altera os dados do projeto e depois tenta mudar o status
- **THEN** a alteração dos dados é aceita e a mudança de status recebe 403

#### Scenario: Colaborador, desenvolvedor e leitor não alteram o projeto
- **WHEN** um colaborador, um desenvolvedor ou um leitor do projeto pede para alterar os dados ou mudar o status
- **THEN** a API responde 403 e nada muda

#### Scenario: Administrador fora do projeto
- **WHEN** o administrador, sem estar no projeto, altera os dados e muda o status
- **THEN** as duas ações são aceitas

### Requirement: Dados do projeto

Um projeto MUST ter código e nome e pode ter descrição e cliente. O nome MUST ter até 150 caracteres e a descrição, até 4.000, sem os espaços das pontas. O cliente MUST ser um cliente ativo; um cliente que ficou inativo continua no projeto enquanto não for trocado (ADR-0015, ADR-0016).

#### Scenario: Criar projeto
- **WHEN** o gerente de projeto cria o projeto "erp-fin", "Implantação do ERP financeiro", com cliente e descrição
- **THEN** o projeto é criado com o código "ERP-FIN", em Planejamento

#### Scenario: Projeto inválido
- **WHEN** alguém cria um projeto sem nome, com descrição longa ou com um cliente inativo ou que não existe
- **THEN** a API responde 400, com o motivo em cada campo

#### Scenario: Projeto interno
- **WHEN** o gerente de projeto cria um projeto sem cliente
- **THEN** o projeto é criado sem cliente e a lista o mostra como projeto interno

### Requirement: Código do projeto

O código MUST ter de 2 a 15 caracteres, só letras, algarismos e hífens, começar por letra, sem hífen no fim nem dois seguidos. Ele é guardado em maiúsculas e MUST ser único entre todos os projetos, inclusive os encerrados (ADR-0016).

#### Scenario: Código inválido
- **WHEN** alguém cria um projeto com o código "1-ERP", "ERP-", "ERP--FIN" ou "E"
- **THEN** a API responde 400 no campo do código

#### Scenario: Código repetido
- **WHEN** alguém cria ou altera um projeto com o código de outro projeto, mesmo concluído ou cancelado, em maiúsculas ou minúsculas
- **THEN** a API responde 409, com o nome do projeto que já usa o código

### Requirement: Gerente e líder técnico na criação

A criação MUST definir o gerente do projeto: quem cria, por padrão, ou outra pessoa ativa com o papel no sistema Gerente de projeto ou Administrador. Com outro gerente, quem cria MUST NOT entrar no projeto. O líder técnico é opcional, MUST ser uma pessoa ativa diferente do gerente e entra com o papel Líder técnico; pode ser um usuário do sistema ou uma pessoa do diretório que ainda não entrou, que vira usuário com o papel Usuário (ADR-0005, ADR-0072).

#### Scenario: Quem cria é o gerente
- **WHEN** um gerente de projeto cria um projeto sem trocar o gerente
- **THEN** ele entra no projeto como gerente

#### Scenario: Projeto para outro gerente
- **WHEN** o administrador cria um projeto com Marina Costa como gerente
- **THEN** Marina entra como gerente, e o administrador não entra no projeto

#### Scenario: Gerente sem o papel no sistema
- **WHEN** alguém cria um projeto com um gerente que tem o papel Usuário, está inativo ou não existe
- **THEN** a API responde 400 no campo do gerente

#### Scenario: Líder técnico na criação
- **WHEN** o gerente de projeto cria um projeto com Rafael Souza como líder técnico
- **THEN** Rafael entra no projeto como líder técnico e passa a ver o projeto

#### Scenario: Líder técnico do diretório na criação
- **WHEN** o gerente de projeto cria um projeto com um líder técnico do diretório que nunca entrou
- **THEN** o sistema cria o usuário dessa pessoa, com o papel Usuário, e ela entra no projeto como líder técnico

#### Scenario: Líder técnico inválido
- **WHEN** alguém escolhe como líder técnico o próprio gerente, uma pessoa inativa ou alguém que não está no diretório
- **THEN** a API responde 400 no campo do líder técnico

### Requirement: Status do projeto

O projeto MUST começar em Planejamento e pode passar a Em andamento, Pausado, Concluído ou Cancelado, em qualquer ordem. Concluído ou cancelado, o projeto MUST ficar somente leitura: alterar os dados MUST receber 409, até o gerente ou o administrador reabrir o projeto, mudando o status para Planejamento, Em andamento ou Pausado. Nenhum projeto é excluído (ADR-0016, ADR-0043).

#### Scenario: Pausar e retomar
- **WHEN** o gerente pausa um projeto em andamento e depois o retoma
- **THEN** o status passa a Pausado e depois a Em andamento

#### Scenario: Projeto encerrado somente leitura
- **WHEN** alguém altera os dados de um projeto concluído ou cancelado
- **THEN** a API responde 409 e nada muda

#### Scenario: Reabrir projeto
- **WHEN** o gerente reabre um projeto concluído
- **THEN** o projeto volta a Em andamento e aceita alterações

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

### Requirement: Tela Novo projeto

O botão "Novo projeto" MUST aparecer só para o administrador e o gerente de projeto. A tela MUST pedir o código, o nome, o cliente, o gerente, o líder técnico e a descrição, como a seção "Dados do projeto" do canvas NovoProjeto. "Criar projeto" MUST abrir a página do projeto criado (ADR-0072).

#### Scenario: Novo projeto pela tela
- **WHEN** o gerente de projeto preenche o Novo projeto, escolhe um líder técnico e escolhe "Criar projeto"
- **THEN** a página do projeto abre, com ele como gerente e o líder técnico no cabeçalho

#### Scenario: Usuário sem Novo projeto
- **WHEN** um usuário abre Projetos
- **THEN** a tela não mostra "Novo projeto"

### Requirement: Gerente e cliente no Novo projeto

O gerente MUST vir com quem cria, marcado "(você)", e trocar o gerente MUST mostrar que quem cria não entra no projeto. "Cadastrar cliente" MUST abrir o diálogo do cliente, sem o logo para o gerente de projeto, e escolher o cliente incluído (ADR-0015, ADR-0046, ADR-0072).

#### Scenario: Cadastrar cliente na criação
- **WHEN** o gerente de projeto escolhe "Cadastrar cliente" e inclui um cliente novo
- **THEN** o cliente novo fica escolhido no campo Cliente

#### Scenario: Trocar o gerente na criação
- **WHEN** o administrador escolhe outro gerente
- **THEN** a tela avisa que ele não entra no projeto

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

### Requirement: Abas do projeto sem rolagem vertical

A linha das abas do projeto MUST NOT rolar na vertical nem mostrar barra de rolagem; ela só rola para o lado quando as abas não cabem na largura, como no celular (ADR-0066, ADR-0071).

#### Scenario: Abas sem barra de rolagem
- **WHEN** uma pessoa abre a página de um projeto no computador
- **THEN** a linha das abas não rola na vertical e não mostra barra de rolagem, e o traço da aba escolhida fica sobre a linha do cabeçalho

### Requirement: Ações do projeto

"Ações do projeto" MUST oferecer "Editar dados" a quem altera os dados e as mudanças de status a quem muda o status; concluir e cancelar MUST pedir confirmação. Um projeto encerrado MUST mostrar que está somente leitura, com "Reabrir projeto" para quem muda o status (ADR-0016, ADR-0043).

#### Scenario: Editar dados pela tela
- **WHEN** o líder técnico escolhe "Editar dados", troca o nome e salva
- **THEN** o cabeçalho mostra o nome novo, e as mudanças de status não aparecem para ele

#### Scenario: Concluir projeto pela tela
- **WHEN** o gerente escolhe "Concluir projeto" e confirma
- **THEN** o status passa a Concluído, a página avisa que o projeto está somente leitura e oferece "Reabrir projeto"

### Requirement: Meus projetos

A barra lateral MUST listar em Meus projetos os projetos ativos em que a pessoa está, com o nome e o código, e marcar o projeto aberto (ADR-0066).

#### Scenario: Meus projetos na barra lateral
- **WHEN** a pessoa está em projetos ativos
- **THEN** a barra lateral lista esses projetos, com o nome e o código, e marca o projeto aberto

### Requirement: Mudanças dos projetos na auditoria

Criar, alterar e mudar o status de um projeto MUST entrar na auditoria, com quem fez e os campos antes e depois; o cliente aparece pelo nome. A entrada de cada pessoa no projeto MUST entrar na auditoria, com a pessoa, o papel e o projeto pelo nome. A tela de Auditoria MUST ter os tipos Projetos e Pessoas dos projetos no filtro (ADR-0039, ADR-0072, ADR-0084).

#### Scenario: Projeto criado na auditoria
- **WHEN** o administrador cria um projeto com Marina Costa como gerente
- **THEN** a auditoria tem "Criou um projeto", com o administrador como autor, e "Incluiu uma pessoa num projeto", com Marina Costa, Gerente de projeto e o nome do projeto

#### Scenario: Status na auditoria
- **WHEN** o gerente conclui e depois reabre um projeto
- **THEN** a auditoria tem "Concluiu um projeto" e "Reabriu um projeto", com o status antes e depois

### Requirement: Cronograma na lista de projetos

Na lista, a coluna Cronograma MUST mostrar a faixa de fases, com a parte concluída, as fases atrasadas, os marcos e o dia de hoje, e embaixo o progresso com a fase atual: a primeira fase atrasada ou, sem atraso, a última que já começou; antes do início, "Começa em" e a data; com todas as tarefas a 100%, a data da conclusão. O Prazo final MUST mostrar o fim do cronograma e as tarefas e os marcos atrasados, com ícone e texto. Sem itens, "Sem cronograma" (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma na lista
- **WHEN** hoje é 16/10/2026 e a pessoa abre Projetos com um projeto de três fases, a segunda em andamento, que termina em 13/11/2026 com duas tarefas atrasadas na segunda fase
- **THEN** a linha do projeto mostra a faixa com as três fases, a segunda atrasada, o progresso com o nome da segunda fase, "13/11/2026" e "2 tarefas atrasadas"

#### Scenario: Projeto que ainda não começou
- **WHEN** hoje é 16/10/2026 e o cronograma de um projeto começa em 26/10/2026
- **THEN** a linha do projeto mostra a faixa e "Começa em 26/10"

#### Scenario: Projeto sem cronograma na lista
- **WHEN** um projeto não tem tarefas nem marcos
- **THEN** a linha do projeto mostra "Sem cronograma" e fica sem prazo final

#### Scenario: Marco atrasado na lista
- **WHEN** hoje é 16/10/2026 e um projeto tem uma tarefa e um marco atrasados
- **THEN** o prazo final do projeto diz "1 tarefa atrasada" e "1 marco atrasado"
