# cronograma Specification

## Purpose

Define o cronograma de cada projeto: fases, tarefas e marcos em dias úteis, dependências que empurram as tarefas seguintes, o progresso ponderado pela duração, o atraso, quem vê e quem edita, a aba Cronograma e a auditoria dessas mudanças (ADR-0018 a ADR-0020, ADR-0043, ADR-0071).

## Requirements

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

### Requirement: Fases do cronograma

O cronograma MUST ter dois níveis: fases e, dentro delas, tarefas e marcos. Uma fase MUST ter nome, com até 150 caracteres, sem os espaços das pontas, e pode ser renomeada. Excluir uma fase MUST excluir as tarefas e os marcos dela e as dependências deles (ADR-0019).

#### Scenario: Criar e renomear fase
- **WHEN** o gerente inclui a fase "Levantamento" e depois a renomeia para "Diagnóstico"
- **THEN** o cronograma mostra a fase "Diagnóstico", sem tarefas e sem datas

#### Scenario: Fase sem nome
- **WHEN** o gerente inclui uma fase sem nome ou com mais de 150 caracteres
- **THEN** a API responde 400 no nome e nenhuma fase é criada

#### Scenario: Excluir fase com tarefas
- **WHEN** o gerente exclui uma fase com duas tarefas, uma delas anterior a uma tarefa de outra fase
- **THEN** a fase, as duas tarefas e a dependência saem do cronograma, e a tarefa da outra fase continua com as mesmas datas

### Requirement: Tarefas e marcos

Uma tarefa MUST ter a fase, o nome, com até 200 caracteres, o início e a duração em dias úteis, de 1 a 999; o fim MUST ser calculado pelo sistema, contando o início e pulando os dias que não são úteis no calendário da instalação. Um marco MUST ser uma data, sem duração, sem responsável e sem progresso. O tipo não muda depois da criação (ADR-0018, ADR-0019).

#### Scenario: Criar tarefa
- **WHEN** o gerente inclui a tarefa "Levantar cadastros" começando na quinta-feira 08/10/2026, com 3 dias úteis, e 12/10/2026 é feriado
- **THEN** a tarefa termina na terça-feira 13/10/2026

#### Scenario: Marco sem duração
- **WHEN** o gerente inclui o marco "Aceite da homologação" em 03/11/2026
- **THEN** o marco começa e termina em 03/11/2026, com duração zero

#### Scenario: Tarefa inválida
- **WHEN** o gerente inclui uma tarefa sem nome, com duração zero, numa fase de outro projeto, ou um marco com responsável
- **THEN** a API responde 400 nos campos errados e nada é criado

### Requirement: Início em dia útil

Tarefas e marcos MUST começar num dia útil do calendário da instalação: um início num fim de semana ou feriado MUST passar para o dia útil seguinte. As datas guardadas não mudam quando o calendário muda depois; a próxima alteração da tarefa usa o calendário do momento (ADR-0018).

#### Scenario: Início num sábado
- **WHEN** o gerente inclui uma tarefa começando no sábado 10/10/2026, e 12/10/2026 é feriado
- **THEN** a tarefa começa na terça-feira 13/10/2026

### Requirement: Responsável da tarefa

Uma tarefa pode ter um responsável: uma pessoa do projeto que não é leitora. Outra pessoa MUST receber 400 no responsável (ADR-0020, ADR-0043).

#### Scenario: Responsável do projeto
- **WHEN** o gerente escolhe como responsável de uma tarefa um desenvolvedor do projeto
- **THEN** a tarefa mostra o desenvolvedor como responsável

#### Scenario: Leitor ou pessoa de fora como responsável
- **WHEN** o gerente escolhe como responsável um leitor do projeto ou uma pessoa que não está no projeto
- **THEN** a API responde 400 no responsável e a tarefa continua como estava

### Requirement: Dependências

Uma tarefa ou um marco pode depender de outras tarefas e marcos do mesmo cronograma, do tipo termina → começa: uma tarefa MUST começar no dia útil seguinte ao fim das anteriores, e um marco, no dia em que elas terminam. Um início mais cedo do que as anteriores permitem MUST passar para o primeiro dia possível. Depender de si mesmo ou formar um ciclo MUST receber 400 (ADR-0019).

#### Scenario: Tarefa depois da anterior
- **WHEN** o gerente faz uma tarefa que começa em 08/10/2026 depender de outra que termina na sexta-feira 09/10/2026, e 12/10/2026 é feriado
- **THEN** a tarefa passa a começar na terça-feira 13/10/2026, e um marco que dependa da mesma tarefa fica em 09/10/2026

#### Scenario: Dependência circular
- **WHEN** a tarefa B depende de A, a tarefa C depende de B, e o gerente tenta fazer A depender de C
- **THEN** a API responde 400 com o tipo de problema de ciclo e as dependências continuam como estavam

### Requirement: Dependências que empurram

Mudar o início ou a duração de uma tarefa para o fim ficar depois MUST empurrar as tarefas e os marcos seguintes, em cadeia, só o necessário para cada um começar depois das anteriores, pulando fins de semana e feriados. Antecipar uma tarefa MUST NOT mover as seguintes. A resposta MUST dizer quantos itens foram empurrados (ADR-0019).

#### Scenario: Atrasar empurra a cadeia
- **WHEN** A vai de 05/10 a 09/10/2026, B depende de A e vai de 13/10 a 14/10, C depende de B e fica em 15/10, e o gerente aumenta a duração de A em um dia
- **THEN** A termina em 13/10, B vai de 14/10 a 15/10, C fica em 16/10/2026, e a resposta diz que 2 itens foram empurrados

#### Scenario: Antecipar não puxa
- **WHEN** o gerente antecipa em dois dias úteis o início de uma tarefa que tem uma tarefa seguinte
- **THEN** a tarefa seguinte continua com as mesmas datas, e a resposta diz que nenhum item foi empurrado

### Requirement: Excluir sem mover

Excluir uma tarefa ou um marco MUST excluir as dependências dele, e tirar uma dependência MUST NOT mover nenhuma tarefa (ADR-0019).

#### Scenario: Excluir a tarefa anterior
- **WHEN** o gerente exclui uma tarefa da qual outra depende
- **THEN** a tarefa sai do cronograma, a outra perde a dependência e continua com as mesmas datas

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

### Requirement: Datas e progresso das fases e do projeto

As datas de uma fase MUST ir do primeiro início ao último fim das tarefas e marcos dela, e as do projeto, do primeiro ao último de todo o cronograma. O progresso da fase e o do projeto MUST ser ponderados pela duração das tarefas, arredondados para o inteiro mais próximo; marcos não contam. Uma fase sem tarefas nem marcos não tem datas (ADR-0016, ADR-0019, ADR-0020).

#### Scenario: Progresso ponderado pela duração
- **WHEN** uma fase tem uma tarefa de 2 dias úteis a 100%, uma de 8 dias úteis a 25% e um marco
- **THEN** a fase está a 40%, e as datas dela vão do primeiro início ao último fim, inclusive o do marco

### Requirement: Tarefa atrasada

Uma tarefa MUST ficar atrasada quando o fim é anterior a hoje, no fuso da instalação, e o progresso está abaixo de 100%. Uma fase com uma tarefa atrasada MUST ficar atrasada. O cronograma MUST dizer quantas tarefas estão atrasadas (ADR-0020).

#### Scenario: Tarefa vencida
- **WHEN** hoje é 16/10/2026 e uma tarefa terminou em 15/10/2026 a 80%
- **THEN** a tarefa e a fase dela aparecem atrasadas, e o cronograma conta uma tarefa atrasada

#### Scenario: Tarefa concluída ou do dia não fica atrasada
- **WHEN** hoje é 16/10/2026, uma tarefa terminou em 15/10/2026 a 100% e outra termina hoje a 50%
- **THEN** nenhuma das duas aparece atrasada

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

### Requirement: Cronograma de um projeto encerrado

Um projeto concluído ou cancelado é somente leitura: mudar o cronograma MUST receber 409 até o projeto ser reaberto, e o cronograma continua visível (ADR-0016).

#### Scenario: Alterar tarefa num projeto concluído
- **WHEN** o gerente tenta alterar uma tarefa de um projeto concluído
- **THEN** a API responde 409 e a tarefa continua como estava

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

### Requirement: Editar o cronograma pela tela

Para quem edita o cronograma, com o projeto aberto, a aba MUST oferecer "Nova fase" e "Nova tarefa", que pede o tipo (tarefa ou marco), o nome, a fase, o início, a duração, o responsável e de quem depende. Escolher uma tarefa MUST abrir o detalhe numa gaveta, com o fim calculado, para alterar ou excluir; escolher uma fase MUST permitir renomear ou excluir, com confirmação. Uma mensagem MUST dizer quantos itens foram empurrados. Para os outros, o detalhe é só leitura (ADR-0043, ADR-0081).

#### Scenario: Nova tarefa pela tela
- **WHEN** o gerente escolhe "Nova tarefa", preenche o nome, a fase, o início e a duração, escolhe um responsável e confirma
- **THEN** a tarefa aparece na fase, com o fim calculado pelo sistema, e uma mensagem confirma a inclusão

#### Scenario: Empurrar pela tela
- **WHEN** o gerente aumenta no detalhe a duração de uma tarefa com duas tarefas seguintes e salva
- **THEN** as datas das três mudam na lista e a mensagem diz que 2 itens foram empurrados

#### Scenario: Excluir fase pela tela
- **WHEN** o gerente escolhe uma fase com tarefas, escolhe "Excluir fase" e confirma
- **THEN** a confirmação diz que as tarefas e os marcos da fase também saem, e a fase sai da lista

#### Scenario: Cronograma sem editar pela tela
- **WHEN** o líder técnico abre a aba Cronograma e escolhe uma tarefa
- **THEN** a tela não mostra "Nova fase" nem "Nova tarefa", e o detalhe mostra a tarefa sem permitir alterar ou excluir

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

### Requirement: Mudanças do cronograma na auditoria

Incluir, alterar e excluir fases, tarefas, marcos e dependências MUST entrar na auditoria, com quem fez e os campos antes e depois, inclusive cada item empurrado por uma dependência; o projeto, a fase, o responsável e as tarefas de uma dependência aparecem pelo nome. A tela de Auditoria MUST ter os tipos Fases dos cronogramas, Tarefas e marcos e Dependências no filtro (ADR-0039, ADR-0084).

#### Scenario: Tarefa empurrada na auditoria
- **WHEN** o gerente aumenta a duração de uma tarefa e isso empurra a tarefa seguinte
- **THEN** a auditoria tem "Alterou uma tarefa" para as duas, com o gerente como autor, o projeto pelo nome e as datas antes e depois

#### Scenario: Dependência na auditoria
- **WHEN** o gerente faz uma tarefa depender de outra e depois exclui a dependência
- **THEN** a auditoria tem "Incluiu uma dependência" e "Excluiu uma dependência", com as duas tarefas pelo nome
