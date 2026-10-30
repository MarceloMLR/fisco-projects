## ADDED Requirements

### Requirement: Apontamento de horas

Quem aponta horas num projeto MUST incluir o apontamento com o dia, as horas, a descrição (até 500 caracteres) e o projeto. O dia MUST ser no máximo hoje, no fuso da instalação (ADR-0013). As horas MUST ir de 0,25 a 24, em quartos de hora, e a soma dos apontamentos da pessoa num dia MUST não passar de 24 horas. O apontamento MUST guardar quem apontou e quando. Os erros MUST voltar no campo, em Problem Details (ADR-0128).

#### Scenario: Apontar horas no projeto
- **WHEN** a colaboradora aponta 2,5 horas de ontem no projeto ERP-FIN, com a descrição "Reunião de levantamento"
- **THEN** a API grava o apontamento com ela como autora e o devolve na lista dela, com o projeto, o dia, as horas e a descrição

#### Scenario: Dia depois de hoje
- **WHEN** alguém aponta horas para amanhã
- **THEN** a API recusa com 400 no campo do dia, e nada é gravado

#### Scenario: Horas fora dos quartos de hora
- **WHEN** alguém aponta 1,1 hora, 0 hora ou 25 horas
- **THEN** a API recusa com 400 no campo das horas

#### Scenario: Mais de 24 horas no dia
- **WHEN** a pessoa já tem 20 horas apontadas num dia e aponta mais 5 nesse dia, em qualquer projeto
- **THEN** a API recusa com 400 no campo das horas, e o apontamento não é gravado

#### Scenario: Apontamento sem descrição
- **WHEN** alguém aponta horas com a descrição em branco
- **THEN** a API recusa com 400 no campo da descrição

### Requirement: Tarefa ou chamado do apontamento

O apontamento MAY indicar uma tarefa do cronograma do projeto ou um chamado do projeto, nunca os dois. A tarefa MUST ser uma tarefa do cronograma daquele projeto, e não uma fase ou um marco; o chamado MUST ser daquele projeto. Uma tarefa excluída depois MUST deixar o apontamento no projeto, mostrado como tarefa excluída (ADR-0128).

#### Scenario: Apontar numa tarefa
- **WHEN** o desenvolvedor aponta 3 horas na tarefa "Configurar o ambiente" do ERP-FIN
- **THEN** a lista dele mostra o apontamento com o nome da tarefa

#### Scenario: Apontar num chamado
- **WHEN** a colaboradora aponta 1 hora no chamado #104 do ERP-FIN
- **THEN** a lista dela mostra o apontamento com o número e o título do chamado

#### Scenario: Tarefa de outro projeto
- **WHEN** alguém aponta horas no ERP-FIN indicando uma tarefa do TMS-DELTA, um marco ou uma fase
- **THEN** a API recusa com 400 no campo da tarefa

#### Scenario: Chamado de outro projeto
- **WHEN** alguém aponta horas no ERP-FIN indicando um chamado do TMS-DELTA
- **THEN** a API recusa com 400 no campo do chamado

#### Scenario: Tarefa e chamado juntos
- **WHEN** alguém indica uma tarefa e um chamado no mesmo apontamento
- **THEN** a API recusa com 400

#### Scenario: Tarefa excluída depois
- **WHEN** a tarefa de um apontamento é excluída do cronograma
- **THEN** o apontamento continua no projeto, com as mesmas horas, e a lista o mostra como tarefa excluída

### Requirement: Horas de cada pessoa

Cada pessoa MUST ver, corrigir e apagar só as próprias horas, por período de até 93 dias, com o total do período, e cada apontamento da lista MUST dizer se quem pede pode corrigi-lo e apagá-lo. Corrigir MUST seguir as regras do apontamento, também no projeto de destino quando o projeto muda (ADR-0128).

#### Scenario: Só as próprias horas
- **WHEN** a colaboradora pede as horas da semana
- **THEN** a API devolve só os apontamentos dela, de todos os projetos, e o total da semana

#### Scenario: Horas de outra pessoa
- **WHEN** a colaboradora tenta ver, corrigir ou apagar um apontamento do desenvolvedor, ou pede a lista de outra pessoa
- **THEN** a API responde 404 ao apontamento e 403 à lista, e nada muda

#### Scenario: Período longo demais
- **WHEN** alguém pede as horas de um período de mais de 93 dias
- **THEN** a API recusa com 400

### Requirement: Horas de outra pessoa pelo administrador

O administrador MUST ver, corrigir e apagar as horas de qualquer pessoa, com as mesmas regras do apontamento (ADR-0046, ADR-0128).

#### Scenario: Administrador corrige as horas de outra pessoa
- **WHEN** o administrador pede as horas do desenvolvedor e corrige um apontamento dele de 8 para 6 horas
- **THEN** a lista do desenvolvedor passa a mostrar 6 horas, e a auditoria registra a correção pelo administrador

### Requirement: Horas de quem saiu do projeto

Quem saiu do projeto ou passou a leitor MUST não apontar nem corrigir horas nele, e as horas que apontou MUST continuar no projeto (ADR-0128).

#### Scenario: Quem saiu do projeto
- **WHEN** a colaboradora sai do ERP-FIN e tenta apontar ou corrigir horas nele
- **THEN** a API recusa, e os apontamentos que ela fez continuam no projeto e na lista dela, sem as ações de corrigir e apagar

### Requirement: Horas na auditoria

Toda inclusão, correção e exclusão de um apontamento MUST ir para a auditoria, com o projeto, a pessoa, a tarefa e o chamado pelos nomes atuais (ADR-0039, ADR-0084).

#### Scenario: Horas na auditoria
- **WHEN** alguém inclui, corrige e apaga um apontamento
- **THEN** a auditoria tem os três registros, com o projeto e a pessoa pelos nomes atuais

### Requirement: Permissões nas horas

Apontar horas MUST ser do administrador, em qualquer projeto, e das pessoas do projeto, menos o leitor. Ver, corrigir e apagar as horas de outra pessoa MUST ser só do administrador. Quem não está no projeto MUST receber 404, o leitor 403, e o projeto concluído ou cancelado MUST responder 409 (ADR-0016, ADR-0040, ADR-0128).

#### Scenario: Permissões nas horas por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor tentam apontar horas no projeto, corrigir e apagar o próprio apontamento e ver, corrigir e apagar o apontamento de outra pessoa
- **THEN** o administrador faz tudo; o gerente, o líder técnico, o colaborador e o desenvolvedor apontam, corrigem e apagam os próprios e recebem 404 no de outra pessoa; o leitor recebe 403 ao apontar e 404 no de outra pessoa

#### Scenario: Administrador fora do projeto
- **WHEN** o administrador, sem papel no projeto, aponta horas nele
- **THEN** a API grava o apontamento

#### Scenario: Horas de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador aponta horas nele
- **THEN** a API responde 404

#### Scenario: Horas de projeto encerrado
- **WHEN** alguém aponta, corrige ou apaga horas de um projeto concluído
- **THEN** a API responde 409 e nada muda

### Requirement: Opções para apontar

A API MUST dizer à pessoa em que projetos ela aponta horas, os em aberto em que tem papel, menos o de leitor, e, para o administrador, todos os em aberto, com o código e o nome. Para um desses projetos, MUST dizer as tarefas do cronograma, na ordem do cronograma, e os chamados, primeiro os não fechados, com o número e o título (ADR-0128).

#### Scenario: Projetos para apontar
- **WHEN** a colaboradora, que está no ERP-FIN, no TMS-DELTA como leitora e num projeto concluído, pede os projetos para apontar
- **THEN** a API devolve só o ERP-FIN

#### Scenario: Tarefas e chamados do projeto
- **WHEN** a colaboradora pede as opções do ERP-FIN
- **THEN** a API devolve as tarefas do cronograma, sem as fases e os marcos, e os chamados do projeto

### Requirement: Minhas horas pela tela

A aplicação MUST ter a tela Minhas horas, com a semana de segunda a domingo: os apontamentos da pessoa agrupados por dia, com o total de cada dia e da semana, cada um com o projeto, a tarefa ou o chamado, a descrição, as horas e o "⋮" com corrigir e apagar; a semana anterior, a seguinte e "Esta semana". Os dias seguem o fuso da instalação, e no celular cada apontamento mostra o projeto, a descrição e as horas (ADR-0013, ADR-0065, ADR-0066).

#### Scenario: Semana pela tela
- **WHEN** a pessoa abre Minhas horas
- **THEN** a tela mostra a semana atual, com os apontamentos dela agrupados por dia, o total de cada dia e o total da semana

#### Scenario: Corrigir e apagar pela tela
- **WHEN** a pessoa corrige as horas de um apontamento pelo "⋮" e depois apaga outro, confirmando
- **THEN** a lista e os totais mudam a cada vez

#### Scenario: Trocar a semana
- **WHEN** a pessoa vai para a semana anterior e depois escolhe "Esta semana"
- **THEN** a tela mostra os apontamentos da semana anterior e depois volta à semana atual

#### Scenario: Semana sem horas
- **WHEN** a pessoa abre uma semana sem apontamentos
- **THEN** a tela diz que não há horas na semana e oferece "Apontar horas"

#### Scenario: Minhas horas no celular
- **WHEN** a pessoa abre Minhas horas numa tela menor que 900px
- **THEN** cada apontamento mostra o projeto, a descrição e as horas, sem rolagem horizontal da página

### Requirement: Apontar pela tela

"Apontar horas" MUST abrir o diálogo com o dia (hoje, de início), o projeto, a tarefa ou o chamado opcional, as horas, aceitando "1,5" e "1:30", e a descrição; o erro da API MUST voltar ao campo (ADR-0128).

#### Scenario: Apontar pela tela
- **WHEN** a pessoa escolhe "Apontar horas", escolhe o projeto e uma tarefa, digita "1:30" e a descrição e confirma
- **THEN** o apontamento de 1,5 hora aparece no dia escolhido, os totais mudam e a tela confirma

#### Scenario: Erro no campo
- **WHEN** a pessoa aponta horas que passam de 24 no dia
- **THEN** o diálogo continua aberto, com o erro no campo das horas
