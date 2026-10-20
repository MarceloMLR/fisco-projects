## Purpose

Modelos de projeto salvos a partir do cronograma de um projeto, usados para criar projetos com o cronograma pronto e administrados pelo administrador (ADR-0017, ADR-0043, ADR-0046).

## ADDED Requirements

### Requirement: Salvar como modelo

O administrador e o gerente do projeto MUST poder salvar o cronograma como modelo, com nome e descrição. O modelo guarda fases, tarefas, subtarefas, marcos, dependências, durações e a distância em dias úteis de cada item ao início do cronograma, sem datas nem responsáveis. Os demais recebem 403; cronograma sem itens, 400; nome repetido, 409 (ADR-0017, ADR-0043).

#### Scenario: Salvar o cronograma como modelo
- **WHEN** o gerente salva como modelo um cronograma com uma fase, uma tarefa de 3 dias úteis e um marco depois dela
- **THEN** o modelo guarda a fase, a tarefa com a duração 3 e a distância 0, o marco ligado a ela, sem datas nem responsável

#### Scenario: Modelo de um projeto encerrado
- **WHEN** o gerente salva como modelo o cronograma de um projeto concluído
- **THEN** o modelo é salvo

#### Scenario: Nome de modelo repetido
- **WHEN** o gerente salva um modelo com o nome de outro modelo
- **THEN** a API responde 409 e nada é salvo

### Requirement: Projeto a partir de um modelo

Ao criar um projeto, quem cria MUST poder escolher um modelo ativo e a data de início, e o servidor MUST gerar o cronograma em dias úteis do calendário da instalação, mantendo as distâncias e as dependências do modelo. Modelo inativo ou sem início MUST receber 400; "Em branco" cria o projeto sem cronograma (ADR-0017, ADR-0018).

#### Scenario: Criar projeto a partir de um modelo
- **WHEN** a gerente cria um projeto a partir de um modelo com o início em 09/10/2026, uma sexta-feira, antes do feriado de 12/10/2026
- **THEN** o cronograma tem as fases e os itens do modelo, a primeira tarefa de 3 dias úteis vai de 09/10/2026 a 14/10/2026 e o marco cai no fim dela

#### Scenario: Modelo inativo na criação
- **WHEN** alguém tenta criar um projeto a partir de um modelo inativo
- **THEN** a API responde 400 e o projeto não é criado

#### Scenario: Projeto em branco
- **WHEN** a gerente cria um projeto em branco
- **THEN** o projeto nasce sem cronograma

### Requirement: Modelos na Administração

Só o administrador MUST ver todos os modelos na Administração e poder renomear, descrever, inativar, reativar e excluir cada um; quem cria projetos MUST ver só os ativos, no "Novo projeto" (ADR-0017, ADR-0046).

#### Scenario: Inativar um modelo
- **WHEN** o administrador inativa um modelo
- **THEN** o modelo continua na Administração como inativo e some do "Novo projeto"

#### Scenario: Excluir um modelo
- **WHEN** o administrador exclui um modelo e confirma
- **THEN** o modelo some da Administração e do "Novo projeto", e os projetos criados dele continuam

#### Scenario: Gerente fora da Administração dos modelos
- **WHEN** um gerente de projeto pede a lista da Administração ou tenta alterar um modelo
- **THEN** a API responde 403

### Requirement: Modelos pela tela

O menu "⋮" da aba Cronograma MUST ter "Salvar como modelo" para quem edita. O "Novo projeto" MUST oferecer "Em branco" ou "A partir de um modelo", com o modelo, o início e a prévia das fases e itens. A Administração MUST ter "Modelos de projeto" com a lista e o menu de cada linha (ADR-0017, ADR-0071).

#### Scenario: Salvar como modelo pela tela
- **WHEN** a gerente escolhe "Salvar como modelo" no menu "⋮", digita o nome e salva
- **THEN** a tela avisa que o modelo foi salvo

#### Scenario: Novo projeto a partir de um modelo pela tela
- **WHEN** a gerente escolhe "A partir de um modelo", o modelo e o início, e cria o projeto
- **THEN** a prévia mostra as fases do modelo, e a API recebe o modelo e o início

#### Scenario: Modelos na Administração pela tela
- **WHEN** o administrador abre "Modelos de projeto", inativa um modelo pelo menu da linha e depois o exclui
- **THEN** a lista mostra o modelo inativo e depois sem ele

### Requirement: Modelos na auditoria

Salvar, alterar e excluir um modelo MUST entrar na auditoria pelo nome do modelo, com o nome, a descrição e a situação antes e depois. As fases e os itens do modelo MUST NOT entrar um a um. A tela de Auditoria MUST ter o tipo "Modelos de projeto" (ADR-0039).

#### Scenario: Modelo na auditoria
- **WHEN** a gerente salva um modelo e o administrador o inativa
- **THEN** a auditoria tem "Salvou um modelo" e "Alterou um modelo", pelo nome, e nenhum registro por item
