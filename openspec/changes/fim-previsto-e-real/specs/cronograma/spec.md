## ADDED Requirements

### Requirement: Fim previsto e fim real

Cada tarefa e marco MUST ter o fim previsto, o fim na linha de base (o dia do marco), vazio sem linha de base ou no item incluído depois dela, e o fim real, o dia em que a tarefa chegou a 100% (na tarefa-resumo, o da última subtarefa) ou o marco foi atingido, vazio em aberto. A fase MUST ter o maior fim previsto dos itens dela e, com todos concluídos, o maior fim real (ADR-0020, ADR-0021, ADR-0088).

#### Scenario: Tarefa concluída depois do previsto
- **WHEN** a linha de base guardou o fim de uma tarefa em 15/10/2026 e a tarefa chegou a 100% em 20/10/2026
- **THEN** o fim previsto é 15/10/2026 e o fim real, 20/10/2026

#### Scenario: Marco atingido antes do previsto
- **WHEN** a linha de base guardou um marco em 23/10/2026 e ele foi atingido em 22/10/2026
- **THEN** o fim previsto é 23/10/2026 e o fim real, 22/10/2026

#### Scenario: Item em aberto ou incluído depois da linha de base
- **WHEN** uma tarefa está em 60%, e outra foi incluída depois de salva a linha de base
- **THEN** a primeira não tem fim real, e a segunda não tem fim previsto

#### Scenario: Fim previsto e real da fase
- **WHEN** a fase tem duas tarefas previstas para 13/10/2026 e 15/10/2026, concluídas em 14/10/2026 e 16/10/2026
- **THEN** a fase tem o fim previsto 15/10/2026 e o fim real 16/10/2026, e, com uma tarefa reaberta, fica sem fim real

### Requirement: Fim previsto e fim real na Lista

A Lista MUST mostrar, logo depois de "Fim", as colunas "Fim previsto", só com a linha de base salva, e "Fim real", só para ler, também no modo de edição na tabela (ADR-0021, ADR-0071, ADR-0143).

#### Scenario: Fim previsto e real na Lista
- **WHEN** uma pessoa do projeto abre a Lista de um cronograma com a linha de base e uma tarefa prevista para 15/10 e concluída em 20/10
- **THEN** a linha da tarefa mostra 15/10 em "Fim previsto" e 20/10 em "Fim real", ao lado do fim

#### Scenario: Lista sem linha de base
- **WHEN** uma pessoa do projeto abre a Lista de um cronograma sem linha de base
- **THEN** a Lista mostra "Fim real" e não mostra "Fim previsto"

#### Scenario: Fim previsto e real na edição na tabela
- **WHEN** o gerente liga "Editar na tabela" e escolhe a célula do fim real
- **THEN** a célula não entra em edição

## MODIFIED Requirements

### Requirement: Expandir o cronograma

"Expandir" MUST abrir um diálogo que cobre quase a tela inteira, com o título Cronograma, o código e o nome do projeto, a escala, "Hoje", "Nova tarefa" para quem edita e "Recolher", e a Linha do tempo com a tabela completa: nome, início, fim, fim previsto (com a linha de base salva e ligada em "Exibir"), fim real, data-limite, responsável e progresso (ADR-0071).

#### Scenario: Expandir o cronograma
- **WHEN** o gerente escolhe "Expandir" na Linha do tempo
- **THEN** o diálogo cobre quase a tela inteira, com a tabela completa ao lado da linha do tempo, e "Recolher" volta à aba

#### Scenario: Fim previsto e real no Expandir
- **WHEN** o gerente abre o Expandir de um cronograma com a linha de base e desliga a linha de base em "Exibir"
- **THEN** a tabela mostra o fim previsto e o fim real depois do fim, e o fim previsto some ao desligar

### Requirement: Exportar para o Excel

Todas as pessoas do projeto MUST poder baixar o cronograma numa planilha do Excel pelo menu "⋮", também com o projeto encerrado: uma linha por fase e item, na ordem da tela, com o responsável, as datas, com o fim previsto e o fim real ao lado do fim, a duração, o progresso, a situação, o atraso, as dependências, o caminho crítico e a linha de base, e uma aba com os dados do projeto. A planilha MUST NOT levar e-mails (ADR-0021, ADR-0099, ADR-0136).

#### Scenario: Cronograma no Excel
- **WHEN** uma pessoa do projeto escolhe "Exportar para Excel" num cronograma com uma fase, uma tarefa com uma subtarefa, um marco que depende dela e a linha de base salva
- **THEN** ela baixa o arquivo com o código do projeto no nome, com a aba "Cronograma" (uma linha para a fase, a tarefa, a subtarefa recuada e o marco, com as datas como datas do Excel e o marco apontando para a linha da tarefa) e a aba "Projeto", sem nenhum e-mail

#### Scenario: Fim previsto e real no Excel
- **WHEN** uma pessoa do projeto exporta o cronograma com uma tarefa prevista para 15/10/2026 e concluída em 20/10/2026
- **THEN** a linha da tarefa tem, depois do fim, as colunas "Fim previsto (linha de base)" com 15/10/2026 e "Fim real" com 20/10/2026, como datas do Excel

#### Scenario: Excel do projeto encerrado
- **WHEN** uma pessoa do projeto concluído escolhe "Exportar para Excel"
- **THEN** ela baixa a planilha com o cronograma como ele ficou

#### Scenario: Excel sem cronograma
- **WHEN** o cronograma não tem tarefas nem marcos
- **THEN** "Exportar para Excel" aparece desabilitado no menu "⋮", como o PDF
