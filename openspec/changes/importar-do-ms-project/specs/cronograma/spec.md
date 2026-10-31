## ADDED Requirements

### Requirement: Importar do MS Project

Quem cria projetos MUST poder ler um arquivo XML do MS Project numa prévia, sem guardar nada, e criar um projeto com o cronograma convertido: as fases, as tarefas, as subtarefas, os marcos e as dependências "termina, então começa", com as datas em dias úteis do calendário da instalação a partir do início escolhido. A conversão MUST seguir o ADR-0135 e dizer cada ajuste num aviso; a linha de base MUST NOT vir do arquivo (ADR-0017, ADR-0019, ADR-0088, ADR-0135).

#### Scenario: Prévia do arquivo do MS Project
- **WHEN** a gerente manda na prévia o XML de um cronograma com uma fase, uma tarefa de 13/10/2026 a 15/10/2026 a 40% com o recurso Carla e um marco ligado a ela
- **THEN** a resposta traz a fase, a tarefa de 3 dias úteis com 40%, o marco ligado a ela, o primeiro dia 13/10/2026 e o recurso Carla com uma tarefa, sem avisos, e nada é guardado

#### Scenario: Projeto a partir do MS Project
- **WHEN** a gerente cria um projeto com o cronograma da prévia, o início em 09/10/2026 (uma sexta-feira, antes do feriado de 12/10/2026) e o recurso Carla ligado à Carla como Colaboradora
- **THEN** o projeto nasce com a fase, a tarefa de 09/10/2026 a 14/10/2026 a 40% com a Carla responsável e o marco no fim dela, e a Carla entra no projeto como Colaboradora

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
- **THEN** a prévia traz as mesmas fases, tarefas, subtarefas, marcos, durações, progresso e dependências do cronograma exportado, e um recurso para cada responsável, com a pessoa dele sugerida pelo e-mail

### Requirement: Progresso e responsáveis do MS Project

O cronograma importado MUST trazer o progresso de cada tarefa e marco do arquivo. Cada recurso do arquivo MUST poder ser ligado na criação a uma pessoa, sugerida pelo e-mail no diretório, que entra no projeto com o papel escolhido e fica responsável pelas tarefas dele, ao cliente do projeto ou a ninguém, seguindo o ADR-0135 (ADR-0005, ADR-0042, ADR-0102, ADR-0135).

#### Scenario: Responsáveis do arquivo
- **WHEN** o arquivo tem um recurso com o e-mail de uma pessoa do diretório, outro sem e-mail, outro com o nome do cliente escolhido no formulário e uma tarefa com dois recursos
- **THEN** a prévia sugere a pessoa para o primeiro, ninguém para o segundo, o cliente para o terceiro, e avisa que a tarefa ficou com o primeiro recurso

#### Scenario: Progresso do arquivo
- **WHEN** o arquivo tem uma tarefa a 100% que termina antes de hoje e um marco a 100% depois de hoje
- **THEN** o projeto criado tem a tarefa concluída no fim dela e o marco atingido hoje

### Requirement: Importar do MS Project pela tela

O "Novo projeto" MUST oferecer "A partir do MS Project", além de "Em branco" e "A partir de um modelo", com o arquivo, o início preenchido com o primeiro dia do arquivo, a prévia das fases e dos itens com as datas e os avisos da conversão, e os responsáveis do arquivo, cada um com a pessoa e o papel. O arquivo recusado MUST dizer o motivo no campo (ADR-0071, ADR-0135).

#### Scenario: Novo projeto a partir do MS Project
- **WHEN** a gerente escolhe "A partir do MS Project", escolhe o arquivo, confere a prévia, liga um recurso a uma pessoa como Desenvolvedora e cria o projeto
- **THEN** a prévia mostra as fases, os itens, os avisos e os responsáveis, a API recebe o cronograma convertido, a pessoa com o papel e o início, e a tela abre o projeto

#### Scenario: Arquivo recusado pela tela
- **WHEN** a gerente escolhe um .mpp em "A partir do MS Project"
- **THEN** o campo do arquivo diz que o arquivo precisa ser salvo como XML no MS Project, e o projeto não é criado

## MODIFIED Requirements

### Requirement: Exportar para o MS Project

O administrador e o gerente do projeto MUST poder baixar o cronograma no formato XML do MS Project, também com o projeto encerrado. O arquivo leva as fases, as tarefas, as subtarefas e os marcos em níveis, com as datas, a duração em dias úteis, o progresso, o responsável como recurso com o nome e o e-mail (sem o e-mail da pessoa anonimizada), as dependências "termina, então começa", a linha de base e o calendário da instalação. As outras pessoas do projeto recebem 403, e quem não está no projeto, 404 (ADR-0021, ADR-0043, ADR-0051, ADR-0136).

#### Scenario: XML do cronograma
- **WHEN** a gerente baixa o XML de um cronograma com uma fase, uma tarefa de 3 dias úteis do Rafael e um marco depois dela
- **THEN** o arquivo tem a fase no nível 1, a tarefa no nível 2, de 13/10/2026 às 08:00 a 15/10/2026 às 17:00, com 24 horas de duração, o marco com duração zero, ligado à tarefa, e o Rafael como recurso da tarefa, com o nome e o e-mail

#### Scenario: Calendário no XML
- **WHEN** o cronograma passa pelo feriado de 12/10/2026, uma segunda-feira
- **THEN** o calendário do arquivo tem segunda a sexta como dias de trabalho e o dia 12/10/2026 como exceção sem trabalho

#### Scenario: Linha de base no XML
- **WHEN** o cronograma tem a linha de base salva
- **THEN** cada tarefa e marco dela leva no arquivo o início e o fim da linha de base

#### Scenario: XML só da gestão
- **WHEN** o líder técnico, uma colaboradora e um leitor do projeto pedem o XML do cronograma
- **THEN** a API responde 403 a cada um, e o gerente de um projeto concluído baixa o arquivo

### Requirement: Exportar em PDF pela impressão

Todas as pessoas do projeto MUST poder abrir a página de impressão do cronograma, com o período e a escala que escolherem. A página mostra o cabeçalho do projeto, a tabela com a linha do tempo de cada linha e a legenda, em A4 paisagem, e o botão "Imprimir" abre a impressão do navegador, que salva em PDF. "Exportar para o MS Project (XML)" MUST aparecer no menu "⋮" só para o administrador e o gerente do projeto (ADR-0021, ADR-0071, ADR-0136).

#### Scenario: Escolher o período e a escala
- **WHEN** uma pessoa do projeto escolhe "Exportar em PDF" no menu "⋮", troca o período e a escala e confirma
- **THEN** a página de impressão abre numa aba nova com esse período e essa escala

#### Scenario: Página de impressão
- **WHEN** a página de impressão abre com o período de 05/10/2026 a 31/10/2026, em dias
- **THEN** ela mostra o código e o nome do projeto, o período, uma linha por fase e item com início, fim e progresso, a barra de cada tarefa dentro do período, e a legenda

#### Scenario: Exportar sem editar
- **WHEN** quem não é o administrador nem o gerente do projeto abre o menu "⋮"
- **THEN** o menu tem só "Exportar em PDF", sem o XML, sem refazer e sem a linha de base

#### Scenario: XML no menu da gestão
- **WHEN** o gerente de um projeto concluído abre o menu "⋮"
- **THEN** o menu tem "Exportar em PDF" e "Exportar para o MS Project (XML)"
