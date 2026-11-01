## MODIFIED Requirements

### Requirement: Salvar como modelo

O administrador e o gerente do projeto MUST poder salvar o cronograma como modelo, com nome e descrição. O modelo guarda fases, tarefas, subtarefas, marcos, dependências com o tipo e o intervalo, durações e a distância em dias úteis de cada item ao início do cronograma, sem datas nem responsáveis. Os demais recebem 403; cronograma sem itens, 400; nome repetido, 409 (ADR-0017, ADR-0043, ADR-0137).

#### Scenario: Salvar o cronograma como modelo
- **WHEN** o gerente salva como modelo um cronograma com uma fase, uma tarefa de 3 dias úteis e um marco depois dela
- **THEN** o modelo guarda a fase, a tarefa com a duração 3 e a distância 0, o marco ligado a ela, sem datas nem responsável

#### Scenario: Modelo com o tipo e o intervalo
- **WHEN** o gerente salva como modelo um cronograma em que os testes começam junto com a parametrização, 1 dia útil depois, e cria um projeto com ele
- **THEN** o projeto novo tem a mesma dependência "começa junto" com o intervalo de 1 dia útil

#### Scenario: Modelo de um projeto encerrado
- **WHEN** o gerente salva como modelo o cronograma de um projeto concluído
- **THEN** o modelo é salvo

#### Scenario: Nome de modelo repetido
- **WHEN** o gerente salva um modelo com o nome de outro modelo
- **THEN** a API responde 409 e nada é salvo
