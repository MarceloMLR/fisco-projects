## ADDED Requirements

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
