## ADDED Requirements

### Requirement: Importar do MS Project

Quem cria projetos MUST poder ler um arquivo XML do MS Project numa prévia, sem guardar nada, e criar um projeto com o cronograma convertido: as fases, as tarefas, as subtarefas, os marcos e as dependências "termina, então começa", com as datas em dias úteis do calendário da instalação a partir do início escolhido. A conversão MUST seguir o ADR-0135 e dizer cada ajuste num aviso. Responsáveis, progresso e linha de base MUST NOT vir do arquivo (ADR-0017, ADR-0019, ADR-0088, ADR-0135).

#### Scenario: Prévia do arquivo do MS Project
- **WHEN** a gerente manda na prévia o XML de um cronograma com uma fase, uma tarefa de 13/10/2026 a 15/10/2026 e um marco ligado a ela
- **THEN** a resposta traz a fase, a tarefa de 3 dias úteis, o marco ligado a ela e o primeiro dia 13/10/2026, sem avisos, e nada é guardado

#### Scenario: Projeto a partir do MS Project
- **WHEN** a gerente cria um projeto com o cronograma da prévia e o início em 09/10/2026, uma sexta-feira, antes do feriado de 12/10/2026
- **THEN** o projeto nasce com a fase, a tarefa de 09/10/2026 a 14/10/2026 e o marco no fim dela, sem responsáveis nem progresso

#### Scenario: O que não cabe no FiscoProjects
- **WHEN** o arquivo tem uma tarefa no nível 4, uma tarefa solta no nível 1, uma dependência "começa, então começa", outra com intervalo e outra de uma tarefa-resumo
- **THEN** a prévia traz a tarefa do nível 4 como subtarefa, a solta numa fase com o título do projeto, a dependência com intervalo sem o intervalo, as outras duas fora, e um aviso de cada ajuste com os nomes

#### Scenario: Arquivo que não é do MS Project
- **WHEN** a gerente manda na prévia um .mpp, um XML de outro programa, um XML com DTD ou um arquivo acima de 5 MB
- **THEN** a API responde 400 com o motivo e a orientação de salvar como XML no MS Project, sem ler entidades externas

#### Scenario: Importar sem poder criar projetos
- **WHEN** uma pessoa com o papel Usuário manda um arquivo na prévia
- **THEN** a API responde 403

#### Scenario: Ida e volta pelo MS Project
- **WHEN** a gerente exporta o XML de um cronograma e o manda na prévia
- **THEN** a prévia traz as mesmas fases, tarefas, subtarefas, marcos, durações e dependências do cronograma exportado

### Requirement: Importar do MS Project pela tela

O "Novo projeto" MUST oferecer "A partir do MS Project", além de "Em branco" e "A partir de um modelo", com o arquivo, o início preenchido com o primeiro dia do arquivo e a prévia das fases e dos itens com as datas e os avisos da conversão. O arquivo recusado MUST dizer o motivo no campo (ADR-0071, ADR-0135).

#### Scenario: Novo projeto a partir do MS Project
- **WHEN** a gerente escolhe "A partir do MS Project", escolhe o arquivo, confere a prévia e cria o projeto
- **THEN** a prévia mostra as fases, os itens e os avisos, a API recebe o cronograma convertido e o início, e a tela abre o projeto

#### Scenario: Arquivo recusado pela tela
- **WHEN** a gerente escolhe um .mpp em "A partir do MS Project"
- **THEN** o campo do arquivo diz que o arquivo precisa ser salvo como XML no MS Project, e o projeto não é criado
