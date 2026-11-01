## ADDED Requirements

### Requirement: Recolher fases na Lista

Na Lista, cada fase MUST ter uma seta ao lado do nome que recolhe e mostra as tarefas e os marcos dela, pelo clique ou pelo teclado, sem abrir o diálogo da fase; recolhida, a fase continua com as datas e o progresso. Tudo começa à mostra (ADR-0071).

#### Scenario: Recolher uma fase na lista
- **WHEN** uma pessoa escolhe a seta de uma fase na Lista e depois a escolhe de novo
- **THEN** as tarefas e os marcos da fase somem da Lista sem abrir o diálogo da fase, a fase continua com as datas e o progresso, e as tarefas voltam na segunda vez

### Requirement: Recolhido igual nas duas visões

O que está recolhido, fases e tarefas-resumo, MUST ser o mesmo na Lista, na Linha do tempo e no Expandir enquanto a aba está aberta; a exportação em PDF e em Excel MUST trazer o cronograma inteiro (ADR-0071).

#### Scenario: Fase recolhida ao trocar de visão
- **WHEN** uma pessoa recolhe uma fase na Linha do tempo e troca para a Lista
- **THEN** a fase está recolhida na Lista, e, mostrada de novo na Lista, aparece aberta na Linha do tempo
