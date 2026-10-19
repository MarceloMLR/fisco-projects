# Spec Delta

## MODIFIED Requirements

### Requirement: Quem vê e quem edita o cronograma

Todas as pessoas do projeto e o administrador MUST ver o cronograma. Só o administrador e o gerente do projeto MUST poder editá-lo: fases, tarefas, marcos, dependências, responsáveis e o marco atingido. O líder técnico, o colaborador, o desenvolvedor e o leitor MUST receber 403 ao editar; o progresso das tarefas tem regra própria. Para quem não está no projeto e não é administrador, o cronograma não existe (404); sem sessão, 401 (ADR-0043).

#### Scenario: Gerente edita o cronograma
- **WHEN** o gerente do projeto inclui uma fase, uma tarefa e um marco, altera a tarefa e exclui o marco
- **THEN** a API aceita as cinco mudanças, e o cronograma mostra a fase e a tarefa alterada

#### Scenario: Administrador fora do projeto edita o cronograma
- **WHEN** o administrador, sem estar no projeto, vê o cronograma e inclui uma tarefa
- **THEN** a API aceita as duas ações

#### Scenario: Líder técnico só vê o cronograma
- **WHEN** o líder técnico vê o cronograma e depois tenta incluir, alterar ou excluir uma fase ou uma tarefa
- **THEN** a API mostra o cronograma, responde 403 às mudanças e nada muda

#### Scenario: Colaborador, desenvolvedor e leitor só veem o cronograma
- **WHEN** um colaborador, um desenvolvedor ou um leitor do projeto vê o cronograma e tenta alterar uma tarefa, mesmo uma da qual é responsável
- **THEN** a API mostra o cronograma, responde 403 à alteração e nada muda

#### Scenario: Cronograma de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede o cronograma ou tenta alterá-lo
- **THEN** a API responde 404

#### Scenario: Cronograma sem sessão
- **WHEN** alguém sem sessão pede o cronograma de um projeto
- **THEN** a API responde 401

### Requirement: Progresso das tarefas

O progresso de uma tarefa MUST ir de 0 a 100%; fora disso, 400. Ao chegar a 100%, a tarefa MUST guardar a data real de conclusão, o dia de hoje no fuso da instalação; abaixo de 100%, a data MUST ser apagada. Alterar os outros dados da tarefa sem informar o progresso MUST manter o progresso como está (ADR-0020).

#### Scenario: Concluir tarefa guarda a data
- **WHEN** o gerente passa uma tarefa para 100% em 16/10/2026, no fuso da instalação
- **THEN** a tarefa mostra a conclusão em 16/10/2026, e salvar de novo a 100% no dia seguinte mantém essa data

#### Scenario: Voltar abaixo de 100%
- **WHEN** o gerente passa para 80% uma tarefa concluída
- **THEN** a tarefa fica sem data de conclusão

#### Scenario: Alterar a tarefa mantém o progresso
- **WHEN** o responsável passa a tarefa para 60% e, depois, o gerente muda o nome dela sem informar o progresso
- **THEN** a tarefa fica com o nome novo e continua a 60%

### Requirement: Aba Cronograma

A página do projeto MUST ter a aba Cronograma, entre Visão geral e Membros, com a Lista: as fases em linhas de grupo, com datas e progresso, e abaixo de cada uma as tarefas e os marcos em ordem de início, com início, fim, duração, responsável, progresso e de quem dependem. O marco MUST ter a bandeira e mostrar Atingido, Pendente ou Atrasado; a tarefa atrasada, ícone, cor e "Atrasada". Acima da lista, o progresso do projeto e os atrasos (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma pela tela
- **WHEN** uma pessoa do projeto abre a aba Cronograma de um projeto com fases, tarefas, um marco e uma tarefa atrasada
- **THEN** a tela mostra as fases com as datas e o progresso, as tarefas e o marco abaixo delas, a tarefa atrasada com o texto "Atrasada", e a trilha mostra Projetos, o código e Cronograma

#### Scenario: Cronograma vazio
- **WHEN** o gerente abre a aba Cronograma de um projeto sem fases
- **THEN** a tela explica que o cronograma ainda não tem fases e oferece "Nova fase"

#### Scenario: Estados do marco pela tela
- **WHEN** hoje é 16/10/2026 e o cronograma tem um marco atingido em 09/10/2026, um pendente em 03/11/2026 e um não atingido de 15/10/2026
- **THEN** a Lista mostra "Atingido em 09/10", "Pendente" e "Atrasado", este com ícone e cor, e o resumo conta um marco atrasado

## ADDED Requirements

### Requirement: Quem atualiza o progresso

O administrador e o gerente MUST poder atualizar o progresso de qualquer tarefa. O líder técnico, o colaborador e o desenvolvedor MUST poder atualizar só o das tarefas de que são responsáveis e MUST receber 403 nas outras; o leitor MUST receber 403 sempre. Num marco, a API MUST responder 400, e num projeto encerrado, 409. Para quem não está no projeto, 404 (ADR-0016, ADR-0043).

#### Scenario: Gerente atualiza qualquer tarefa
- **WHEN** o gerente atualiza o progresso de uma tarefa de outra pessoa e de uma tarefa sem responsável
- **THEN** a API aceita as duas mudanças

#### Scenario: Responsável atualiza a própria tarefa
- **WHEN** o líder técnico, um colaborador ou um desenvolvedor atualiza o progresso de uma tarefa de que é responsável e depois o de uma tarefa de outra pessoa
- **THEN** a API aceita a primeira mudança e responde 403 à segunda

#### Scenario: Leitor não atualiza o progresso
- **WHEN** um leitor do projeto tenta atualizar o progresso de uma tarefa
- **THEN** a API responde 403 e o progresso continua como estava

#### Scenario: Progresso de um marco
- **WHEN** o gerente tenta atualizar o progresso de um marco
- **THEN** a API responde 400

#### Scenario: Progresso num projeto encerrado
- **WHEN** o responsável tenta atualizar o progresso da própria tarefa num projeto concluído
- **THEN** a API responde 409 e o progresso continua como estava

### Requirement: Marco atingido

O administrador e o gerente MUST poder marcar um marco como atingido e desmarcá-lo. Ao ser marcado, o marco MUST guardar o dia de hoje no fuso da instalação; ao ser desmarcado, MUST apagá-lo. O líder técnico, o colaborador, o desenvolvedor e o leitor MUST receber 403; numa tarefa, a API MUST responder 400, e num projeto encerrado, 409. Mudar a data do marco MUST NOT desmarcá-lo (ADR-0020, ADR-0043, ADR-0086).

#### Scenario: Marcar marco como atingido
- **WHEN** o gerente marca como atingido, em 16/10/2026, o marco "Aceite da homologação"
- **THEN** o marco fica atingido em 16/10/2026, e a auditoria tem "Alterou um marco" com a data antes e depois

#### Scenario: Desmarcar marco
- **WHEN** o gerente desmarca um marco atingido
- **THEN** o marco fica pendente, sem a data

#### Scenario: Líder técnico não marca o marco
- **WHEN** o líder técnico tenta marcar um marco como atingido
- **THEN** a API responde 403 e o marco continua pendente

#### Scenario: Atingir uma tarefa
- **WHEN** o gerente tenta marcar uma tarefa como atingida
- **THEN** a API responde 400

### Requirement: Marco atrasado

Um marco não atingido MUST ficar atrasado quando a data é anterior a hoje, no fuso da instalação. Uma fase com um marco atrasado MUST ficar atrasada, e o cronograma MUST dizer quantos marcos estão atrasados, além das tarefas (ADR-0086).

#### Scenario: Marco vencido
- **WHEN** hoje é 16/10/2026 e um marco de 15/10/2026 não foi atingido
- **THEN** o marco e a fase dele aparecem atrasados, e o cronograma conta um marco atrasado

#### Scenario: Marco atingido ou do dia não fica atrasado
- **WHEN** hoje é 16/10/2026, um marco de 15/10/2026 foi atingido e outro é de hoje
- **THEN** nenhum dos dois aparece atrasado

### Requirement: Progresso pela tela

No detalhe de uma tarefa, quem pode atualizar o progresso MUST ver o percentual com "Iniciar" a 0%, que pede o percentual, "Atualizar progresso" acima de 0% e "Concluir" abaixo de 100%. O formulário da tarefa MUST NOT ter o progresso. Para os outros, o progresso é só leitura. Uma mensagem MUST confirmar cada mudança (ADR-0020, ADR-0086).

#### Scenario: Iniciar pela tela
- **WHEN** o desenvolvedor responsável escolhe "Iniciar" numa tarefa a 0%, digita 30 e salva
- **THEN** a tarefa passa a 30% e a mensagem confirma a mudança

#### Scenario: Concluir pela tela
- **WHEN** o responsável escolhe "Concluir" numa tarefa a 80%
- **THEN** a tarefa passa a 100%, com a data de conclusão, e o detalhe deixa de oferecer "Concluir"

#### Scenario: Progresso sem permissão pela tela
- **WHEN** um colaborador abre o detalhe de uma tarefa de outra pessoa
- **THEN** o detalhe mostra o progresso sem "Iniciar", "Atualizar progresso" nem "Concluir"

### Requirement: Marco atingido pela tela

No detalhe de um marco, quem edita o cronograma MUST ver "Marcar como atingido" ou, num marco atingido, "Desmarcar"; os outros veem só o estado. A Lista e uma mensagem MUST mostrar a mudança (ADR-0043, ADR-0086).

#### Scenario: Marcar marco pela tela
- **WHEN** o gerente abre um marco pendente e escolhe "Marcar como atingido"
- **THEN** o marco aparece como atingido na Lista, com a data de hoje, e o detalhe oferece "Desmarcar"
