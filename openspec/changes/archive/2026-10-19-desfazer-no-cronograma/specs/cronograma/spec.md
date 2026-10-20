## MODIFIED Requirements

### Requirement: Quem vê e quem edita o cronograma

Todas as pessoas do projeto e o administrador MUST ver o cronograma. Só o administrador e o gerente do projeto MUST poder editá-lo: fases, tarefas, marcos, dependências, responsáveis, o marco atingido, a linha de base e desfazer e refazer as próprias mudanças. O líder técnico, o colaborador, o desenvolvedor e o leitor MUST receber 403 ao editar; o progresso das tarefas tem regra própria. Para quem não está no projeto e não é administrador, o cronograma não existe (404); sem sessão, 401 (ADR-0043).

#### Scenario: Gerente edita o cronograma
- **WHEN** o gerente do projeto inclui uma fase, uma tarefa e um marco, altera a tarefa e exclui o marco
- **THEN** a API aceita as cinco mudanças, e o cronograma mostra a fase e a tarefa alterada

#### Scenario: Administrador fora do projeto edita o cronograma
- **WHEN** o administrador, sem estar no projeto, vê o cronograma e inclui uma tarefa
- **THEN** a API aceita as duas ações

#### Scenario: Líder técnico só vê o cronograma
- **WHEN** o líder técnico vê o cronograma e depois tenta incluir, alterar ou excluir uma fase ou uma tarefa
- **THEN** a API mostra o cronograma, responde 403 às mudanças e nada muda

#### Scenario: Colaborador, desenvolvedor e leitor só veem o cronograma
- **WHEN** um colaborador, um desenvolvedor ou um leitor do projeto vê o cronograma e tenta alterar uma tarefa, mesmo uma da qual é responsável
- **THEN** a API mostra o cronograma, responde 403 à alteração e nada muda

#### Scenario: Cronograma de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede o cronograma ou tenta alterá-lo
- **THEN** a API responde 404

#### Scenario: Cronograma sem sessão
- **WHEN** alguém sem sessão pede o cronograma de um projeto
- **THEN** a API responde 401

## ADDED Requirements

### Requirement: Desfazer no cronograma

Quem edita o cronograma MUST poder desfazer a própria última mudança ainda não desfeita, com tudo o que ela empurrou e recalculou. Se alguém mudou esses itens depois, o desfazer MUST ser recusado com 409, e a mudança sai do histórico. Cada pessoa guarda as últimas 20 mudanças por projeto (ADR-0021, ADR-0091).

#### Scenario: Desfazer volta a cadeia
- **WHEN** o gerente adia uma tarefa, que empurra o marco seguinte, e desfaz
- **THEN** a tarefa e o marco voltam às datas de antes, numa vez só

#### Scenario: Desfazer uma exclusão
- **WHEN** o gerente exclui uma tarefa com dependências e desfaz
- **THEN** a tarefa volta, com o mesmo identificador, as mesmas datas e as dependências

#### Scenario: Desfazer bloqueado
- **WHEN** o gerente adia uma tarefa, o administrador muda a mesma tarefa, e o gerente tenta desfazer
- **THEN** a API responde 409, nada muda, e o próximo desfazer do gerente vai para a mudança anterior

#### Scenario: Só as próprias mudanças
- **WHEN** o administrador inclui uma tarefa e o gerente, sem mudanças suas, tenta desfazer
- **THEN** a API responde 409 por não haver o que desfazer, e a tarefa continua lá

#### Scenario: Últimas 20 mudanças
- **WHEN** o gerente faz 21 mudanças e desfaz 21 vezes
- **THEN** as 20 últimas voltam, e o vigésimo primeiro desfazer responde que não há o que desfazer

### Requirement: Refazer no cronograma

Quem edita o cronograma MUST poder refazer a última mudança que desfez, com a mesma proteção do desfazer. Uma mudança nova da pessoa MUST apagar o que ela podia refazer (ADR-0071, ADR-0091).

#### Scenario: Refazer depois de desfazer
- **WHEN** o gerente renomeia uma fase, desfaz e refaz
- **THEN** a fase volta ao nome novo

#### Scenario: Mudança nova apaga o refazer
- **WHEN** o gerente desfaz uma mudança e depois inclui uma tarefa
- **THEN** não há o que refazer

### Requirement: Desfazer pela tela

Para quem edita, a aba Cronograma e o Expandir MUST ter o botão "Desfazer", com o que será desfeito na dica e desligado quando não há o que desfazer, e o menu "⋮" MUST ter "Refazer". A tela MUST dizer o que foi desfeito ou refeito, ou por que não deu (ADR-0071, ADR-0091).

#### Scenario: Desfazer pela tela
- **WHEN** o gerente inclui uma tarefa e escolhe "Desfazer"
- **THEN** a tela diz que a inclusão da tarefa foi desfeita e a tarefa some do cronograma

#### Scenario: Refazer pela tela
- **WHEN** o gerente escolhe "Refazer" no menu "⋮" depois de desfazer
- **THEN** a tela diz que a mudança foi refeita

#### Scenario: Desfazer bloqueado pela tela
- **WHEN** o desfazer é recusado porque alguém mudou os itens depois
- **THEN** a tela diz que não dá para desfazer porque alguém mudou esses itens depois

#### Scenario: Sem editar, sem desfazer
- **WHEN** quem não edita o cronograma abre a aba Cronograma
- **THEN** a tela não oferece desfazer nem refazer
