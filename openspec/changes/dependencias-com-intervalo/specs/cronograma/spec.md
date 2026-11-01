## MODIFIED Requirements

### Requirement: Dependências

Uma tarefa ou um marco pode depender de outras tarefas e marcos do mesmo cronograma, com um tipo e um intervalo em dias úteis de -99 a 999: em "termina, então começa", uma tarefa MUST começar no dia útil seguinte ao fim da anterior e um marco no dia do fim; em "começa junto", no início da anterior; em "termina junto", terminar no fim da anterior; sempre mais o intervalo. Um início mais cedo do que as anteriores permitem MUST passar para o primeiro dia possível. Depender de si mesmo ou formar um ciclo MUST receber 400 (ADR-0019, ADR-0137).

#### Scenario: Tarefa depois da anterior
- **WHEN** o gerente faz uma tarefa que começa em 08/10/2026 depender de outra que termina na sexta-feira 09/10/2026, e 12/10/2026 é feriado
- **THEN** a tarefa passa a começar na terça-feira 13/10/2026, e um marco que dependa da mesma tarefa fica em 09/10/2026

#### Scenario: Intervalo entre as tarefas
- **WHEN** a homologação do cliente depende da entrega, que termina na sexta-feira 09/10/2026, com o intervalo de 2 dias úteis, e 12/10/2026 é feriado
- **THEN** a homologação começa na quinta-feira 15/10/2026, e com o intervalo de -1 começa na sexta-feira 09/10/2026

#### Scenario: Começa junto
- **WHEN** os testes, de 3 dias úteis, dependem da parametrização, que começa em 14/10/2026, do tipo "começa junto" com o intervalo de 1 dia útil
- **THEN** os testes vão de 15/10/2026 a 19/10/2026, mesmo que a parametrização termine depois

#### Scenario: Termina junto
- **WHEN** o treinamento, de 2 dias úteis, depende da migração, que termina em 20/10/2026, do tipo "termina junto"
- **THEN** o treinamento vai de 19/10/2026 a 20/10/2026

#### Scenario: Tipo e intervalo inválidos
- **WHEN** o gerente manda uma dependência com o intervalo de 1.000 dias úteis ou um tipo que não existe
- **THEN** a API responde 400 em `links` e nada muda

#### Scenario: Dependência circular
- **WHEN** a tarefa B depende de A, a tarefa C depende de B, e o gerente tenta fazer A depender de C
- **THEN** a API responde 400 com o tipo de problema de ciclo e as dependências continuam como estavam

### Requirement: Dependências que empurram

Mudar o início ou a duração de uma tarefa para o fim ficar depois MUST empurrar as tarefas e os marcos seguintes, em cadeia, só o necessário para cada um respeitar o tipo e o intervalo das dependências, pulando fins de semana e feriados. Antecipar uma tarefa MUST NOT mover as seguintes. A resposta MUST dizer quantos itens foram empurrados (ADR-0019, ADR-0137).

#### Scenario: Atrasar empurra a cadeia
- **WHEN** A vai de 05/10 a 09/10/2026, B depende de A e vai de 13/10 a 14/10, C depende de B e fica em 15/10, e o gerente aumenta a duração de A em um dia
- **THEN** A termina em 13/10, B vai de 14/10 a 15/10, C fica em 16/10/2026, e a resposta diz que 2 itens foram empurrados

#### Scenario: Empurrar pelo início
- **WHEN** B começa junto com A e o gerente atrasa o início de A em dois dias úteis, sem mudar o fim de A além disso
- **THEN** B começa dois dias úteis depois, com a mesma duração

#### Scenario: Antecipar não puxa
- **WHEN** o gerente antecipa em dois dias úteis o início de uma tarefa que tem uma tarefa seguinte
- **THEN** a tarefa seguinte continua com as mesmas datas, e a resposta diz que nenhum item foi empurrado

### Requirement: Importar do MS Project

Quem cria projetos MUST poder ler um XML do MS Project numa prévia, sem guardar nada, e criar um projeto com o cronograma convertido: fases, tarefas, subtarefas, marcos e dependências com o tipo e o intervalo, com as datas em dias úteis da instalação a partir do início escolhido. A conversão MUST seguir o ADR-0135, dizer num aviso cada ajuste e o que ficou de fora e contar as tarefas e os recursos do arquivo que vieram; a linha de base MUST NOT vir (ADR-0017, ADR-0019, ADR-0088, ADR-0135, ADR-0137).

#### Scenario: Prévia do arquivo do MS Project
- **WHEN** a gerente manda na prévia o XML de um cronograma com uma fase, uma tarefa de 13/10/2026 a 15/10/2026 a 40% com o recurso Carla e um marco ligado a ela
- **THEN** a resposta traz a fase, a tarefa de 3 dias úteis com 40%, o marco ligado a ela, o primeiro dia 13/10/2026, o recurso Carla com uma tarefa e as 3 de 3 tarefas e o 1 de 1 recurso do arquivo, sem avisos, e nada é guardado

#### Scenario: Projeto a partir do MS Project
- **WHEN** a gerente cria um projeto com o cronograma da prévia, o início em 09/10/2026 (uma sexta-feira, antes do feriado de 12/10/2026) e o recurso Carla ligado à Carla como Colaboradora
- **THEN** o projeto nasce com a fase, a tarefa de 09/10/2026 a 14/10/2026 a 40% com a Carla responsável e o marco no fim dela, e a Carla entra no projeto como Colaboradora

#### Scenario: O que não cabe no FiscoProjects
- **WHEN** o arquivo tem uma tarefa no nível 4, uma tarefa solta no nível 1, uma dependência "começa, então termina", outra com o intervalo em porcentagem e outra de uma tarefa-resumo
- **THEN** a prévia traz a tarefa do nível 4 como subtarefa, a solta numa fase com o título do projeto, a "começa, então termina" como "termina, então começa", o intervalo em dias úteis, a da tarefa-resumo fora, e um aviso de cada ajuste com os nomes

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
- **THEN** a prévia traz as mesmas fases, tarefas, subtarefas, marcos, durações, progresso e dependências, com os tipos e os intervalos, do cronograma exportado, e um recurso para cada responsável, com a pessoa dele sugerida pelo e-mail

## ADDED Requirements

### Requirement: Tipo e intervalo das dependências pela tela

No detalhe da tarefa e do marco, quem edita MUST escolher o tipo ("Termina, então começa", "Começa junto", "Termina junto") e o intervalo de cada item de "Depende de". A Lista MUST dizer o tipo e o intervalo de cada anterior que não for "termina, então começa" sem intervalo, e a Linha do tempo MUST desenhar a seta de "começa junto" de início a início e a de "termina junto" de fim a fim; o Excel MUST dizê-los como o MS Project em português ("5II+2") (ADR-0137).

#### Scenario: Tipo e intervalo no detalhe
- **WHEN** a gerente escolhe, em Testes integrados, "Começa junto" e o intervalo 1 para Parametrizar regras de ICMS e salva
- **THEN** a API recebe a dependência com o tipo e o intervalo, e a coluna "Depende de" diz "Parametrizar regras de ICMS (começa junto, +1)"

#### Scenario: Setas dos tipos na Linha do tempo
- **WHEN** a Linha do tempo mostra uma dependência "começa junto" e outra "termina junto"
- **THEN** a primeira sai do início da anterior para o início da seguinte, e a segunda do fim da anterior para o fim da seguinte

#### Scenario: Dependências no Excel
- **WHEN** uma pessoa do projeto exporta para o Excel um item que depende da linha 3 sem intervalo, da linha 5 "começa junto" com +2 e da linha 7 "termina junto" com -1
- **THEN** a coluna "Depende de (Nº)" diz "3; 5II+2; 7TT-1"

### Requirement: Tipo e intervalo no MS Project

O XML do MS Project MUST levar o tipo ("termina, então começa", "começa junto", "termina junto") e o intervalo em dias de cada dependência, e a importação MUST trazê-los. "Começa, então termina" MUST virar "termina, então começa", e um intervalo em horas, dias corridos ou porcentagem MUST virar dias úteis arredondados, cada um com o aviso e os nomes (ADR-0135, ADR-0137).

#### Scenario: Tipos no MS Project
- **WHEN** a gerente exporta um cronograma com uma dependência "começa junto" com +2 e outra "termina junto" e cria um projeto com esse arquivo
- **THEN** o XML traz `Type` 3 com `LinkLag` 9600 e `Type` 0, e o projeto novo tem as mesmas dependências e datas
