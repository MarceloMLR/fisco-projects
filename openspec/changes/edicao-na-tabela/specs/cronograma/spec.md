## ADDED Requirements

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
