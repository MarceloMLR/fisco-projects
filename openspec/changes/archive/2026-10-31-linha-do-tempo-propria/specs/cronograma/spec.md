## ADDED Requirements

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

A Linha do tempo MUST ser desenhada pela própria aplicação, sem componente pago nem chave de licença, completa e sem aviso de versão de avaliação em qualquer instalação (ADR-0134).

#### Scenario: Instalação sem chave de licença
- **WHEN** uma pessoa do projeto abre a Linha do tempo numa instalação sem a chave do Syncfusion
- **THEN** a Linha do tempo aparece completa, sem aviso de versão de avaliação

#### Scenario: Barra que volta na recusa
- **WHEN** o gerente arrasta uma tarefa e a API recusa a mudança
- **THEN** a barra volta para as datas de antes e a tela diz por quê
