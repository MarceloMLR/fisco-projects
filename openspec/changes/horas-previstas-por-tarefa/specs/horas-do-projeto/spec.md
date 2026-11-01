## ADDED Requirements

### Requirement: Horas previstas da tarefa

Só o administrador MUST pôr, trocar e tirar as horas previstas de uma tarefa do cronograma de um projeto, de qualquer situação. A tarefa MAY ser uma tarefa-resumo ou uma subtarefa; o marco e a fase MUST NOT receber horas previstas, nem a tarefa de outro projeto. As horas previstas MUST ir de 0,25 a 9.999, em quartos de hora, e tirá-las MUST deixar a tarefa sem horas previstas (ADR-0128, ADR-0138).

#### Scenario: Pôr as horas previstas
- **WHEN** o administrador põe 40 horas previstas na tarefa "Parametrizar regras de ICMS" e depois troca para 48
- **THEN** as horas das tarefas do projeto mostram a tarefa com 48 horas previstas

#### Scenario: Tirar as horas previstas
- **WHEN** o administrador tira as horas previstas de uma tarefa sem horas apontadas
- **THEN** a tarefa sai das horas das tarefas do projeto

#### Scenario: Horas previstas inválidas
- **WHEN** o administrador informa 0, 10,1 ou 10.000 horas previstas
- **THEN** a API recusa com 400 no campo das horas, e nada muda

#### Scenario: Horas previstas fora de uma tarefa
- **WHEN** o administrador põe horas previstas num marco, numa fase ou numa tarefa de outro projeto
- **THEN** a API recusa com 404, e nada muda

### Requirement: Horas previstas fora do cronograma

As horas previstas MUST NOT mudar as datas, o progresso, o desfazer, a linha de base, os modelos nem o XML do MS Project do cronograma, e MUST sair com o projeto excluído (ADR-0091, ADR-0111, ADR-0138).

#### Scenario: Cronograma igual com as previstas
- **WHEN** o administrador põe horas previstas numa tarefa
- **THEN** o cronograma continua com as mesmas datas e o mesmo progresso, e o desfazer não oferece a mudança

#### Scenario: Horas previstas na exclusão do projeto
- **WHEN** o administrador exclui um projeto com horas previstas nas tarefas
- **THEN** as horas previstas saem com o projeto

### Requirement: Horas das tarefas

O administrador MUST ver, de um projeto, as tarefas com horas previstas ou apontadas, na ordem do cronograma, cada uma com o nome atual, as previstas, as apontadas nela por qualquer pessoa, o saldo, o consumo e o estado, e os totais do projeto: as previstas nas tarefas, as apontadas nelas e as apontadas fora das tarefas do cronograma, sem tarefa ou numa tarefa excluída (ADR-0128, ADR-0138).

#### Scenario: Horas fora das tarefas
- **WHEN** um projeto tem horas apontadas sem tarefa e numa tarefa que foi excluída do cronograma
- **THEN** essas horas contam como apontadas fora das tarefas, e a tarefa excluída não aparece

#### Scenario: Tarefa sem horas previstas
- **WHEN** uma tarefa tem só horas apontadas
- **THEN** a tarefa aparece sem horas previstas, sem saldo nem consumo

### Requirement: Estado das horas da tarefa

O estado das horas de uma tarefa MUST seguir a regra do saldo do projeto, com a tarefa concluída no lugar do projeto encerrado: acima das previstas, perto das previstas a partir de 90% numa tarefa abaixo de 100%, dentro e sem horas previstas (ADR-0065, ADR-0138).

#### Scenario: Tarefa acima das previstas
- **WHEN** uma tarefa tem 40 horas previstas e 52 apontadas por duas pessoas
- **THEN** a tarefa está acima das previstas, com o saldo de 12 horas acima e o consumo de 130%

#### Scenario: Tarefa perto das previstas
- **WHEN** uma tarefa em andamento tem 20 horas previstas e 18 apontadas
- **THEN** a tarefa está perto das previstas, restam 2 horas, e o consumo é de 90%

#### Scenario: Tarefa concluída perto das previstas
- **WHEN** uma tarefa a 100% tem 20 horas previstas e 19 apontadas
- **THEN** a tarefa está dentro das previstas, porque não recebe mais trabalho

### Requirement: Permissões nas horas previstas

Ver as horas das tarefas e pôr, trocar e tirar as horas previstas MUST ser permitido ao administrador e negado, com 403, ao gerente de projeto, ao líder técnico e ao usuário, mesmo no projeto deles (ADR-0046, ADR-0128, ADR-0138).

#### Scenario: Permissões nas horas previstas por papel
- **WHEN** cada papel no sistema tenta ver as horas das tarefas de um projeto e pôr as horas previstas numa tarefa dele
- **THEN** o administrador consegue as duas, e o gerente de projeto, o líder técnico e o usuário recebem 403 em cada uma, sem mudar nada

### Requirement: Horas previstas na auditoria

Toda mudança das horas previstas MUST ir para a auditoria, com a tarefa pelo nome atual e o projeto como contexto (ADR-0039, ADR-0084).

#### Scenario: Horas previstas na auditoria
- **WHEN** o administrador põe e depois troca as horas previstas de uma tarefa
- **THEN** a auditoria tem a inclusão e a alteração, por ele, com o nome da tarefa, no projeto

### Requirement: Horas na Lista do cronograma pela tela

Para o administrador, a Lista do cronograma MUST ter a coluna "Horas", com as apontadas e as previstas de cada tarefa ("12 de 16 h"), o estado por ícone, cor e texto quando a tarefa está acima ou perto das previstas, e só as apontadas quando a tarefa não tem previstas. Para as outras pessoas, a Lista MUST NOT mostrar horas (ADR-0065, ADR-0138).

#### Scenario: Horas na Lista do administrador
- **WHEN** o administrador abre a Lista de um cronograma com uma tarefa de 52 horas apontadas e 40 previstas
- **THEN** a coluna "Horas" mostra "52 de 40 h" com o ícone e a cor de acima das previstas

#### Scenario: Cronograma sem horas para a gestão
- **WHEN** a gerente do projeto abre a Lista e o detalhe de uma tarefa
- **THEN** a tela não tem a coluna "Horas" nem a seção "Horas"

### Requirement: Horas no detalhe da tarefa pela tela

Para o administrador, o detalhe de uma tarefa MUST ter a seção "Horas", com as previstas, as apontadas, o estado, a barra do consumo e "Alterar", que abre o diálogo das previstas, com o campo vazio para tirar (ADR-0138).

#### Scenario: Alterar as previstas pelo detalhe da tarefa
- **WHEN** o administrador abre o detalhe de uma tarefa, escolhe "Alterar" na seção "Horas", informa 48 e salva
- **THEN** a seção e a coluna mostram as 48 horas previstas, e a tela confirma

### Requirement: Horas por tarefa no detalhe das horas pela tela

O detalhe de um projeto em Horas dos projetos MUST ter a seção "Por tarefa", com as previstas nas tarefas, as apontadas nelas e as apontadas fora das tarefas, a lista das tarefas com previstas ou apontadas, cada uma com as previstas, as apontadas e o saldo (ícone, cor e texto) e "Alterar", que abre o diálogo das previstas, e "Abrir o cronograma", que leva à Lista do cronograma do projeto (ADR-0081, ADR-0138).

#### Scenario: Por tarefa no detalhe das horas
- **WHEN** o administrador abre o detalhe de um projeto com previstas em duas tarefas e horas apontadas sem tarefa
- **THEN** a seção "Por tarefa" mostra as duas tarefas com o saldo de cada uma e as horas apontadas fora das tarefas

#### Scenario: Alterar as previstas pelo detalhe das horas
- **WHEN** o administrador escolhe "Alterar" numa tarefa da seção "Por tarefa", informa 24 e salva
- **THEN** a tarefa mostra 24 horas previstas e o novo saldo, e a tela confirma
