## MODIFIED Requirements

### Requirement: Tarefa atrasada

Uma tarefa MUST ficar atrasada quando o fim é anterior a hoje, no fuso da instalação, e o progresso está abaixo de 100%. Uma fase com uma tarefa atrasada MUST ficar atrasada. O cronograma MUST dizer quantas tarefas estão atrasadas, contando as subtarefas e não a tarefa-resumo. A API MUST dizer há quantos dias úteis cada tarefa está atrasada (`lateDays`): os dias úteis do calendário da instalação do dia seguinte ao fim até hoje, zero quando a tarefa não está atrasada; na tarefa-resumo, os da subtarefa mais atrasada (ADR-0018, ADR-0020, ADR-0088).

#### Scenario: Tarefa vencida
- **WHEN** hoje é 16/10/2026 e uma tarefa terminou em 15/10/2026 a 80%
- **THEN** a tarefa e a fase dela aparecem atrasadas, e o cronograma conta uma tarefa atrasada

#### Scenario: Tarefa concluída ou do dia não fica atrasada
- **WHEN** hoje é 16/10/2026, uma tarefa terminou em 15/10/2026 a 100% e outra termina hoje a 50%
- **THEN** nenhuma das duas aparece atrasada

#### Scenario: Dias de atraso da tarefa
- **WHEN** hoje é 19/10/2026 e uma subtarefa terminou em 15/10/2026 a 80%
- **THEN** ela tem 2 dias úteis de atraso, 16/10 e 19/10, sem contar o sábado e o domingo, e a tarefa-resumo dela também

#### Scenario: Atraso que começou num dia não útil
- **WHEN** hoje é o sábado 17/10/2026 e uma tarefa terminou na sexta 16/10/2026 a 80%
- **THEN** a tarefa está atrasada, com zero dia útil de atraso

### Requirement: Marco atrasado

Um marco não atingido MUST ficar atrasado quando a data é anterior a hoje, no fuso da instalação. Uma fase com um marco atrasado MUST ficar atrasada, e o cronograma MUST dizer quantos marcos estão atrasados, além das tarefas. A API MUST dizer há quantos dias úteis cada marco está atrasado, contados como os da tarefa (ADR-0018, ADR-0086).

#### Scenario: Marco vencido
- **WHEN** hoje é 16/10/2026 e um marco de 15/10/2026 não foi atingido
- **THEN** o marco e a fase dele aparecem atrasados, e o cronograma conta um marco atrasado

#### Scenario: Marco atingido ou do dia não fica atrasado
- **WHEN** hoje é 16/10/2026, um marco de 15/10/2026 foi atingido e outro é de hoje
- **THEN** nenhum dos dois aparece atrasado

#### Scenario: Dias de atraso do marco
- **WHEN** hoje é 16/10/2026 e um marco de 09/10/2026 não foi atingido
- **THEN** o marco tem 4 dias úteis de atraso, sem contar o fim de semana e o feriado de 12/10/2026

### Requirement: Aba Cronograma

A página do projeto MUST ter a aba Cronograma, entre Visão geral e Membros, que alterna entre Linha do tempo e Lista; no computador abre na Linha do tempo e no celular, na Lista. A Lista MUST ter as fases em linhas de grupo, com datas e progresso, e abaixo delas as tarefas e os marcos em ordem de início, com início, fim, duração, responsável, progresso e dependências. O marco mostra Atingido, Pendente ou Atrasado. A tarefa e o marco atrasados MUST dizer na coluna Fim há quantos dias úteis ("1 dia útil de atraso", "3 dias úteis de atraso"): a tarefa embaixo da data, e o marco, que não tem fim, só com o atraso. Com zero dia útil, a tarefa diz "Atrasada", e o marco fica só com "Atrasado" no estado (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma pela tela
- **WHEN** hoje é 16/10/2026 e uma pessoa do projeto abre a Lista da aba Cronograma de um projeto com fases, tarefas, um marco e uma tarefa que terminou em 15/10/2026 a 80%
- **THEN** a tela mostra as fases com as datas e o progresso, as tarefas e o marco abaixo delas, a tarefa atrasada com "1 dia útil de atraso" embaixo do fim, e a trilha mostra Projetos, o código e Cronograma

#### Scenario: Cronograma vazio
- **WHEN** o gerente abre a aba Cronograma de um projeto sem fases
- **THEN** a tela explica que o cronograma ainda não tem fases e oferece "Nova fase"

#### Scenario: Estados do marco pela tela
- **WHEN** hoje é 16/10/2026 e o cronograma tem um marco atingido em 09/10/2026, um pendente em 03/11/2026 e um não atingido de 15/10/2026
- **THEN** a Lista mostra "Atingido em 09/10", "Pendente" e "Atrasado", este com ícone e cor e com "1 dia útil de atraso" na coluna Fim, e o resumo conta um marco atrasado

#### Scenario: Atraso desde um dia não útil pela tela
- **WHEN** a API diz que uma tarefa e um marco estão atrasados com zero dia útil de atraso
- **THEN** a Lista mostra "Atrasada" embaixo do fim da tarefa, e o marco fica só com "Atrasado" no estado

#### Scenario: Alternar Linha do tempo e Lista
- **WHEN** uma pessoa abre a aba Cronograma no computador e depois escolhe "Lista"
- **THEN** a aba abre na Linha do tempo e passa para a Lista, e no celular a aba abre na Lista

### Requirement: Linha do tempo

A Linha do tempo MUST mostrar à esquerda o nome e o progresso de cada fase, tarefa e marco e, à direita, as fases como barras de resumo, as tarefas como barras com a parte concluída em verde, os marcos como losangos, as dependências como setas, os dias não úteis sombreados e a linha de hoje com "Hoje". A tarefa atrasada MUST ter ícone e cor, e o ícone MUST dizer há quantos dias úteis ela está atrasada. A palavra "Gantt" MUST NOT aparecer (ADR-0051, ADR-0065, ADR-0067, ADR-0071).

#### Scenario: Linha do tempo pela tela
- **WHEN** hoje é 16/10/2026 e uma pessoa do projeto abre a Linha do tempo de um projeto com fases, tarefas, um marco, dependências e uma tarefa que terminou em 15/10/2026 a 80%
- **THEN** a tela mostra cada fase com as tarefas e o marco abaixo dela, com o progresso, a tarefa atrasada com ícone e cor e o ícone dizendo "1 dia útil de atraso", as barras nas datas do cronograma e a linha de hoje em 16/10/2026

#### Scenario: Dias não úteis na linha do tempo
- **WHEN** a instalação considera o feriado de 12/10/2026 e trabalha de segunda a sexta
- **THEN** a Linha do tempo sombreia os sábados, os domingos e 12/10/2026

### Requirement: Detalhe pela linha do tempo

Escolher uma tarefa ou um marco na Linha do tempo MUST abrir o mesmo detalhe da Lista, e escolher uma fase, para quem edita, MUST abrir a fase. Fechado o detalhe, a Linha do tempo MUST NOT manter a linha escolhida, e escolher a mesma barra ou linha de novo MUST abrir o detalhe outra vez (ADR-0081).

#### Scenario: Detalhe pela linha do tempo
- **WHEN** uma pessoa do projeto escolhe uma tarefa na Linha do tempo
- **THEN** o detalhe da tarefa abre na gaveta, como na Lista

#### Scenario: Reabrir o detalhe pela linha do tempo
- **WHEN** uma pessoa escolhe a barra de uma tarefa na Linha do tempo, fecha o detalhe e escolhe a mesma barra de novo
- **THEN** a linha deixa de estar escolhida ao fechar, e o detalhe da tarefa abre de novo

### Requirement: Subtarefas pela tela

A Lista e a Linha do tempo MUST mostrar as subtarefas abaixo da tarefa, e a tarefa-resumo com as datas e o progresso calculados. Na Lista, a tarefa-resumo MUST ter uma seta que recolhe e mostra as subtarefas, sem abrir o detalhe; elas começam à mostra. O detalhe de uma tarefa direto na fase MUST ter a seção Subtarefas, com as subtarefas dela, cada uma abrindo o próprio detalhe, e, para quem edita, "Nova subtarefa". Quem edita MUST poder incluir uma subtarefa por "Nova subtarefa" ou pelo campo "Dentro de". A tarefa-resumo MUST NOT se arrastar nem se ligar na Linha do tempo, e "Depende de" MUST NOT oferecer tarefas-resumo (ADR-0071, ADR-0081, ADR-0088).

#### Scenario: Subtarefas na lista
- **WHEN** uma pessoa do projeto abre a Lista de um cronograma com uma tarefa-resumo
- **THEN** as subtarefas aparecem abaixo dela, com um recuo a mais, e a tarefa-resumo mostra a seta de recolher, as datas, a duração e o progresso calculados

#### Scenario: Recolher subtarefas na lista
- **WHEN** uma pessoa escolhe a seta de uma tarefa-resumo na Lista e depois a escolhe de novo
- **THEN** as subtarefas somem da Lista sem abrir o detalhe, e voltam na segunda vez

#### Scenario: Nova subtarefa pela tela
- **WHEN** o gerente abre o detalhe de uma tarefa sem subtarefas, escolhe "Nova subtarefa" na seção Subtarefas e inclui uma tarefa
- **THEN** o formulário vem com "Dentro de" já escolhido e avisa que a tarefa passa a ser resumo, e a subtarefa aparece abaixo dela

#### Scenario: Subtarefas na linha do tempo
- **WHEN** o gerente abre a Linha do tempo de um cronograma com uma tarefa-resumo
- **THEN** a tarefa-resumo aparece como barra de resumo com as subtarefas abaixo, as subtarefas se arrastam e a tarefa-resumo não

#### Scenario: Detalhe da tarefa-resumo
- **WHEN** o gerente abre o detalhe de uma tarefa-resumo e escolhe uma das subtarefas na seção Subtarefas
- **THEN** o nome, a fase e o responsável se editam, o início, o fim, a duração e o progresso aparecem calculados pelas subtarefas, sem editar, e o detalhe passa para a subtarefa escolhida
