# cronograma Specification

## Purpose

Define o cronograma de cada projeto: fases, tarefas e marcos em dias úteis, dependências que empurram as tarefas seguintes, o progresso ponderado pela duração, o atraso, quem vê e quem edita, a aba Cronograma e a auditoria dessas mudanças (ADR-0018 a ADR-0020, ADR-0043, ADR-0071).

## Requirements

### Requirement: Quem vê e quem edita o cronograma

Todas as pessoas do projeto e o administrador MUST ver o cronograma. Só o administrador e o gerente MUST poder editá-lo: fases, tarefas, marcos, dependências, responsáveis, marco atingido, linha de base, desfazer e refazer. O líder técnico, o colaborador, o desenvolvedor e o leitor MUST receber 403 ao editar; o progresso tem regra própria. Para quem não está no projeto e não é administrador, o cronograma não existe (404); sem sessão, 401 (ADR-0043).

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

Tarefas e marcos MUST começar num dia útil do calendário da instalação: um início num fim de semana ou feriado MUST passar para o dia útil seguinte. As datas guardadas não mudam sozinhas quando o calendário muda depois: a próxima alteração da tarefa usa o calendário do momento, e o administrador pode recalcular as datas dos cronogramas em aberto (ADR-0018, ADR-0126).

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

Uma tarefa ou um marco pode depender de outras tarefas e marcos do mesmo cronograma, com um tipo ("termina, então começa", "começa junto" ou "termina junto") e um intervalo de -99 a 999 dias úteis. O item MUST ficar no primeiro dia que as anteriores permitem pela regra do ADR-0137: na primeira, a tarefa começa no dia útil seguinte ao fim da anterior e o marco no dia do fim. Depender de si mesmo ou formar um ciclo MUST receber 400 (ADR-0019).

#### Scenario: Tarefa depois da anterior
- **WHEN** o gerente faz uma tarefa que começa em 08/10/2026 depender de outra que termina na sexta-feira 09/10/2026, e 12/10/2026 é feriado
- **THEN** a tarefa passa a começar na terça-feira 13/10/2026, e um marco que dependa da mesma tarefa fica em 09/10/2026

#### Scenario: Intervalo entre as tarefas
- **WHEN** a homologação do cliente depende da entrega, que termina na sexta-feira 09/10/2026, com o intervalo de 2 dias úteis, e 12/10/2026 é feriado
- **THEN** a homologação começa na quinta-feira 15/10/2026, e com o intervalo de -1 começa na sexta-feira 09/10/2026

#### Scenario: Começa junto
- **WHEN** os testes, de 3 dias úteis, dependem da parametrização, que começa em 14/10/2026, do tipo "começa junto" com o intervalo de 1 dia útil
- **THEN** os testes vão de 15/10/2026 a 19/10/2026, mesmo que a parametrização termine depois

#### Scenario: Termina junto
- **WHEN** o treinamento, de 2 dias úteis, depende da migração, que termina em 20/10/2026, do tipo "termina junto"
- **THEN** o treinamento vai de 19/10/2026 a 20/10/2026

#### Scenario: Tipo e intervalo inválidos
- **WHEN** o gerente manda uma dependência com o intervalo de 1.000 dias úteis ou um tipo que não existe
- **THEN** a API responde 400 em `links` e nada muda

#### Scenario: Dependência circular
- **WHEN** a tarefa B depende de A, a tarefa C depende de B, e o gerente tenta fazer A depender de C
- **THEN** a API responde 400 com o tipo de problema de ciclo e as dependências continuam como estavam

### Requirement: Dependências que empurram

Mudar o início ou a duração de uma tarefa para o fim ficar depois MUST empurrar as tarefas e os marcos seguintes, em cadeia, só o necessário para cada um respeitar o tipo e o intervalo das dependências, pulando fins de semana e feriados. Antecipar uma tarefa MUST NOT mover as seguintes. A resposta MUST dizer quantos itens foram empurrados (ADR-0019, ADR-0137).

#### Scenario: Atrasar empurra a cadeia
- **WHEN** A vai de 05/10 a 09/10/2026, B depende de A e vai de 13/10 a 14/10, C depende de B e fica em 15/10, e o gerente aumenta a duração de A em um dia
- **THEN** A termina em 13/10, B vai de 14/10 a 15/10, C fica em 16/10/2026, e a resposta diz que 2 itens foram empurrados

#### Scenario: Empurrar pelo início
- **WHEN** B começa junto com A e o gerente atrasa o início de A em dois dias úteis, sem mudar o fim de A além disso
- **THEN** B começa dois dias úteis depois, com a mesma duração

#### Scenario: Antecipar não puxa
- **WHEN** o gerente antecipa em dois dias úteis o início de uma tarefa que tem uma tarefa seguinte
- **THEN** a tarefa seguinte continua com as mesmas datas, e a resposta diz que nenhum item foi empurrado

### Requirement: Excluir sem mover

Excluir uma tarefa ou um marco MUST excluir as dependências dele, e tirar uma dependência MUST NOT mover nenhuma tarefa (ADR-0019).

#### Scenario: Excluir a tarefa anterior
- **WHEN** o gerente exclui uma tarefa da qual outra depende
- **THEN** a tarefa sai do cronograma, a outra perde a dependência e continua com as mesmas datas

### Requirement: Subtarefas

Uma tarefa que está diretamente na fase MUST poder ter subtarefas, tarefas e marcos, num nível só e na fase dela. Subtarefa de subtarefa, subtarefa de marco, tarefa-resumo dentro de outra tarefa ou subtarefa numa fase diferente da tarefa MUST receber 400 em `parentId`. Mudar a fase da tarefa-resumo MUST levar as subtarefas junto (ADR-0088).

#### Scenario: Incluir subtarefa
- **WHEN** o gerente inclui a tarefa "Mapear regras de ICMS" e o marco "Regras aprovadas" dentro de "Parametrizar regras fiscais"
- **THEN** os dois ficam abaixo de "Parametrizar regras fiscais", na fase dela

#### Scenario: Subtarefa fora do nível
- **WHEN** o gerente tenta pôr uma tarefa dentro de uma subtarefa ou de um marco, ou uma tarefa-resumo dentro de outra tarefa
- **THEN** a API responde 400 em `parentId` e o cronograma continua como estava

#### Scenario: Mudar a fase da tarefa-resumo
- **WHEN** o gerente muda uma tarefa-resumo da fase Configuração para a fase Homologação
- **THEN** a tarefa e as subtarefas dela passam para Homologação

### Requirement: Tarefa-resumo

Uma tarefa com subtarefas MUST ter o início, o fim, a duração em dias úteis, o progresso ponderado pela duração, a conclusão e o atraso calculados das subtarefas. Nela só mudam o nome, a fase e o responsável; atualizar o progresso ou mandar novas datas MUST receber 400 (ADR-0020, ADR-0088).

#### Scenario: Datas e progresso da tarefa-resumo
- **WHEN** uma tarefa tem uma subtarefa de 05/10 a 06/10/2026 a 100% e outra de 07/10 a 19/10/2026, com 8 dias úteis, a 25%, e 12/10/2026 é feriado
- **THEN** a tarefa vai de 05/10 a 19/10/2026, com 10 dias úteis e 40%

#### Scenario: Tarefa-resumo atrasada
- **WHEN** hoje é 16/10/2026 e uma subtarefa terminou em 15/10/2026 a 80%
- **THEN** a subtarefa, a tarefa-resumo e a fase aparecem atrasadas, e o cronograma conta uma tarefa atrasada

#### Scenario: Progresso de uma tarefa-resumo
- **WHEN** o gerente tenta atualizar o progresso ou mudar as datas de uma tarefa-resumo
- **THEN** a API responde 400 e a tarefa continua como estava

### Requirement: Dependências com subtarefas

Só subtarefas, tarefas sem subtarefas e marcos MUST ter dependências, inclusive entre tarefas e fases diferentes; uma tarefa-resumo como anterior ou seguinte MUST receber 400. Quando uma tarefa ganha a primeira subtarefa, as dependências dela MUST passar para a subtarefa; quando a última subtarefa é excluída, as dela MUST voltar para a tarefa (ADR-0019, ADR-0088).

#### Scenario: Primeira subtarefa herda as dependências
- **WHEN** "Testes integrados" depende de "Parametrizar regras de ICMS", "Testes com a equipe do cliente" depende de "Testes integrados", e o gerente inclui nela a primeira subtarefa, "Roteiro de testes"
- **THEN** "Roteiro de testes" depende de "Parametrizar regras de ICMS", "Testes com a equipe do cliente" depende de "Roteiro de testes", e "Testes integrados" fica sem dependências

#### Scenario: Depender de uma tarefa-resumo
- **WHEN** o gerente tenta fazer uma tarefa depender de uma tarefa-resumo, ou uma tarefa-resumo depender de outra tarefa
- **THEN** a API responde 400 em `predecessorIds` e as dependências continuam como estavam

#### Scenario: Empurrar subtarefas
- **WHEN** uma subtarefa de uma tarefa atrasa e uma subtarefa de outra tarefa depende dela
- **THEN** a segunda subtarefa é empurrada, as duas tarefas-resumo passam a ter as novas datas, e a resposta conta um item empurrado

#### Scenario: Excluir a última subtarefa
- **WHEN** o gerente exclui a única subtarefa de uma tarefa, que dependia de outra tarefa
- **THEN** a tarefa volta a ser uma tarefa comum, com as datas e o progresso que tinha, e passa a depender da outra tarefa

### Requirement: Excluir tarefa-resumo

Excluir uma tarefa-resumo MUST excluir as subtarefas e as dependências delas, sem mover os outros itens do cronograma (ADR-0019, ADR-0088).

#### Scenario: Excluir tarefa com subtarefas
- **WHEN** o gerente exclui uma tarefa com duas subtarefas, e uma tarefa de outra fase depende de uma delas
- **THEN** a tarefa e as subtarefas saem do cronograma, e a outra tarefa perde a dependência e continua com as mesmas datas

### Requirement: Subtarefas pela tela

A Lista e a Linha do tempo MUST mostrar as subtarefas abaixo da tarefa, e a tarefa-resumo com as datas e o progresso calculados. Quem edita MUST poder incluir uma subtarefa por "Nova subtarefa" ou pelo campo "Dentro de". A tarefa-resumo MUST NOT se arrastar nem se ligar na Linha do tempo, e "Depende de" MUST NOT oferecer tarefas-resumo (ADR-0071, ADR-0081, ADR-0088).

#### Scenario: Subtarefas na lista
- **WHEN** uma pessoa do projeto abre a Lista de um cronograma com uma tarefa-resumo
- **THEN** as subtarefas aparecem abaixo dela, com um recuo a mais, e a tarefa-resumo mostra a seta de recolher, as datas, a duração e o progresso calculados

#### Scenario: Subtarefas na linha do tempo
- **WHEN** o gerente abre a Linha do tempo de um cronograma com uma tarefa-resumo
- **THEN** a tarefa-resumo aparece como barra de resumo com as subtarefas abaixo, as subtarefas se arrastam e a tarefa-resumo não

#### Scenario: Detalhe da tarefa-resumo
- **WHEN** o gerente abre o detalhe de uma tarefa-resumo e escolhe uma das subtarefas na seção Subtarefas
- **THEN** o nome, a fase e o responsável se editam, o início, o fim, a duração e o progresso aparecem calculados pelas subtarefas, sem editar, e o detalhe passa para a subtarefa escolhida

### Requirement: Recolher subtarefas na Lista

Na Lista, a tarefa-resumo MUST ter uma seta que recolhe e mostra as subtarefas, pelo clique ou pelo teclado, sem abrir o detalhe; as subtarefas começam à mostra (ADR-0071, ADR-0088).

#### Scenario: Recolher subtarefas na lista
- **WHEN** uma pessoa escolhe a seta de uma tarefa-resumo na Lista e depois a escolhe de novo
- **THEN** as subtarefas somem da Lista sem abrir o detalhe, e voltam na segunda vez

### Requirement: Seção Subtarefas no detalhe

O detalhe de uma tarefa direto na fase MUST ter a seção Subtarefas, com as subtarefas dela, cada uma abrindo o próprio detalhe, e, para quem edita, "Nova subtarefa". Os botões do rodapé do detalhe, "Excluir" e "Salvar", MUST ter o mesmo tamanho (ADR-0081, ADR-0088).

#### Scenario: Nova subtarefa pela tela
- **WHEN** o gerente abre o detalhe de uma tarefa sem subtarefas, escolhe "Nova subtarefa" na seção Subtarefas e inclui uma tarefa
- **THEN** o formulário vem com "Dentro de" já escolhido e avisa que a tarefa passa a ser resumo, e a subtarefa aparece abaixo dela

### Requirement: Subtarefas na auditoria

Incluir, mover e excluir subtarefas MUST entrar na auditoria como as tarefas, com a tarefa de "Dentro de" pelo nome atual; a mudança de datas da tarefa-resumo entra como alteração da tarefa (ADR-0039, ADR-0084).

#### Scenario: Subtarefa na auditoria
- **WHEN** o gerente inclui uma subtarefa e depois a move para outra tarefa
- **THEN** a auditoria tem "Incluiu uma tarefa" e "Alterou uma tarefa", com "Dentro de" pelo nome das duas tarefas

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

As datas de uma fase MUST ir do primeiro início ao último fim das tarefas e marcos dela, e as do projeto, do primeiro ao último de todo o cronograma. O progresso da fase e o do projeto MUST ser ponderados pela duração das tarefas, arredondados para o inteiro mais próximo; marcos não contam, e a tarefa-resumo também não, porque as subtarefas já contam. Uma fase sem tarefas nem marcos não tem datas (ADR-0016, ADR-0019, ADR-0020, ADR-0088).

#### Scenario: Progresso ponderado pela duração
- **WHEN** uma fase tem uma tarefa de 2 dias úteis a 100%, uma de 8 dias úteis a 25% e um marco
- **THEN** a fase está a 40%, e as datas dela vão do primeiro início ao último fim, inclusive o do marco

#### Scenario: Progresso sem contar a tarefa-resumo
- **WHEN** uma fase tem só uma tarefa-resumo, com uma subtarefa de 2 dias úteis a 100% e outra de 8 dias úteis a 25%
- **THEN** a tarefa-resumo e a fase estão a 40%

### Requirement: Tarefa atrasada

Uma tarefa MUST ficar atrasada quando o fim é anterior a hoje, no fuso da instalação, e o progresso está abaixo de 100%. Uma fase com uma tarefa atrasada MUST ficar atrasada. O cronograma MUST dizer quantas tarefas estão atrasadas, contando as subtarefas e não a tarefa-resumo (ADR-0020, ADR-0088).

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

### Requirement: Dias de atraso

A API MUST dizer, em cada tarefa e marco atrasado, há quantos dias úteis está atrasado (`lateDays`): os dias úteis do calendário da instalação do dia seguinte ao fim até hoje. Fora do atraso, o número é zero; a tarefa-resumo tem o da subtarefa mais atrasada (ADR-0018, ADR-0020, ADR-0086, ADR-0088).

#### Scenario: Dias de atraso da tarefa
- **WHEN** hoje é 19/10/2026 e uma subtarefa terminou em 15/10/2026 a 80%
- **THEN** ela tem 2 dias úteis de atraso, 16/10 e 19/10, sem contar o sábado e o domingo, e a tarefa-resumo dela também

#### Scenario: Atraso que começou num dia não útil
- **WHEN** hoje é o sábado 17/10/2026 e uma tarefa terminou na sexta 16/10/2026 a 80%
- **THEN** a tarefa está atrasada, com zero dia útil de atraso

#### Scenario: Dias de atraso do marco
- **WHEN** hoje é 16/10/2026 e um marco de 09/10/2026 não foi atingido
- **THEN** o marco tem 4 dias úteis de atraso, sem contar o fim de semana e o feriado de 12/10/2026

### Requirement: Cronograma de um projeto encerrado

Um projeto concluído ou cancelado é somente leitura: mudar o cronograma MUST receber 409 até o projeto ser reaberto, e o cronograma continua visível (ADR-0016).

#### Scenario: Alterar tarefa num projeto concluído
- **WHEN** o gerente tenta alterar uma tarefa de um projeto concluído
- **THEN** a API responde 409 e a tarefa continua como estava

### Requirement: Aba Cronograma

A página do projeto MUST ter a aba Cronograma, entre Visão geral e Membros, que alterna entre Linha do tempo e Lista; no computador abre na Linha do tempo e no celular, na Lista. A Lista MUST ter as fases em linhas de grupo, com datas e progresso, e abaixo delas as tarefas e os marcos em ordem de início, com início, fim, duração, responsável, progresso e dependências. O marco mostra Atingido, Pendente ou Atrasado, e o atraso aparece em dias úteis (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma pela tela
- **WHEN** hoje é 16/10/2026 e uma pessoa do projeto abre a Lista da aba Cronograma de um projeto com fases, tarefas, um marco e uma tarefa que terminou em 15/10/2026 a 80%
- **THEN** a tela mostra as fases com as datas e o progresso, as tarefas e o marco abaixo delas, a tarefa atrasada com "1 dia útil de atraso" embaixo do fim, e a trilha mostra Projetos, o código e Cronograma

#### Scenario: Cronograma vazio
- **WHEN** o gerente abre a aba Cronograma de um projeto sem fases
- **THEN** a tela explica que o cronograma ainda não tem fases e oferece "Nova fase"

#### Scenario: Estados do marco pela tela
- **WHEN** hoje é 16/10/2026 e o cronograma tem um marco atingido em 09/10/2026, um pendente em 03/11/2026 e um não atingido de 15/10/2026
- **THEN** a Lista mostra "Atingido em 09/10", "Pendente" e "Atrasado", este com ícone e cor e com "1 dia útil de atraso" na coluna Fim, e o resumo conta um marco atrasado

#### Scenario: Alternar Linha do tempo e Lista
- **WHEN** uma pessoa abre a aba Cronograma no computador e depois escolhe "Lista"
- **THEN** a aba abre na Linha do tempo e passa para a Lista, e no celular a aba abre na Lista

### Requirement: Atraso pela tela

A tarefa e o marco atrasados MUST dizer há quantos dias úteis ("1 dia útil de atraso", "3 dias úteis de atraso") na coluna Fim da Lista, a tarefa embaixo da data e o marco, que não tem fim, só com o atraso, e no cabeçalho do detalhe. Com zero dia útil, a tarefa diz "Atrasada", e o marco fica só com "Atrasado" no estado (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Atraso desde um dia não útil pela tela
- **WHEN** a API diz que uma tarefa e um marco estão atrasados com zero dia útil de atraso
- **THEN** a Lista mostra "Atrasada" embaixo do fim da tarefa, e o marco fica só com "Atrasado" no estado

### Requirement: Linha do tempo

A Linha do tempo MUST mostrar à esquerda o nome e o progresso de cada fase, tarefa e marco e, à direita, as fases como barras de resumo, as tarefas como barras com a parte concluída em verde, os marcos como losangos, as dependências como setas, os dias não úteis sombreados e a linha de hoje com "Hoje". A tarefa atrasada MUST ter ícone e cor, e o ícone MUST dizer há quantos dias úteis ela está atrasada. A palavra "Gantt" MUST NOT aparecer (ADR-0051, ADR-0065, ADR-0067, ADR-0071).

#### Scenario: Linha do tempo pela tela
- **WHEN** hoje é 16/10/2026 e uma pessoa do projeto abre a Linha do tempo de um projeto com fases, tarefas, um marco, dependências e uma tarefa que terminou em 15/10/2026 a 80%
- **THEN** a tela mostra cada fase com as tarefas e o marco abaixo dela, com o progresso, a tarefa atrasada com ícone e cor e o ícone dizendo "1 dia útil de atraso", as barras nas datas do cronograma e a linha de hoje em 16/10/2026

#### Scenario: Dias não úteis na linha do tempo
- **WHEN** a instalação considera o feriado de 12/10/2026 e trabalha de segunda a sexta
- **THEN** a Linha do tempo sombreia os sábados, os domingos e 12/10/2026

### Requirement: Escala e ajuste ao espaço

A Linha do tempo MUST ter as escalas Dias, Semanas e Meses. O período do cronograma MUST ocupar toda a largura disponível e a grade descer até o fim da área; se o período não couber com a largura mínima de cada dia, semana ou mês, a rolagem lateral MUST ficar só dentro da linha do tempo. "Hoje" MUST levar a linha do tempo até o dia de hoje (ADR-0071).

#### Scenario: Período ajustado ao espaço
- **WHEN** um cronograma de 05/10/2026 a 13/11/2026 é aberto na escala Dias num espaço que cabe o período
- **THEN** o período ocupa toda a largura da linha do tempo, sem espaço em branco nem rolagem lateral

#### Scenario: Período que não cabe
- **WHEN** um cronograma de um ano é aberto na escala Dias
- **THEN** cada dia fica com a largura mínima e a rolagem lateral aparece só dentro da linha do tempo

#### Scenario: Mudar a escala
- **WHEN** a pessoa escolhe Semanas e depois Meses
- **THEN** a linha do tempo passa a mostrar as semanas e depois os meses do período, ainda ocupando a largura

### Requirement: Arrastar na linha do tempo

No computador, quem edita o cronograma MUST poder arrastar a barra de uma tarefa ou de um marco para mudar o início, mantendo a duração em dias úteis, e puxar a borda da barra de uma tarefa para mudar a duração. O servidor MUST recalcular as datas e empurrar as seguintes, e a Linha do tempo MUST mostrar as datas que ele devolve, com uma mensagem dos itens empurrados. Quem não edita e o celular MUST NOT arrastar (ADR-0019, ADR-0021, ADR-0043, ADR-0051).

#### Scenario: Arrastar uma tarefa
- **WHEN** o gerente arrasta uma tarefa de 3 dias úteis que começa em 13/10/2026 para começar em 16/10/2026
- **THEN** a tarefa passa a ir de 16/10/2026 a 20/10/2026, as seguintes são empurradas e a mensagem diz quantas

#### Scenario: Puxar a borda de uma tarefa
- **WHEN** o gerente puxa o fim de uma tarefa que vai de 13/10/2026 a 15/10/2026 até 20/10/2026
- **THEN** a tarefa passa a durar 6 dias úteis, terminando em 20/10/2026

#### Scenario: Arrastar sem permissão
- **WHEN** o líder técnico abre a Linha do tempo
- **THEN** as barras não se arrastam nem se ligam

### Requirement: Ligar duas barras

Na Linha do tempo, quem edita o cronograma MUST poder ligar o fim de uma barra ao início de outra para criar a dependência; o servidor empurra a seguinte se preciso. A dependência circular MUST ser recusada com uma mensagem, sem mudar o cronograma (ADR-0019, ADR-0021).

#### Scenario: Ligar duas barras
- **WHEN** o gerente liga o fim de "Integrar emissão de NF-e" ao início de "Testes integrados"
- **THEN** "Testes integrados" passa a depender de "Integrar emissão de NF-e", e a seta aparece

#### Scenario: Ligação circular pela linha do tempo
- **WHEN** o gerente liga o fim de uma tarefa ao início de outra que vem antes dela na mesma cadeia
- **THEN** a tela diz que a dependência formaria um ciclo, e nada muda

### Requirement: Novas datas pela API

A API MUST aceitar a mudança das datas de uma tarefa ou de um marco pelo início e, numa tarefa, também pelo fim. Só com o início, a duração em dias úteis fica a mesma; com o fim, a duração passa a ser os dias úteis entre o início e o fim, de 1 a 999. O fim antes do início, o fim num marco ou a duração fora dos limites MUST receber 400; quem edita, o projeto encerrado e as seguintes empurradas seguem a alteração da tarefa (ADR-0018, ADR-0019, ADR-0043).

#### Scenario: Mudar o início pela API
- **WHEN** o gerente muda pela API o início de uma tarefa de 3 dias úteis para 16/10/2026, sem o fim
- **THEN** a tarefa vai de 16/10/2026 a 20/10/2026 e a resposta diz quantos itens foram empurrados

#### Scenario: Mudar o fim pela API
- **WHEN** o gerente muda pela API o fim de uma tarefa que começa em 13/10/2026 para 20/10/2026, mantendo o início
- **THEN** a tarefa passa a durar 6 dias úteis

#### Scenario: Fim antes do início
- **WHEN** o gerente manda o fim antes do início, ou um fim para um marco
- **THEN** a API responde 400 e nada muda

### Requirement: Dias não úteis no cronograma

O cronograma MUST trazer os dias não úteis do calendário da instalação, do mês anterior ao início ao mês seguinte ao fim, para a Linha do tempo sombreá-los sem repetir a regra do calendário; sem tarefas nem marcos, nenhum (ADR-0018, ADR-0051).

#### Scenario: Dias não úteis no cronograma
- **WHEN** a instalação considera o feriado de 12/10/2026 e o cronograma vai de 05/10/2026 a 13/11/2026
- **THEN** o cronograma traz 12/10/2026 e os fins de semana de setembro a dezembro de 2026, e nenhum dia útil

### Requirement: Detalhe pela linha do tempo

Escolher uma tarefa ou um marco na Linha do tempo MUST abrir o mesmo detalhe da Lista, e escolher uma fase, para quem edita, MUST abrir a fase. Fechado o detalhe, a Linha do tempo MUST NOT manter a linha escolhida, e escolher a mesma barra ou linha de novo MUST abrir o detalhe outra vez (ADR-0081).

#### Scenario: Detalhe pela linha do tempo
- **WHEN** uma pessoa do projeto escolhe uma tarefa na Linha do tempo
- **THEN** o detalhe da tarefa abre na gaveta, como na Lista

#### Scenario: Reabrir o detalhe pela linha do tempo
- **WHEN** uma pessoa escolhe a barra de uma tarefa na Linha do tempo, fecha o detalhe e escolhe a mesma barra de novo
- **THEN** a linha deixa de estar escolhida ao fechar, e o detalhe da tarefa abre de novo

### Requirement: Expandir o cronograma

"Expandir" MUST abrir um diálogo que cobre quase a tela inteira, com o título Cronograma, o código e o nome do projeto, a escala, "Hoje", "Nova tarefa" para quem edita e "Recolher", e a Linha do tempo com a tabela completa: nome, início, fim, responsável e progresso (ADR-0071).

#### Scenario: Expandir o cronograma
- **WHEN** o gerente escolhe "Expandir" na Linha do tempo
- **THEN** o diálogo cobre quase a tela inteira, com a tabela completa ao lado da linha do tempo, e "Recolher" volta à aba

### Requirement: Recolher na linha do tempo

Na Linha do tempo, as fases e as tarefas-resumo MUST ter uma seta ao lado do nome que recolhe e mostra as linhas de dentro, na tabela e no gráfico, sem abrir o detalhe; tudo começa à mostra (ADR-0071, ADR-0088).

#### Scenario: Recolher uma fase na linha do tempo
- **WHEN** uma pessoa escolhe a seta de uma fase na Linha do tempo e depois a escolhe de novo
- **THEN** as tarefas e os marcos da fase somem da tabela e do gráfico sem abrir o detalhe, e voltam na segunda vez

### Requirement: Linha do tempo pelo teclado

A tabela da Linha do tempo MUST ser navegável pelo teclado: as setas para cima e para baixo trocam a linha, a seta para a esquerda recolhe e a para a direita mostra uma fase ou tarefa-resumo, e Enter abre o detalhe, como o clique. Cada barra e cada marco MUST ter um nome para o leitor de tela, com o tipo, o nome, as datas e o progresso (ADR-0071, ADR-0081).

#### Scenario: Detalhe pelo teclado
- **WHEN** uma pessoa entra na tabela da Linha do tempo, desce duas linhas com a seta e aperta Enter
- **THEN** o detalhe da terceira linha abre na gaveta

#### Scenario: Recolher pelo teclado
- **WHEN** uma pessoa está numa fase da tabela e aperta a seta para a esquerda e depois a seta para a direita
- **THEN** a fase recolhe e depois mostra de novo as linhas de dentro

#### Scenario: Nome da barra
- **WHEN** o leitor de tela chega à barra de uma tarefa de 13/10/2026 a 15/10/2026 com 40%
- **THEN** ele lê o tipo, o nome, "13/10 a 15/10" e "40%"

### Requirement: Linha do tempo sem licença de terceiros

A Linha do tempo MUST ser desenhada pela própria aplicação, sem componente pago nem chave de licença, completa e sem aviso de versão de avaliação em qualquer instalação; a instalação MUST NOT pedir, guardar nem entregar ao navegador uma chave de licença para ela (ADR-0134).

#### Scenario: Instalação sem chave de licença
- **WHEN** uma pessoa do projeto abre a Linha do tempo numa instalação sem a chave do Syncfusion
- **THEN** a Linha do tempo aparece completa, sem aviso de versão de avaliação

#### Scenario: Barra que volta na recusa
- **WHEN** o gerente arrasta uma tarefa e a API recusa a mudança
- **THEN** a barra volta para as datas de antes e a tela diz por quê

#### Scenario: Instalação que não guarda a chave
- **WHEN** quem opera monta ou atualiza uma instalação pelo guia e pelo Terraform, e o navegador pede a rota que entregava a chave
- **THEN** nada pede nem guarda no Key Vault uma chave do Syncfusion, e a rota responde 404

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

### Requirement: Linha de base

O administrador e o gerente MUST poder salvar a linha de base do projeto: a cópia do início e do fim de cada tarefa e marco e do projeto, com o dia em que foi salva, no fuso da instalação. Salvar de novo MUST substituir a anterior. Cronograma sem tarefas nem marcos MUST receber 400, e projeto encerrado, 409 (ADR-0016, ADR-0021, ADR-0043).

#### Scenario: Salvar linha de base
- **WHEN** o gerente salva, em 16/10/2026, a linha de base de um cronograma que vai de 05/10/2026 a 03/11/2026
- **THEN** cada tarefa e marco guarda o próprio início e fim, e a linha de base é de 16/10/2026, de 05/10/2026 a 03/11/2026

#### Scenario: Atualizar linha de base
- **WHEN** o gerente muda o início de uma tarefa e salva a linha de base de novo, em 19/10/2026
- **THEN** a linha de base passa a ser de 19/10/2026, com as datas novas da tarefa

#### Scenario: Linha de base sem tarefas
- **WHEN** o gerente tenta salvar a linha de base de um cronograma só com fases
- **THEN** a API responde 400 e nada muda

#### Scenario: Linha de base num projeto encerrado
- **WHEN** o gerente tenta salvar a linha de base de um projeto concluído
- **THEN** a API responde 409 e a linha de base continua como estava

### Requirement: Diferença da linha de base

Com a linha de base salva, cada tarefa e marco que estava nela MUST dizer quantos dias úteis o fim está depois (positivo) ou antes (negativo) do fim na linha de base, e o cronograma MUST dizer o mesmo do fim do projeto. O item incluído depois MUST NOT ter linha de base (ADR-0018, ADR-0021).

#### Scenario: Tarefa depois da linha de base
- **WHEN** a linha de base guardou o fim de uma tarefa em 15/10/2026 e hoje a tarefa termina em 19/10/2026
- **THEN** a tarefa está 2 dias úteis depois da linha de base, 16/10 e 19/10, sem contar o fim de semana

#### Scenario: Tarefa antes da linha de base
- **WHEN** a linha de base guardou o fim de uma tarefa em 14/10/2026 e hoje a tarefa termina em 09/10/2026
- **THEN** a tarefa está 2 dias úteis antes da linha de base, sem contar o feriado de 12/10/2026

#### Scenario: Item incluído depois da linha de base
- **WHEN** o gerente inclui uma tarefa depois de salvar a linha de base
- **THEN** a tarefa não tem linha de base nem diferença

#### Scenario: Fim do projeto contra a linha de base
- **WHEN** a linha de base guardou o fim do projeto na sexta 30/10/2026 e hoje o projeto termina na quarta 04/11/2026
- **THEN** o cronograma está 2 dias úteis depois da linha de base, sem contar o fim de semana e o feriado de 02/11/2026

### Requirement: Caminho crítico

O caminho crítico MUST ser calculado no servidor: as tarefas e os marcos com datas próprias sem folga, isto é, que não podem terminar um dia útil depois sem atrasar o fim do projeto, pelas dependências com o tipo e o intervalo e pelo calendário da instalação. O servidor MUST dizer a folga de cada um, em dias úteis. A tarefa-resumo MUST NOT entrar; as subtarefas, sim. Uma tarefa concluída continua no caminho crítico pelas datas (ADR-0018, ADR-0021, ADR-0051, ADR-0088, ADR-0137).

#### Scenario: Cadeia mais longa é crítica
- **WHEN** a tarefa A, de 3 dias úteis, vem antes da tarefa B, de 2, que vem antes do marco M, e a tarefa C, de 2 dias úteis, começa com A e não tem seguintes
- **THEN** A, B e M estão no caminho crítico, e C não

#### Scenario: Folga em dias úteis
- **WHEN** uma tarefa termina na sexta 09/10/2026, sem seguintes, e o projeto termina na terça 13/10/2026, com o feriado de 12/10/2026
- **THEN** a tarefa tem um dia útil de folga, com o último fim em 13/10/2026, e não está no caminho crítico

#### Scenario: Subtarefas no caminho crítico
- **WHEN** a subtarefa mais longa de uma tarefa-resumo leva ao fim do projeto
- **THEN** a subtarefa está no caminho crítico, e a tarefa-resumo não

#### Scenario: Folga da tarefa
- **WHEN** a tarefa A, de 5 dias úteis, e a tarefa B, de 2, começam juntas e vêm antes do marco M
- **THEN** A e M estão no caminho crítico, sem folga, e B tem 3 dias úteis de folga

#### Scenario: Folga com os tipos
- **WHEN** a tarefa B, de 2 dias úteis, termina junto com a tarefa A, de 5, que vem antes do marco M
- **THEN** B não tem folga e está no caminho crítico

### Requirement: Linha de base pela tela

Para quem edita, o menu "⋮" da aba Cronograma MUST ter "Salvar linha de base" ou, com uma salva, "Atualizar linha de base", que pede confirmação. Com a linha de base ligada em "Exibir", a Linha do tempo MUST mostrar uma barra fina embaixo de cada barra. O resumo MUST mostrar a diferença do fim do projeto, e o detalhe, as datas salvas e a diferença do item (ADR-0021, ADR-0071, ADR-0081).

#### Scenario: Salvar linha de base pela tela
- **WHEN** o gerente escolhe "Salvar linha de base" no menu "⋮" de um cronograma sem linha de base
- **THEN** a tela avisa que a linha de base foi salva, e o resumo mostra o fim do projeto na data da linha de base

#### Scenario: Atualizar linha de base pela tela
- **WHEN** o gerente escolhe "Atualizar linha de base" e confirma
- **THEN** a tela avisa que a linha de base foi atualizada, e voltar na confirmação não muda nada

#### Scenario: Linha de base no detalhe
- **WHEN** uma pessoa do projeto abre o detalhe de uma tarefa que escorregou 2 dias úteis desde a linha de base
- **THEN** a seção Linha de base mostra o início e o fim salvos e "2 dias úteis depois"

#### Scenario: Sem editar, sem salvar a linha de base
- **WHEN** quem não edita o cronograma abre a aba Cronograma
- **THEN** a tela não oferece salvar nem atualizar a linha de base

### Requirement: Caminho crítico pela tela

Com o caminho crítico ligado em "Exibir", as barras das tarefas e os marcos do caminho crítico MUST ficar em vermelho, com a parte concluída em verde. "Exibir" MUST ligar e desligar a linha de base e o caminho crítico de forma independente, e no Expandir as duas opções ficam no cabeçalho. A legenda MUST dizer o que é cada cor (ADR-0065, ADR-0067, ADR-0071).

#### Scenario: Caminho crítico na linha do tempo
- **WHEN** uma pessoa do projeto abre a Linha do tempo de um cronograma com linha de base
- **THEN** as tarefas do caminho crítico aparecem em vermelho, cada tarefa com linha de base tem a barra fina embaixo, e a legenda tem "Caminho crítico" e "Linha de base"

#### Scenario: Exibir sem linha de base e caminho crítico
- **WHEN** a pessoa desmarca "Linha de base" e "Caminho crítico" em "Exibir"
- **THEN** a Linha do tempo some com as barras finas e o vermelho, e a legenda deixa de citá-los

### Requirement: Linha de base na auditoria

Salvar e atualizar a linha de base MUST entrar na auditoria como "Linha de base", pelo nome do projeto, com quem fez e o dia, o início e o fim antes e depois. A tela de Auditoria MUST ter o tipo "Linhas de base" no filtro. As datas de cada item na linha de base MUST NOT entrar uma a uma (ADR-0039, ADR-0084).

#### Scenario: Linha de base na auditoria
- **WHEN** o gerente salva a linha de base e depois a atualiza
- **THEN** a auditoria tem "Salvou a linha de base" e "Atualizou a linha de base" do projeto, com o gerente como autor e as datas antes e depois, e nenhum registro por tarefa

### Requirement: Desfazer no cronograma

Quem edita o cronograma MUST poder desfazer a própria última mudança ainda não desfeita, com tudo o que ela empurrou e recalculou. Se alguém mudou esses itens depois, o desfazer MUST ser recusado com 409, e a mudança sai do histórico. Cada pessoa guarda as últimas 20 mudanças por projeto (ADR-0021, ADR-0091).

#### Scenario: Desfazer volta a cadeia
- **WHEN** o gerente adia uma tarefa, que empurra o marco seguinte, e desfaz
- **THEN** a tarefa e o marco voltam às datas de antes, numa vez só

#### Scenario: Desfazer uma exclusão
- **WHEN** o gerente exclui uma tarefa com dependências e desfaz
- **THEN** a tarefa volta, com o mesmo identificador, as mesmas datas e as dependências

#### Scenario: Desfazer bloqueado
- **WHEN** o gerente adia uma tarefa, o administrador muda a mesma tarefa, e o gerente tenta desfazer
- **THEN** a API responde 409, nada muda, e o próximo desfazer do gerente vai para a mudança anterior

#### Scenario: Só as próprias mudanças
- **WHEN** o administrador inclui uma tarefa e o gerente, sem mudanças suas, tenta desfazer
- **THEN** a API responde 409 por não haver o que desfazer, e a tarefa continua lá

#### Scenario: Últimas 20 mudanças
- **WHEN** o gerente faz 21 mudanças e desfaz 21 vezes
- **THEN** as 20 últimas voltam, e o vigésimo primeiro desfazer responde que não há o que desfazer

### Requirement: Refazer no cronograma

Quem edita o cronograma MUST poder refazer a última mudança que desfez, com a mesma proteção do desfazer. Uma mudança nova da pessoa MUST apagar o que ela podia refazer (ADR-0071, ADR-0091).

#### Scenario: Refazer depois de desfazer
- **WHEN** o gerente renomeia uma fase, desfaz e refaz
- **THEN** a fase volta ao nome novo

#### Scenario: Mudança nova apaga o refazer
- **WHEN** o gerente desfaz uma mudança e depois inclui uma tarefa
- **THEN** não há o que refazer

### Requirement: Desfazer pela tela

Para quem edita, a aba Cronograma e o Expandir MUST ter o botão "Desfazer", com o que será desfeito na dica e desligado quando não há o que desfazer, e o menu "⋮" MUST ter "Refazer". A tela MUST dizer o que foi desfeito ou refeito, ou por que não deu (ADR-0071, ADR-0091).

#### Scenario: Desfazer pela tela
- **WHEN** o gerente inclui uma tarefa e escolhe "Desfazer"
- **THEN** a tela diz que a inclusão da tarefa foi desfeita e a tarefa some do cronograma

#### Scenario: Refazer pela tela
- **WHEN** o gerente escolhe "Refazer" no menu "⋮" depois de desfazer
- **THEN** a tela diz que a mudança foi refeita

#### Scenario: Desfazer bloqueado pela tela
- **WHEN** o desfazer é recusado porque alguém mudou os itens depois
- **THEN** a tela diz que não dá para desfazer porque alguém mudou esses itens depois

#### Scenario: Sem editar, sem desfazer
- **WHEN** quem não edita o cronograma abre a aba Cronograma
- **THEN** a tela não oferece desfazer nem refazer

### Requirement: Exportar para o MS Project

O administrador e o gerente do projeto MUST poder baixar o cronograma no XML do MS Project, também com o projeto encerrado: as fases, as tarefas, as subtarefas e os marcos em níveis, com as datas, a duração em dias úteis, o progresso, cada responsável como recurso com o nome e o e-mail, as dependências "termina, então começa", a linha de base e o calendário da instalação. As outras pessoas do projeto recebem 403, e quem está fora dele, 404 (ADR-0021, ADR-0043, ADR-0051, ADR-0136).

#### Scenario: XML do cronograma
- **WHEN** a gerente baixa o XML de um cronograma com uma fase, uma tarefa de 3 dias úteis do Rafael e um marco depois dela
- **THEN** o arquivo tem a fase no nível 1, a tarefa no nível 2, de 13/10/2026 às 08:00 a 15/10/2026 às 17:00, com 24 horas de duração, o marco com duração zero, ligado à tarefa, e o Rafael como recurso da tarefa, com o nome e o e-mail

#### Scenario: Calendário no XML
- **WHEN** o cronograma passa pelo feriado de 12/10/2026, uma segunda-feira
- **THEN** o calendário do arquivo tem segunda a sexta como dias de trabalho e o dia 12/10/2026 como exceção sem trabalho

#### Scenario: Linha de base no XML
- **WHEN** o cronograma tem a linha de base salva
- **THEN** cada tarefa e marco dela leva no arquivo o início e o fim da linha de base

#### Scenario: Responsável anonimizado no XML
- **WHEN** a gerente baixa o XML de um cronograma com uma tarefa de uma pessoa anonimizada
- **THEN** o recurso da tarefa tem o identificador da pessoa anonimizada, sem o e-mail

#### Scenario: XML só da gestão
- **WHEN** o líder técnico, uma colaboradora e um leitor do projeto pedem o XML do cronograma
- **THEN** a API responde 403 a cada um, e o gerente de um projeto concluído baixa o arquivo

### Requirement: Exportar em PDF pela impressão

Todas as pessoas do projeto MUST poder abrir a página de impressão do cronograma, com o período e a escala que escolherem. A página mostra o cabeçalho do projeto, a tabela com a linha do tempo de cada linha e a legenda, em A4 paisagem, e o botão "Imprimir" abre a impressão do navegador, que salva em PDF. "Exportar para o MS Project (XML)" MUST aparecer no menu "⋮" só para o administrador e o gerente do projeto (ADR-0021, ADR-0071, ADR-0136).

#### Scenario: Escolher o período e a escala
- **WHEN** uma pessoa do projeto escolhe "Exportar em PDF" no menu "⋮", troca o período e a escala e confirma
- **THEN** a página de impressão abre numa aba nova com esse período e essa escala

#### Scenario: Página de impressão
- **WHEN** a página de impressão abre com o período de 05/10/2026 a 31/10/2026, em dias
- **THEN** ela mostra o código e o nome do projeto, o período, uma linha por fase e item com início, fim e progresso, a barra de cada tarefa dentro do período, e a legenda

#### Scenario: Exportar sem editar
- **WHEN** quem não é o administrador nem o gerente do projeto abre o menu "⋮"
- **THEN** o menu tem só "Exportar em PDF" e "Exportar para Excel", sem o XML, sem refazer e sem a linha de base

#### Scenario: XML no menu da gestão
- **WHEN** o gerente de um projeto concluído abre o menu "⋮"
- **THEN** o menu tem "Exportar em PDF" e "Exportar para o MS Project (XML)"

### Requirement: Exportar para o Excel

Todas as pessoas do projeto MUST poder baixar o cronograma numa planilha do Excel pelo menu "⋮", também com o projeto encerrado: uma linha por fase e item, na ordem da tela, com o responsável, as datas, a duração, o progresso, a situação, o atraso, as dependências, o caminho crítico e a linha de base, e uma aba com os dados do projeto. A planilha MUST NOT levar e-mails (ADR-0021, ADR-0099, ADR-0136).

#### Scenario: Cronograma no Excel
- **WHEN** uma pessoa do projeto escolhe "Exportar para Excel" num cronograma com uma fase, uma tarefa com uma subtarefa, um marco que depende dela e a linha de base salva
- **THEN** ela baixa o arquivo com o código do projeto no nome, com a aba "Cronograma" (uma linha para a fase, a tarefa, a subtarefa recuada e o marco, com as datas como datas do Excel e o marco apontando para a linha da tarefa) e a aba "Projeto", sem nenhum e-mail

#### Scenario: Excel do projeto encerrado
- **WHEN** uma pessoa do projeto concluído escolhe "Exportar para Excel"
- **THEN** ela baixa a planilha com o cronograma como ele ficou

#### Scenario: Excel sem cronograma
- **WHEN** o cronograma não tem tarefas nem marcos
- **THEN** "Exportar para Excel" aparece desabilitado no menu "⋮", como o PDF

### Requirement: Endereço do detalhe

O endereço do cronograma com o item (`?item=`) MUST abrir os detalhes da tarefa ou do marco na gaveta, como a Lista; fechar a gaveta MUST tirar o item do endereço, e um item que não está mais no cronograma MUST deixar a aba como sem item escolhido (ADR-0081).

#### Scenario: Detalhe pelo endereço
- **WHEN** a pessoa abre o cronograma do projeto com o endereço de uma tarefa
- **THEN** os detalhes da tarefa abrem na gaveta

#### Scenario: Item que saiu do cronograma
- **WHEN** a pessoa abre o endereço de uma tarefa já excluída
- **THEN** a aba abre sem a gaveta

### Requirement: Cliente como responsável

Uma tarefa de um projeto com cliente MUST poder ter o cliente do projeto como responsável, no lugar de uma pessoa; nunca os dois, e nunca um marco. Sem cliente no projeto, o cliente como responsável MUST receber 400. O progresso de uma tarefa do cliente MUST ser atualizado só por quem edita o cronograma. A exportação para o MS Project MUST trazer o cliente como recurso (ADR-0102).

#### Scenario: Cliente como responsável
- **WHEN** o gerente escolhe o cliente como responsável dos testes do cliente
- **THEN** a tarefa fica com o cliente e sem pessoa, só o gerente atualiza o progresso, e escolher uma pessoa depois tira o cliente

#### Scenario: Cliente como responsável sem cliente
- **WHEN** o gerente escolhe o cliente como responsável num projeto sem cliente, junto de uma pessoa ou num marco
- **THEN** a API responde 400 no responsável e nada muda

#### Scenario: Desfazer o cliente como responsável
- **WHEN** o gerente desfaz a escolha do cliente como responsável
- **THEN** a tarefa volta a não ter o cliente

#### Scenario: Cliente como responsável pela API
- **WHEN** a tarefa tem o cliente como responsável
- **THEN** o cronograma traz o cliente na tarefa, a auditoria registra a troca e a exportação para o MS Project traz o cliente como recurso

#### Scenario: Cliente como responsável pela tela
- **WHEN** o gerente escolhe "{cliente} (cliente)" no responsável de uma tarefa e salva
- **THEN** a Lista mostra o cliente como responsável da tarefa

#### Scenario: Cliente como responsável sem cliente pela tela
- **WHEN** o projeto não tem cliente
- **THEN** o responsável não oferece o cliente

### Requirement: Recálculo pelo calendário

O administrador MUST poder ver quantas tarefas e marcos dos projetos em aberto mudam com o calendário atual e recalcular as datas deles. Cada item não concluído MUST manter o início, passado ao dia útil seguinte quando deixou de ser útil, e a duração em dias úteis, empurrado pelas dependências; os concluídos, a linha de base e os projetos encerrados MUST NOT mudar. Os responsáveis MUST ser avisados, e o recálculo MUST NOT entrar no desfazer (ADR-0126).

#### Scenario: Feriado novo no meio de uma tarefa
- **WHEN** a tarefa A vai de 26/10 a 30/10/2026, a tarefa B depende de A e vai de 03/11 a 04/11, e o administrador considera o Dia do Servidor Público (28/10) e recalcula as datas
- **THEN** A vai de 26/10 a 03/11 e B, de 04/11 a 05/11/2026

#### Scenario: Marco num feriado novo
- **WHEN** um marco não atingido está em 28/10/2026, e o administrador considera o Dia do Servidor Público e recalcula as datas
- **THEN** o marco passa para 29/10/2026

#### Scenario: Feriado desconsiderado não puxa as seguintes
- **WHEN** a tarefa A vai de 29/10 a 03/11/2026, a tarefa B depende de A e fica em 04/11, e o administrador desconsidera Finados e recalcula as datas
- **THEN** A termina em 02/11, e B continua em 04/11/2026

#### Scenario: Concluídos não mudam
- **WHEN** uma tarefa a 100% vai de 26/10 a 30/10/2026, um marco atingido está em 28/10, e o administrador considera o Dia do Servidor Público e recalcula as datas
- **THEN** a tarefa e o marco continuam com as mesmas datas

#### Scenario: Ver antes de recalcular
- **WHEN** o administrador considera o Dia do Servidor Público com uma tarefa de 26/10 a 30/10/2026 num projeto em andamento e pede o que o calendário atual muda
- **THEN** a API diz que 1 tarefa ou marco do projeto muda, e a tarefa continua de 26/10 a 30/10 até o recálculo

#### Scenario: Projeto encerrado fora do recálculo
- **WHEN** a mesma tarefa está num projeto concluído e o administrador recalcula as datas
- **THEN** o projeto não aparece no recálculo, e o cronograma dele não muda

#### Scenario: Responsável avisado do recálculo
- **WHEN** o recálculo muda as datas de uma tarefa de Carla
- **THEN** Carla recebe o aviso de que as datas da tarefa mudaram, e a auditoria registra a alteração da tarefa pelo administrador

#### Scenario: Recálculo só do administrador
- **WHEN** a gerente do projeto pede o que o calendário muda ou o recálculo
- **THEN** a API responde 403 e nada muda

#### Scenario: Recalcular de novo
- **WHEN** o administrador recalcula as datas duas vezes seguidas
- **THEN** a segunda vez não muda nada e não traz projetos

#### Scenario: Desfazer depois do recálculo
- **WHEN** a gerente desfaz a própria mudança de uma tarefa que o recálculo mudou depois
- **THEN** a API responde 409, a tarefa fica com as datas do recálculo e a mudança sai do histórico

### Requirement: Importar do MS Project

Quem cria projetos MUST poder ler um XML do MS Project numa prévia, sem guardar nada, e criar um projeto com o cronograma convertido (fases, tarefas, subtarefas, marcos e dependências com o tipo e o intervalo), em dias úteis a partir do início escolhido. A conversão MUST seguir o ADR-0135, avisar cada ajuste e o que ficou de fora e contar o que veio do arquivo; a linha de base MUST NOT vir (ADR-0017, ADR-0088, ADR-0137).

#### Scenario: Prévia do arquivo do MS Project
- **WHEN** a gerente manda na prévia o XML de um cronograma com uma fase, uma tarefa de 13/10/2026 a 15/10/2026 a 40% com o recurso Carla e um marco ligado a ela
- **THEN** a resposta traz a fase, a tarefa de 3 dias úteis com 40%, o marco ligado a ela, o primeiro dia 13/10/2026, o recurso Carla com uma tarefa e as 3 de 3 tarefas e o 1 de 1 recurso do arquivo, sem avisos, e nada é guardado

#### Scenario: Projeto a partir do MS Project
- **WHEN** a gerente cria um projeto com o cronograma da prévia, o início em 09/10/2026 (uma sexta-feira, antes do feriado de 12/10/2026) e o recurso Carla ligado à Carla como Colaboradora
- **THEN** o projeto nasce com a fase, a tarefa de 09/10/2026 a 14/10/2026 a 40% com a Carla responsável e o marco no fim dela, e a Carla entra no projeto como Colaboradora

#### Scenario: O que não cabe no FiscoProjects
- **WHEN** o arquivo tem uma tarefa no nível 4, uma tarefa solta no nível 1, uma dependência "começa, então termina", outra com o intervalo em porcentagem e outra de uma tarefa-resumo
- **THEN** a prévia traz a tarefa do nível 4 como subtarefa, a solta numa fase com o título do projeto, a "começa, então termina" como "termina, então começa", o intervalo em dias úteis, a da tarefa-resumo fora, e um aviso de cada ajuste com os nomes

#### Scenario: O que fica de fora do arquivo
- **WHEN** o arquivo tem uma tarefa inativa com uma subtarefa, duas linhas em branco, um recurso de material, um de custo e um recurso de trabalho só na subtarefa inativa
- **THEN** a prévia não traz a tarefa inativa, a subtarefa nem esses recursos, avisa a tarefa inativa e a subtarefa pelos nomes, as linhas em branco pela quantidade, os recursos de material e custo e o recurso sem tarefa pelos nomes, e conta as tarefas e os recursos do arquivo e os que vieram

#### Scenario: Arquivo que não é do MS Project
- **WHEN** a gerente manda na prévia um .mpp, um XML de outro programa, um XML com DTD ou um arquivo acima de 5 MB
- **THEN** a API responde 400 com o motivo e a orientação de salvar como XML no MS Project, sem ler entidades externas

#### Scenario: Importar sem poder criar projetos
- **WHEN** uma pessoa com o papel Usuário manda um arquivo na prévia
- **THEN** a API responde 403

#### Scenario: Ida e volta pelo MS Project
- **WHEN** a gerente exporta o XML de um cronograma e o manda na prévia
- **THEN** a prévia traz as mesmas fases, tarefas, subtarefas, marcos, durações, progresso e dependências, com os tipos e os intervalos, do cronograma exportado, e um recurso para cada responsável, com a pessoa dele sugerida pelo e-mail

### Requirement: Progresso e responsáveis do MS Project

O cronograma importado MUST trazer o progresso de cada tarefa e marco do arquivo. Cada recurso do arquivo MUST poder ser ligado na criação a uma pessoa, sugerida pelo e-mail no diretório, que entra no projeto com o papel escolhido e fica responsável pelas tarefas dele, ao cliente do projeto ou a ninguém, seguindo o ADR-0135 (ADR-0005, ADR-0042, ADR-0102, ADR-0135).

#### Scenario: Responsáveis do arquivo
- **WHEN** o arquivo tem um recurso com o e-mail de uma pessoa do diretório, outro sem e-mail, outro com o nome do cliente escolhido no formulário e uma tarefa com dois recursos
- **THEN** a prévia sugere a pessoa para o primeiro, ninguém para o segundo, o cliente para o terceiro, e avisa que a tarefa ficou com o primeiro recurso

#### Scenario: Progresso do arquivo
- **WHEN** o arquivo tem uma tarefa a 100% que termina antes de hoje e um marco a 100% depois de hoje
- **THEN** o projeto criado tem a tarefa concluída no fim dela e o marco atingido hoje

### Requirement: Importar do MS Project pela tela

O "Novo projeto" MUST oferecer "A partir do MS Project", além de "Em branco" e "A partir de um modelo", com o arquivo, o início preenchido com o primeiro dia do arquivo, a prévia das fases e dos itens com as datas, o quadro da conversão e os responsáveis do arquivo, cada um com a pessoa e o papel. O quadro MUST aparecer a cada leitura, com o que veio, os avisos e as tarefas que ficam sem responsável. O arquivo recusado MUST dizer o motivo no campo (ADR-0071, ADR-0135).

#### Scenario: Novo projeto a partir do MS Project
- **WHEN** a gerente escolhe "A partir do MS Project", escolhe o arquivo, confere a prévia, liga um recurso a uma pessoa como Desenvolvedora e cria o projeto
- **THEN** a prévia mostra as fases, os itens, os avisos e os responsáveis, a API recebe o cronograma convertido, a pessoa com o papel e o início, e a tela abre o projeto

#### Scenario: Quadro da conversão na tela
- **WHEN** a gerente escolhe um arquivo com dois avisos da conversão e um recurso sem pessoa achada, e depois troca o início
- **THEN** a tela rola até o quadro só na escolha do arquivo, o quadro diz quantas tarefas e quantos recursos do arquivo vieram, os dois avisos e a tarefa sem responsável, e a prévia mostra "3 ajustes na conversão", que leva ao quadro

#### Scenario: Arquivo sem ajustes na tela
- **WHEN** a gerente escolhe um arquivo que veio inteiro, sem avisos, com a pessoa de cada recurso achada
- **THEN** o quadro diz quantas tarefas e quantos recursos vieram e que nada mudou na conversão, e a prévia não mostra o atalho

#### Scenario: Tarefas sem responsável na tela
- **WHEN** o arquivo tem um recurso sem pessoa achada, com uma tarefa, e a gerente o liga depois a uma pessoa
- **THEN** o quadro e a linha do recurso dizem que a tarefa fica sem responsável, e os dois avisos saem quando a pessoa é escolhida

#### Scenario: Arquivo recusado pela tela
- **WHEN** a gerente escolhe um .mpp em "A partir do MS Project"
- **THEN** o campo do arquivo diz que o arquivo precisa ser salvo como XML no MS Project, e o projeto não é criado

### Requirement: Data-limite de tarefas e marcos

Quem edita o cronograma MUST poder pôr, mudar e tirar a data-limite de uma tarefa ou de um marco, sem mudar as datas do item nem das dependências. O cronograma MUST dizer, em dias úteis da instalação, quanto cada item passou dela (pelo fim ou pelo dia do marco; concluído, pelo dia em que terminou; atrasado, por hoje) e quantos itens abertos passaram. Ela MUST entrar no desfazer, no XML do MS Project e no Excel (ADR-0018, ADR-0019, ADR-0020, ADR-0043, ADR-0091, ADR-0135).

#### Scenario: Data-limite cumprida
- **WHEN** a gerente põe a data-limite 30/10/2026 em Testes integrados, que termina em 29/10/2026
- **THEN** as datas do item e das tarefas que dependem dele não mudam, e ele não passou da data-limite

#### Scenario: Data-limite estourada
- **WHEN** a data-limite de Testes integrados é 30/10/2026 e uma mudança empurra o fim dele para 03/11/2026, com 02/11/2026 feriado
- **THEN** o item passou 1 dia útil da data-limite, e o cronograma conta 1 data-limite estourada

#### Scenario: Data-limite do marco
- **WHEN** um marco do dia 04/11/2026, com a data-limite 04/11/2026, ainda não foi atingido em 05/11/2026
- **THEN** ele passou 1 dia útil da data-limite e conta como estourado; atingido em 05/11/2026, continua 1 dia útil depois, sem contar como estourado

#### Scenario: Data-limite só de quem edita
- **WHEN** um colaborador, um desenvolvedor, o líder técnico ou um leitor do projeto manda a data-limite de um item
- **THEN** a API responde 403, e para o administrador e o gerente a data-limite é gravada

#### Scenario: Desfazer a data-limite
- **WHEN** a gerente põe a data-limite num item e escolhe "Desfazer"
- **THEN** o item volta sem data-limite

#### Scenario: Data-limite no MS Project
- **WHEN** a gerente exporta o XML de um cronograma com uma data-limite e cria um projeto com esse arquivo, com o início 5 dias úteis depois
- **THEN** o XML traz o `Deadline` da tarefa, e o projeto novo tem a data-limite 5 dias úteis depois da original

#### Scenario: Data-limite no Excel
- **WHEN** uma pessoa do projeto exporta para o Excel um cronograma com um item que passou 1 dia útil da data-limite
- **THEN** a planilha traz a data-limite do item e o 1 dia útil na coluna do estouro

### Requirement: Data-limite pela tela

O detalhe da tarefa e do marco MUST ter o campo "Data-limite" para quem edita e dizer a quem vê quanto falta ou quanto passou, com ícone, cor e texto. A Linha do tempo e o PDF MUST marcar o dia da data-limite na linha do item, em vermelho quando passou, com a legenda. A Lista e o Expandir MUST ter a coluna "Data-limite", e o resumo da aba MUST contar as datas-limite estouradas (ADR-0065, ADR-0071, ADR-0134).

#### Scenario: Data-limite no detalhe
- **WHEN** a gerente põe a data-limite 30/10/2026 em Testes integrados pelo detalhe e, depois, o fim do item passa para 03/11/2026
- **THEN** a API recebe a data-limite, e o detalhe diz "Passou 1 dia útil da data-limite" em vermelho, com o ícone

#### Scenario: Marcador da data-limite
- **WHEN** a Linha do tempo mostra um item com a data-limite no período
- **THEN** a linha dele tem o marcador no dia da data-limite, vermelho se passou, e a legenda mostra "Data-limite"

#### Scenario: Data-limite na Lista e no resumo
- **WHEN** a Lista mostra um item que passou da data-limite
- **THEN** a coluna "Data-limite" mostra a data com o estouro destacado, e o resumo da aba mostra "1 data-limite estourada"

### Requirement: Filtros do cronograma

Toda pessoa do projeto MUST poder filtrar a Lista, a Linha do tempo e o Expandir por um filtro de cada vez: atrasadas, datas-limite estouradas, caminho crítico, minhas tarefas, próximas duas semanas ou por responsável. Os itens que passam MUST aparecer sob a fase e, se forem subtarefas, sob a tarefa delas, sem mudar as datas nem o período; o filtro MUST ficar no endereço e dizer quantos itens aparecem (ADR-0020, ADR-0021, ADR-0071, ADR-0102).

#### Scenario: Filtrar as atrasadas
- **WHEN** a gerente escolhe "Atrasadas" num cronograma de 20 tarefas e marcos com 4 atrasados
- **THEN** a Lista mostra só os 4, cada um sob a sua fase, o rodapé diz "4 de 20 tarefas e marcos" e o botão de filtro fica marcado, com o nome "Filtro: Atrasadas"

#### Scenario: Minhas tarefas
- **WHEN** Diego, desenvolvedor do projeto, escolhe "Minhas tarefas"
- **THEN** aparecem só as tarefas de que ele é o responsável

#### Scenario: Por responsável
- **WHEN** uma pessoa do projeto escolhe Carla Mendes, o cliente ou "Sem responsável" em "Por responsável"
- **THEN** aparecem só as tarefas dela, as do cliente ou as sem responsável, e uma subtarefa aparece sob a tarefa dela

#### Scenario: Próximas duas semanas
- **WHEN** hoje é 19/10/2026 e a pessoa escolhe "Próximas duas semanas"
- **THEN** aparecem as tarefas e os marcos em aberto que acontecem de 19/10/2026 a 01/11/2026, e não os concluídos, os atrasados nem os que começam depois

#### Scenario: Filtro na Linha do tempo
- **WHEN** a pessoa escolhe "Caminho crítico" na Linha do tempo e abre o Expandir
- **THEN** as duas mostram só as fases e os itens do caminho crítico, com as setas entre eles, no mesmo período, e o endereço guarda o filtro

#### Scenario: Filtro sem resultado
- **WHEN** o filtro escolhido não tem nenhum item, como "Datas-limite estouradas" sem nenhuma estourada
- **THEN** a tela diz "Nenhuma tarefa ou marco neste filtro." e "Limpar filtro" volta a mostrar tudo

### Requirement: Tipo e intervalo das dependências pela tela

No detalhe da tarefa e do marco, quem edita MUST escolher o tipo ("Termina, então começa", "Começa junto", "Termina junto") e o intervalo de cada item de "Depende de". A Lista MUST dizer o tipo e o intervalo de cada anterior que não for "termina, então começa" sem intervalo, e a Linha do tempo MUST desenhar a seta de "começa junto" de início a início e a de "termina junto" de fim a fim; o Excel MUST dizê-los como o MS Project em português ("5II+2") (ADR-0137).

#### Scenario: Tipo e intervalo no detalhe
- **WHEN** a gerente escolhe, em Testes integrados, "Começa junto" e o intervalo 1 para Parametrizar regras de ICMS e salva
- **THEN** a API recebe a dependência com o tipo e o intervalo, e a coluna "Depende de" diz "Parametrizar regras de ICMS (começa junto, +1)"

#### Scenario: Setas dos tipos na Linha do tempo
- **WHEN** a Linha do tempo mostra uma dependência "começa junto" e outra "termina junto"
- **THEN** a primeira sai do início da anterior para o início da seguinte, e a segunda do fim da anterior para o fim da seguinte

#### Scenario: Dependências no Excel
- **WHEN** uma pessoa do projeto exporta para o Excel um item que depende da linha 3 sem intervalo, da linha 5 "começa junto" com +2 e da linha 7 "termina junto" com -1
- **THEN** a coluna "Depende de (Nº)" diz "3; 5II+2; 7TT-1"

### Requirement: Tipo e intervalo no MS Project

O XML do MS Project MUST levar o tipo ("termina, então começa", "começa junto", "termina junto") e o intervalo em dias de cada dependência, e a importação MUST trazê-los. "Começa, então termina" MUST virar "termina, então começa", e um intervalo em horas, dias corridos ou porcentagem MUST virar dias úteis arredondados, cada um com o aviso e os nomes (ADR-0135, ADR-0137).

#### Scenario: Tipos no MS Project
- **WHEN** a gerente exporta um cronograma com uma dependência "começa junto" com +2 e outra "termina junto" e cria um projeto com esse arquivo
- **THEN** o XML traz `Type` 3 com `LinkLag` 9600 e `Type` 0, e o projeto novo tem as mesmas dependências e datas

### Requirement: Folga pela tela

O detalhe da tarefa e do marco MUST dizer a folga ("4 dias úteis de folga" ou "Sem folga: no caminho crítico"). Com o caminho crítico ligado em "Exibir", a Linha do tempo MUST desenhar, depois da barra de cada item com folga, uma linha fina até o último dia em que ele pode terminar, com "Folga" na legenda; o Excel MUST ter a coluna "Folga (dias úteis)" (ADR-0021, ADR-0071).

#### Scenario: Folga no detalhe
- **WHEN** uma pessoa do projeto abre o detalhe de uma tarefa com 4 dias úteis de folga e o de uma tarefa do caminho crítico
- **THEN** o primeiro diz "4 dias úteis de folga" e o segundo "Sem folga: no caminho crítico"

#### Scenario: Folga na Linha do tempo
- **WHEN** a Linha do tempo mostra, com o caminho crítico ligado, uma tarefa que termina em 28/10/2026 e pode terminar até 03/11/2026
- **THEN** uma linha fina vai do fim da barra até o fim de 03/11/2026, e a legenda diz "Folga"; com o caminho crítico desligado, a linha some

#### Scenario: Folga no Excel
- **WHEN** uma pessoa do projeto exporta para o Excel um cronograma com uma tarefa de 4 dias úteis de folga
- **THEN** a coluna "Folga (dias úteis)" da linha dela diz 4

### Requirement: Notas da tarefa e do marco

Cada tarefa, também a resumo e a subtarefa, e cada marco MAY ter uma nota de texto simples, com quebras de linha, de até 2.000 caracteres. Só quem edita o cronograma MUST escrever, trocar e apagar a nota, pelo pedido de incluir ou de editar o item; todos do projeto MUST vê-la. A nota ausente ou nula no pedido MUST ficar como estava, e a nota vazia MUST ser apagada (ADR-0043, ADR-0139).

#### Scenario: Escrever a nota
- **WHEN** a gerente escreve na tarefa "Configurar plano de contas" a nota "Aguardando o cliente mandar os arquivos do plano de contas"
- **THEN** o cronograma traz a tarefa com a nota, para todos do projeto

#### Scenario: Nota mantida sem o campo
- **WHEN** a gerente arrasta a tarefa com nota na Linha do tempo, num pedido sem a nota
- **THEN** a tarefa muda de datas e continua com a nota

#### Scenario: Apagar a nota
- **WHEN** a gerente salva a tarefa com a nota vazia
- **THEN** a tarefa fica sem nota

#### Scenario: Nota longa demais
- **WHEN** a gerente escreve uma nota de 2.001 caracteres
- **THEN** a API recusa com 400 no campo da nota, e nada muda

### Requirement: Notas no desfazer e na auditoria

A nota MUST entrar no desfazer de quem a mudou e na auditoria, e MUST NOT mudar as datas, o progresso, a linha de base nem os modelos de projeto (ADR-0039, ADR-0091, ADR-0139).

#### Scenario: Desfazer a nota
- **WHEN** a gerente escreve a nota de uma tarefa e escolhe "Desfazer"
- **THEN** a tarefa volta à nota de antes, e a auditoria tem as duas mudanças

### Requirement: Notas no MS Project

A exportação para o MS Project MUST gravar a nota de cada item no campo de notas da tarefa, e a importação MUST trazer a nota de cada tarefa e marco do arquivo, cortada em 2.000 caracteres com um aviso que diz os itens cortados (ADR-0135, ADR-0139).

#### Scenario: Nota exportada para o MS Project
- **WHEN** a gerente baixa o XML do MS Project de um cronograma com uma tarefa com nota
- **THEN** a tarefa do arquivo tem a mesma nota

#### Scenario: Nota importada do MS Project
- **WHEN** quem cria projetos importa um arquivo com uma tarefa com nota e outra com uma nota de 2.500 caracteres
- **THEN** o projeto nasce com a primeira nota inteira e a segunda cortada em 2.000 caracteres, e a prévia avisa o corte com o nome da tarefa

### Requirement: Notas pela tela

A Lista e a tabela da Linha do tempo MUST mostrar um ícone de nota ao lado do nome do item com nota, com o texto da nota ao apontar e para quem lê a tela; o detalhe MUST mostrar a nota, e quem edita MUST ter o campo "Notas" no formulário do item; o Excel MUST ter a coluna "Notas" (ADR-0139).

#### Scenario: Nota na Lista
- **WHEN** alguém do projeto abre a Lista de um cronograma com uma tarefa com nota
- **THEN** o nome da tarefa tem o ícone de nota, com o texto da nota

#### Scenario: Escrever a nota pela tela
- **WHEN** a gerente abre o detalhe da tarefa, escreve a nota no campo "Notas" e salva
- **THEN** a tarefa passa a mostrar o ícone de nota, e a tela confirma

#### Scenario: Nota no detalhe de quem só vê
- **WHEN** quem não edita o cronograma abre o detalhe de uma tarefa com nota
- **THEN** o detalhe mostra a nota, sem campo para mudá-la

#### Scenario: Nota no Excel
- **WHEN** alguém do projeto baixa o Excel do cronograma
- **THEN** a aba Cronograma tem a coluna "Notas", com a nota de cada item

### Requirement: Mover o cronograma

Quem edita o cronograma MUST poder mover, de uma vez, cada tarefa e marco com datas próprias em aberto (tarefa abaixo de 100%, marco não atingido) de 1 a 999 dias úteis do calendário da instalação, para depois ou para antes, com a mesma duração em dias úteis; depois do movimento, cada item MUST começar depois das anteriores pelas dependências (ADR-0018, ADR-0140).

#### Scenario: Mover para depois
- **WHEN** a gerente move o cronograma 5 dias úteis para depois, com um feriado no caminho
- **THEN** cada tarefa e marco em aberto anda 5 dias úteis, pulando o feriado, com a mesma duração, e o fim previsto do projeto anda junto

#### Scenario: Mover para antes
- **WHEN** a gerente move o cronograma 3 dias úteis para antes, com uma tarefa em aberto que depende de uma concluída
- **THEN** os itens em aberto andam 3 dias úteis para antes, e a tarefa que depende da concluída só começa depois dela

#### Scenario: Movimento inválido
- **WHEN** a gerente pede um movimento de 0 ou de 1.000 dias úteis
- **THEN** a API recusa com 400 no campo dos dias, e nada muda

#### Scenario: Mover sem editar
- **WHEN** o líder técnico ou alguém do projeto que não edita pede o movimento
- **THEN** a API recusa com 403, e nada muda

### Requirement: O que não anda no movimento

No movimento do cronograma, as tarefas a 100%, os marcos atingidos, as datas-limite e a linha de base MUST NOT andar, e as tarefas-resumo MUST seguir as subtarefas (ADR-0021, ADR-0088, ADR-0140).

#### Scenario: Concluídos parados
- **WHEN** a gerente move o cronograma com uma tarefa a 100% e um marco atingido
- **THEN** a tarefa e o marco ficam nas datas deles, e as datas-limite e a linha de base também

### Requirement: Movimento no desfazer e na auditoria

O movimento MUST ser uma mudança só no desfazer de quem o fez, MUST ir para a auditoria e MUST avisar os responsáveis das tarefas que mudam de datas (ADR-0037, ADR-0039, ADR-0091).

#### Scenario: Desfazer o movimento
- **WHEN** a gerente move o cronograma e escolhe "Desfazer"
- **THEN** todos os itens voltam às datas de antes num passo só, e a auditoria tem as mudanças

### Requirement: Mover o cronograma pela tela

O "⋮" da aba Cronograma MUST ter "Mover o cronograma", para quem edita, num cronograma com itens em aberto, que abre o diálogo com a direção (para depois ou para antes), os dias úteis e quantos itens andam; ao confirmar, a tela MUST dizer quantos itens mudaram de datas, e o desfazer MUST dizer "o movimento do cronograma" (ADR-0140).

#### Scenario: Mover pela tela
- **WHEN** a gerente escolhe "Mover o cronograma", "Para depois" e 5 dias úteis e confirma
- **THEN** a tela manda o movimento de 5 dias úteis, confirma quantos itens mudaram, e o "Desfazer" diz "o movimento do cronograma"

#### Scenario: Dias inválidos pela tela
- **WHEN** a gerente digita 0 ou 1000 dias úteis
- **THEN** o diálogo mostra o erro no campo e não manda nada

#### Scenario: Sem mover para quem não edita
- **WHEN** quem não edita o cronograma abre o "⋮" da aba
- **THEN** o menu não tem "Mover o cronograma"

### Requirement: Responsável ausente durante a tarefa

Cada tarefa em aberto com datas próprias e uma pessoa como responsável MUST trazer as ausências dessa pessoa que se cruzam com as datas da tarefa; a Lista MUST mostrar o aviso ao lado do responsável, e o detalhe, os períodos. As datas da tarefa MUST NOT mudar pela ausência (ADR-0019, ADR-0141).

#### Scenario: Tarefa na ausência do responsável
- **WHEN** a Carla, responsável pela tarefa de 03/11 a 07/11, tem ausência de 05/11 a 14/11
- **THEN** a tarefa traz a ausência de 05/11 a 14/11, a Lista mostra o aviso ao lado da Carla, e o detalhe diz "Responsável ausente: 05/11 a 14/11, durante a tarefa"

#### Scenario: Sem aviso na concluída
- **WHEN** a tarefa da Carla está a 100% ou a ausência não se cruza com as datas dela
- **THEN** a tarefa não traz ausências

### Requirement: Modo de edição na Lista

A Lista do cronograma MUST ter, no computador, o botão "Editar na tabela", para quem edita o cronograma ou atualiza o progresso de alguma tarefa. Ligado, a linha MUST NOT abrir o detalhe, e a tela MUST dizer como editar pelo teclado; desligado, a Lista MUST voltar a abrir o detalhe pela linha (ADR-0143).

#### Scenario: Ligar o modo de edição
- **WHEN** a gerente liga "Editar na tabela" na Lista
- **THEN** o botão fica marcado, a linha de ajuda aparece, e escolher uma linha não abre o detalhe

#### Scenario: Modo de edição de quem só vê
- **WHEN** o leitor abre a Lista
- **THEN** "Editar na tabela" não aparece

### Requirement: Células editáveis na Lista

No modo de edição, quem edita o cronograma MUST poder mudar o nome de toda tarefa e marco, e o início, o fim (só da tarefa), a duração (só da tarefa) e "Depende de" de quem tem datas próprias; o progresso, quem edita e o responsável, das tarefas com datas próprias. A tarefa-resumo MUST ter só o nome editável, e as fases MUST NOT se editar na tabela (ADR-0088, ADR-0143).

#### Scenario: Células da tarefa-resumo e do marco
- **WHEN** a gerente tenta editar a duração da tarefa-resumo e o fim de um marco
- **THEN** as células não entram em edição, e o nome deles entra

#### Scenario: Progresso do responsável na tabela
- **WHEN** a Carla, que não edita o cronograma, liga o modo de edição
- **THEN** só o progresso das tarefas dela entra em edição

### Requirement: Salvar pela célula

Cada célula salva MUST pedir à API o mesmo que o detalhe ou a Linha do tempo pedem: o nome, a duração e "Depende de" pelo pedido de editar o item, com os outros campos como estão; o início pelo pedido das datas, sem o fim; o fim pelo pedido das datas, com o início; o progresso pelo pedido do progresso. A tela MUST avisar o que foi salvo e mostrar o cronograma de novo (ADR-0143).

#### Scenario: Duração pela célula
- **WHEN** a gerente edita a duração de "Integrar emissão de NF-e" para 10 e aperta Enter
- **THEN** a tela pede a edição do item com a duração 10 e os outros campos como estavam, e avisa que a tarefa foi salva

#### Scenario: Datas pela célula
- **WHEN** a gerente digita outro início e, depois, outro fim de uma tarefa
- **THEN** a tela pede as datas com o início e o fim vazio e, depois, com o início e o fim

#### Scenario: Dependências pela célula
- **WHEN** a gerente escolhe mais uma tarefa em "Depende de" e sai da célula
- **THEN** a tela pede a edição do item com as dependências escolhidas

#### Scenario: Progresso pela célula
- **WHEN** a gerente digita 60 no progresso de uma tarefa e aperta Tab
- **THEN** a tela pede o progresso 60

### Requirement: Valor inválido na célula

A tela MUST conferir a célula antes de mandar: o nome de 1 a 200 caracteres, o dia como dd/mm/aaaa, o fim no início ou depois, a duração de 1 a 999 dias úteis e o progresso de 0 a 100; com valor inválido, a célula MUST continuar em edição, com o motivo, sem pedir nada. A recusa do servidor MUST voltar a célula ao valor de antes, com o aviso (ADR-0143).

#### Scenario: Duração inválida na célula
- **WHEN** a gerente digita 0 na duração e aperta Enter
- **THEN** a célula continua em edição, diz o motivo, e nada é pedido

#### Scenario: Dependência circular pela célula
- **WHEN** a API recusa a dependência escolhida por ser circular
- **THEN** a célula volta às dependências de antes, e a tela avisa a dependência circular
