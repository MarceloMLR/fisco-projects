## ADDED Requirements

### Requirement: Visualização Quadro

A aba Cronograma MUST ter a visualização "Quadro" (`?view=board`), para todos que veem o cronograma, com três colunas pelo progresso e a quantidade de cada uma: A fazer (0%), Em andamento (de 1% a 99%) e Concluída (100%). Cada tarefa com datas próprias (sem subtarefas, ou subtarefa) MUST ser um cartão na coluna do progresso dela; a tarefa-resumo e os marcos MUST NOT aparecer (ADR-0155).

#### Scenario: Tarefas nas colunas do quadro
- **WHEN** a gerente abre o Quadro do ERP-FIN
- **THEN** cada tarefa com datas próprias aparece na coluna do progresso dela, com a quantidade em cada coluna, e a tarefa-resumo e os marcos não aparecem

### Requirement: Cartão do quadro

O cartão MUST mostrar o nome da tarefa, a fase (e a tarefa-mãe, na subtarefa), o responsável (a pessoa, o cliente ou "Sem responsável"), o fim ou, atrasada, os dias úteis de atraso, a barra do progresso em Em andamento e o dia da conclusão em Concluída. Escolher o cartão MUST abrir o detalhe da tarefa, como a linha da Lista (ADR-0155).

#### Scenario: Cartão da tarefa
- **WHEN** a gerente olha o cartão de uma subtarefa atrasada em andamento
- **THEN** o cartão mostra o nome, a fase e a tarefa-mãe, o responsável, os dias úteis de atraso e a barra do progresso

#### Scenario: Detalhe pelo cartão
- **WHEN** a gerente escolhe o cartão de uma tarefa
- **THEN** o detalhe da tarefa abre, como pela linha da Lista

### Requirement: Ordem dos cartões

Em A fazer e Em andamento, os cartões MUST seguir o fim, do mais cedo ao mais tarde; em Concluída, o dia da conclusão, do mais recente ao mais antigo; no empate, a ordem do cronograma (ADR-0155).

#### Scenario: Ordem dos cartões
- **WHEN** a coluna Em andamento tem tarefas com fins diferentes e a Concluída, tarefas concluídas em dias diferentes
- **THEN** Em andamento vai do fim mais cedo ao mais tarde, e Concluída, da conclusão mais recente à mais antiga

### Requirement: Quadro com o filtro e no celular

O filtro do cronograma MUST valer também no quadro, que MUST dizer quando nenhuma tarefa passa no filtro. No celular, as colunas MUST ficar lado a lado, com a rolagem lateral, e a página MUST NOT rolar como um todo (ADR-0066, ADR-0155).

#### Scenario: Quadro com filtro
- **WHEN** a gerente filtra "Minhas tarefas" e abre o Quadro
- **THEN** só os cartões das tarefas dela aparecem, e o quadro diz quando nenhuma tarefa passa no filtro

#### Scenario: Quadro no celular
- **WHEN** a Carla abre o Quadro no celular
- **THEN** as colunas ficam lado a lado, com a rolagem lateral, e os cartões dela têm "Mover"

### Requirement: Mover o cartão no quadro

O cartão MUST se mover arrastando-o, no computador, ou pela ação "Mover" do cartão, também pelo teclado e no celular. Para Concluída, a tarefa MUST ir a 100% na hora; para A fazer, MUST voltar a 0% na hora; para Em andamento, a tela MUST pedir o percentual, como "Iniciar" e "Atualizar progresso", sem gravar nada sem o número (ADR-0086). O cartão MUST ficar na coluna nova até o pedido voltar, e a recusa MUST devolvê-lo, com o aviso (ADR-0155).

#### Scenario: Arrastar para Concluída
- **WHEN** a gerente arrasta o cartão de uma tarefa em andamento para Concluída
- **THEN** a tela pede o progresso 100, avisa que a tarefa foi concluída, e o cartão fica em Concluída

#### Scenario: Arrastar para Em andamento pede o percentual
- **WHEN** a gerente arrasta o cartão de uma tarefa a 0% para Em andamento
- **THEN** a tela pergunta o percentual, como "Iniciar tarefa", e pede o progresso digitado só depois de salvar

#### Scenario: Voltar para A fazer
- **WHEN** a gerente arrasta o cartão de uma tarefa em andamento para A fazer
- **THEN** a tela pede o progresso 0 e avisa que a tarefa voltou para A fazer

#### Scenario: Mover pelo menu
- **WHEN** a Carla escolhe "Mover" no cartão de uma tarefa dela e "Concluída"
- **THEN** a tela pede o progresso 100 da tarefa, e o menu só oferece as outras colunas

#### Scenario: Recusa ao mover
- **WHEN** a API recusa o progresso de um cartão arrastado
- **THEN** o cartão volta à coluna de antes, e a tela avisa a falha

### Requirement: Quem move o cartão

Só quem atualiza o progresso da tarefa MUST mover o cartão (ADR-0043): o administrador e o gerente, qualquer tarefa; o líder técnico, o colaborador e o desenvolvedor, só as tarefas de que são responsáveis; o leitor, nenhuma. Os outros cartões MUST NOT se arrastar nem ter "Mover", e o projeto encerrado MUST NOT deixar mover nenhum; o detalhe MUST abrir para todos (ADR-0155).

#### Scenario: Cartão de quem não move
- **WHEN** o leitor, ou a Carla no cartão de uma tarefa de outra pessoa, abre o Quadro
- **THEN** o cartão não se arrasta e não tem "Mover", e o detalhe abre normalmente
