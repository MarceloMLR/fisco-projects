## MODIFIED Requirements

### Requirement: Permissões nas horas

Apontar horas MUST ser do administrador, em qualquer projeto, e das pessoas do projeto, menos o leitor. Corrigir e apagar as horas de outra pessoa MUST ser só do administrador, e ver as horas de outra pessoa também, salvo os apontamentos de uma tarefa, que o gerente do projeto vê nos projetos que gerencia (ADR-0144). Quem não está no projeto MUST receber 404, o leitor 403, e o projeto concluído ou cancelado MUST responder 409 (ADR-0016, ADR-0040, ADR-0128).

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

### Requirement: Horas das tarefas

O administrador, em qualquer projeto, e o gerente do projeto, nos projetos que gerencia, MUST ver as tarefas com horas previstas ou apontadas, na ordem do cronograma, cada uma com o nome atual, as previstas, as apontadas nela por qualquer pessoa, o saldo, o consumo e o estado, e os totais do projeto: as previstas nas tarefas, as apontadas nelas e as apontadas fora das tarefas do cronograma, sem tarefa ou numa tarefa excluída. A resposta MUST dizer se quem pede pode mudar as previstas (ADR-0128, ADR-0138, ADR-0144).

#### Scenario: Horas fora das tarefas
- **WHEN** um projeto tem horas apontadas sem tarefa e numa tarefa que foi excluída do cronograma
- **THEN** essas horas contam como apontadas fora das tarefas, e a tarefa excluída não aparece

#### Scenario: Tarefa sem horas previstas
- **WHEN** uma tarefa tem só horas apontadas
- **THEN** a tarefa aparece sem horas previstas, sem saldo nem consumo

#### Scenario: Horas das tarefas pela gerente
- **WHEN** a gerente do projeto pede as horas das tarefas de um projeto que gerencia, também depois de concluído
- **THEN** a API devolve as tarefas com as previstas e as apontadas, e diz que ela não pode mudar as previstas

### Requirement: Permissões nas horas previstas

Ver as horas das tarefas e os apontamentos de uma tarefa MUST ser permitido ao administrador e ao gerente do projeto e negado, com 403, ao gerente de outro projeto, ao líder técnico, ao colaborador, ao desenvolvedor e ao leitor do projeto; quem não está no projeto e não é administrador MUST receber 404. Pôr, trocar e tirar as horas previstas MUST ser só do administrador, com 403 para todos os outros, mesmo no projeto deles (ADR-0046, ADR-0128, ADR-0138, ADR-0144).

#### Scenario: Permissões nas horas previstas por papel
- **WHEN** cada papel tenta ver as horas das tarefas de um projeto, ver os apontamentos de uma tarefa dele e pôr as horas previstas nessa tarefa
- **THEN** o administrador consegue as três; o gerente do projeto vê as horas e os apontamentos e recebe 403 ao pôr as previstas; o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403 em cada uma; quem está fora recebe 404 nas leituras e 403 ao pôr; e nada muda

#### Scenario: Gerente de outro projeto
- **WHEN** a gerente pede as horas das tarefas de um projeto em que é colaboradora
- **THEN** a API responde 403

### Requirement: Horas na Lista do cronograma pela tela

Para o administrador e para o gerente do projeto, a Lista do cronograma MUST ter a coluna "Horas", com as apontadas e as previstas de cada tarefa ("12 de 16 h"), o estado por ícone, cor e texto quando a tarefa está acima ou perto das previstas, e só as apontadas quando a tarefa não tem previstas. Para as outras pessoas, a Lista MUST NOT mostrar horas nem pedi-las à API (ADR-0065, ADR-0138, ADR-0144).

#### Scenario: Horas na Lista do administrador
- **WHEN** o administrador abre a Lista de um cronograma com uma tarefa de 52 horas apontadas e 40 previstas
- **THEN** a coluna "Horas" mostra "52 de 40 h" com o ícone e a cor de acima das previstas

#### Scenario: Horas na Lista da gerente
- **WHEN** a gerente do projeto abre a Lista do cronograma
- **THEN** a coluna "Horas" mostra as horas de cada tarefa, como para o administrador

#### Scenario: Cronograma sem horas para a gestão
- **WHEN** o líder técnico do projeto abre a Lista e o detalhe de uma tarefa
- **THEN** a tela não tem a coluna "Horas" nem a seção "Horas", e as horas não são pedidas

### Requirement: Horas no detalhe da tarefa pela tela

Para o administrador e para o gerente do projeto, o detalhe de uma tarefa MUST ter a seção "Horas", com as previstas, as apontadas, o estado e a barra do consumo. "Alterar", que abre o diálogo das previstas, com o campo vazio para tirar, MUST aparecer só para quem pode mudá-las, o administrador. Com horas apontadas na tarefa, "Ver apontamentos" MUST aparecer ao lado das apontadas (ADR-0138, ADR-0144).

#### Scenario: Alterar as previstas pelo detalhe da tarefa
- **WHEN** o administrador abre o detalhe de uma tarefa, escolhe "Alterar" na seção "Horas", informa 48 e salva
- **THEN** a seção e a coluna mostram as 48 horas previstas, e a tela confirma

#### Scenario: Horas no detalhe para a gerente
- **WHEN** a gerente do projeto abre o detalhe de uma tarefa com horas previstas e apontadas
- **THEN** a seção "Horas" mostra as previstas, as apontadas, o saldo e o consumo e "Ver apontamentos", sem "Alterar"

## ADDED Requirements

### Requirement: Apontamentos da tarefa

O administrador e o gerente do projeto MUST ver os apontamentos de uma tarefa do cronograma, também a resumo e a subtarefa, de todas as pessoas: a pessoa pelo nome atual, o dia, as horas, a descrição e se quem pede pode corrigi-lo e apagá-lo, pelas regras do apontamento, do dia mais recente para o mais antigo, com o total. O marco, a fase e a tarefa de outro projeto MUST responder 404 (ADR-0128, ADR-0144).

#### Scenario: Apontamentos de uma tarefa
- **WHEN** a colaboradora aponta 8 horas e o desenvolvedor 5 horas numa tarefa, em dias diferentes, e a gerente do projeto pede os apontamentos dela
- **THEN** a API devolve os dois apontamentos, do mais recente para o mais antigo, com a pessoa, o dia, as horas e a descrição de cada um e o total de 13 horas, sem os apontamentos de outras tarefas

#### Scenario: Correção pelos apontamentos da tarefa
- **WHEN** a gerente do projeto e o administrador pedem os apontamentos de uma tarefa com um apontamento da gerente e um da colaboradora
- **THEN** para a gerente, só o apontamento dela pode ser corrigido; para o administrador, os dois

#### Scenario: Apontamentos fora de uma tarefa
- **WHEN** o administrador pede os apontamentos de um marco, de uma fase ou de uma tarefa de outro projeto
- **THEN** a API responde 404

### Requirement: Apontamentos da tarefa pela tela

Na seção "Horas" do detalhe de uma tarefa, "Ver apontamentos" MUST abrir os apontamentos da tarefa num diálogo com o nome da tarefa, agrupados por dia, do mais recente para o mais antigo, cada um com a pessoa, a descrição e as horas, o total da tarefa no rodapé e o "⋮" de corrigir e apagar só nos que a pessoa pode corrigir; corrigir ou apagar MUST atualizar as horas da tarefa (ADR-0081, ADR-0128, ADR-0144).

#### Scenario: Ver apontamentos pelo detalhe da tarefa
- **WHEN** a gerente do projeto escolhe "Ver apontamentos" na seção "Horas" de uma tarefa com apontamentos de duas pessoas
- **THEN** o diálogo mostra cada apontamento com a pessoa, o dia, as horas e a descrição e o total da tarefa, com o "⋮" só no apontamento dela
