## MODIFIED Requirements

### Requirement: Tela de Auditoria

A seção Auditoria da Administração MUST listar os registros do mais novo para o mais antigo, cada um com a data e a hora, o autor (ou Sistema), o que foi feito e o item, com o total de registros no período e paginação. A lista MUST ter filtros por pessoa, por tipo de item e por período, que começa nos últimos 30 dias. Ao escolher um registro, a tela MUST mostrar na gaveta de detalhes (requisito "Detalhes em gaveta sobreposta" de `estrutura-das-telas`) a data e a hora, quem fez o quê, o item e a tabela dos campos alterados, com o valor antes e depois, sem quebrar palavras no meio (ADR-0039, ADR-0081). Antes de o administrador escolher um registro, nenhum detalhe MUST aparecer.

#### Scenario: Lista da auditoria
- **WHEN** o administrador abre a seção Auditoria
- **THEN** ele vê os registros dos últimos 30 dias, do mais novo para o mais antigo, com data e hora, autor, o que foi feito e o item, e o total no período, sem nenhum detalhe aberto

#### Scenario: Troca de papel na lista
- **WHEN** a lista tem a troca de papel de uma pessoa
- **THEN** o registro mostra "Trocou um papel", o nome da pessoa e o papel antes e depois

#### Scenario: Filtro por pessoa
- **WHEN** o administrador filtra por uma pessoa
- **THEN** a lista mostra só os registros feitos por ela, e o filtro mostra o nome da pessoa inteiro ou, se não couber, terminado em reticências

#### Scenario: Filtro por tipo de item
- **WHEN** o administrador filtra por um tipo de item, como as entradas
- **THEN** a lista mostra só os registros desse tipo

#### Scenario: Filtro por período
- **WHEN** o administrador escolhe um período
- **THEN** a lista mostra só os registros entre o primeiro e o último dia escolhidos, inclusive

#### Scenario: Detalhes de um registro
- **WHEN** o administrador escolhe um registro
- **THEN** a gaveta de detalhes mostra quem fez o quê, quando, o item e a tabela dos campos com o valor antes e depois, e a linha do registro fica marcada enquanto a gaveta está aberta
