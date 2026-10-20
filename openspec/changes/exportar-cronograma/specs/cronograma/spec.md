## ADDED Requirements

### Requirement: Exportar para o MS Project

Todas as pessoas do projeto e o administrador MUST poder baixar o cronograma no formato XML do MS Project. O arquivo leva as fases, as tarefas, as subtarefas e os marcos em níveis, com as datas, a duração em dias úteis, o progresso, o responsável, as dependências "termina, então começa", a linha de base e o calendário da instalação. Quem não está no projeto recebe 404 (ADR-0021, ADR-0043, ADR-0051).

#### Scenario: XML do cronograma
- **WHEN** uma pessoa do projeto baixa o XML de um cronograma com uma fase, uma tarefa de 3 dias úteis e um marco depois dela
- **THEN** o arquivo tem a fase no nível 1, a tarefa no nível 2, de 13/10/2026 às 08:00 a 15/10/2026 às 17:00, com 24 horas de duração, e o marco com duração zero, ligado à tarefa

#### Scenario: Calendário no XML
- **WHEN** o cronograma passa pelo feriado de 12/10/2026, uma segunda-feira
- **THEN** o calendário do arquivo tem segunda a sexta como dias de trabalho e o dia 12/10/2026 como exceção sem trabalho

#### Scenario: Linha de base no XML
- **WHEN** o cronograma tem a linha de base salva
- **THEN** cada tarefa e marco dela leva no arquivo o início e o fim da linha de base

### Requirement: Exportar em PDF pela impressão

Todas as pessoas do projeto MUST poder abrir a página de impressão do cronograma, com o período e a escala que escolherem. A página mostra o cabeçalho do projeto, a tabela com a linha do tempo de cada linha e a legenda, em A4 paisagem, e o botão "Imprimir" abre a impressão do navegador, que salva em PDF (ADR-0021, ADR-0071).

#### Scenario: Escolher o período e a escala
- **WHEN** uma pessoa do projeto escolhe "Exportar em PDF" no menu "⋮", troca o período e a escala e confirma
- **THEN** a página de impressão abre numa aba nova com esse período e essa escala

#### Scenario: Página de impressão
- **WHEN** a página de impressão abre com o período de 05/10/2026 a 31/10/2026, em dias
- **THEN** ela mostra o código e o nome do projeto, o período, uma linha por fase e item com início, fim e progresso, a barra de cada tarefa dentro do período, e a legenda

#### Scenario: Exportar sem editar
- **WHEN** quem não edita o cronograma abre o menu "⋮"
- **THEN** o menu tem "Exportar em PDF" e "Exportar para o MS Project (XML)", sem refazer nem a linha de base
