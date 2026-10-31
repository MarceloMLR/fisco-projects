## MODIFIED Requirements

### Requirement: Importar do MS Project

Quem cria projetos MUST poder ler um XML do MS Project numa prévia, sem guardar nada, e criar um projeto com o cronograma convertido: fases, tarefas, subtarefas, marcos e dependências "termina, então começa", com as datas em dias úteis da instalação a partir do início escolhido. A conversão MUST seguir o ADR-0135, dizer num aviso cada ajuste e o que ficou de fora e contar as tarefas e os recursos do arquivo que vieram; a linha de base MUST NOT vir (ADR-0017, ADR-0019, ADR-0088, ADR-0135).

#### Scenario: Prévia do arquivo do MS Project
- **WHEN** a gerente manda na prévia o XML de um cronograma com uma fase, uma tarefa de 13/10/2026 a 15/10/2026 a 40% com o recurso Carla e um marco ligado a ela
- **THEN** a resposta traz a fase, a tarefa de 3 dias úteis com 40%, o marco ligado a ela, o primeiro dia 13/10/2026, o recurso Carla com uma tarefa e as 3 de 3 tarefas e o 1 de 1 recurso do arquivo, sem avisos, e nada é guardado

#### Scenario: Projeto a partir do MS Project
- **WHEN** a gerente cria um projeto com o cronograma da prévia, o início em 09/10/2026 (uma sexta-feira, antes do feriado de 12/10/2026) e o recurso Carla ligado à Carla como Colaboradora
- **THEN** o projeto nasce com a fase, a tarefa de 09/10/2026 a 14/10/2026 a 40% com a Carla responsável e o marco no fim dela, e a Carla entra no projeto como Colaboradora

#### Scenario: O que não cabe no FiscoProjects
- **WHEN** o arquivo tem uma tarefa no nível 4, uma tarefa solta no nível 1, uma dependência "começa, então começa", outra com intervalo e outra de uma tarefa-resumo
- **THEN** a prévia traz a tarefa do nível 4 como subtarefa, a solta numa fase com o título do projeto, a dependência com intervalo sem o intervalo, as outras duas fora, e um aviso de cada ajuste com os nomes

#### Scenario: O que fica de fora do arquivo
- **WHEN** o arquivo tem uma tarefa inativa com uma subtarefa, duas linhas em branco, um recurso de material, um de custo e um recurso de trabalho só na subtarefa inativa
- **THEN** a prévia não traz a tarefa inativa, a subtarefa nem esses recursos, avisa a tarefa inativa e a subtarefa pelos nomes, as linhas em branco pela quantidade, os recursos de material e custo e o recurso sem tarefa pelos nomes, e conta as tarefas e os recursos do arquivo e os que vieram

#### Scenario: Arquivo que não é do MS Project
- **WHEN** a gerente manda na prévia um .mpp, um XML de outro programa, um XML com DTD ou um arquivo acima de 5 MB
- **THEN** a API responde 400 com o motivo e a orientação de salvar como XML no MS Project, sem ler entidades externas

#### Scenario: Importar sem poder criar projetos
- **WHEN** uma pessoa com o papel Usuário manda um arquivo na prévia
- **THEN** a API responde 403

#### Scenario: Ida e volta pelo MS Project
- **WHEN** a gerente exporta o XML de um cronograma e o manda na prévia
- **THEN** a prévia traz as mesmas fases, tarefas, subtarefas, marcos, durações, progresso e dependências do cronograma exportado, e um recurso para cada responsável, com a pessoa dele sugerida pelo e-mail

### Requirement: Importar do MS Project pela tela

O "Novo projeto" MUST oferecer "A partir do MS Project", além de "Em branco" e "A partir de um modelo", com o arquivo, o início preenchido com o primeiro dia do arquivo, a prévia das fases e dos itens com as datas, o quadro da conversão e os responsáveis do arquivo, cada um com a pessoa e o papel. O quadro MUST aparecer a cada leitura, com o que veio, os avisos e as tarefas que ficam sem responsável. O arquivo recusado MUST dizer o motivo no campo (ADR-0071, ADR-0135).

#### Scenario: Novo projeto a partir do MS Project
- **WHEN** a gerente escolhe "A partir do MS Project", escolhe o arquivo, confere a prévia, liga um recurso a uma pessoa como Desenvolvedora e cria o projeto
- **THEN** a prévia mostra as fases, os itens, os avisos e os responsáveis, a API recebe o cronograma convertido, a pessoa com o papel e o início, e a tela abre o projeto

#### Scenario: Quadro da conversão na tela
- **WHEN** a gerente escolhe um arquivo com dois avisos da conversão
- **THEN** a tela rola até o quadro, que diz quantas tarefas e quantos recursos do arquivo vieram e os dois avisos, e a prévia mostra "2 ajustes na conversão", que leva ao quadro

#### Scenario: Arquivo sem ajustes na tela
- **WHEN** a gerente escolhe um arquivo que veio inteiro, sem avisos, com a pessoa de cada recurso achada
- **THEN** o quadro diz quantas tarefas e quantos recursos vieram e que nada mudou na conversão, e a prévia não mostra o atalho

#### Scenario: Tarefas sem responsável na tela
- **WHEN** o arquivo tem um recurso sem pessoa achada, com uma tarefa, e a gerente o liga depois a uma pessoa
- **THEN** o quadro e a linha do recurso dizem que a tarefa fica sem responsável, e os dois avisos saem quando a pessoa é escolhida

#### Scenario: Arquivo recusado pela tela
- **WHEN** a gerente escolhe um .mpp em "A partir do MS Project"
- **THEN** o campo do arquivo diz que o arquivo precisa ser salvo como XML no MS Project, e o projeto não é criado
