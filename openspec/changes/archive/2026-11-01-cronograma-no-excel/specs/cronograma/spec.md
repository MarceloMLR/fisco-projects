## ADDED Requirements

### Requirement: Exportar para o Excel

Todas as pessoas do projeto MUST poder baixar o cronograma numa planilha do Excel pelo menu "⋮", também com o projeto encerrado: uma linha por fase e item, na ordem da tela, com o responsável, as datas, a duração, o progresso, a situação, o atraso, as dependências, o caminho crítico e a linha de base, e uma aba com os dados do projeto. A planilha MUST NOT levar e-mails (ADR-0021, ADR-0099, ADR-0136).

#### Scenario: Cronograma no Excel
- **WHEN** uma pessoa do projeto escolhe "Exportar para Excel" num cronograma com uma fase, uma tarefa com uma subtarefa, um marco que depende dela e a linha de base salva
- **THEN** ela baixa o arquivo com o código do projeto no nome, com a aba "Cronograma" (uma linha para a fase, a tarefa, a subtarefa recuada e o marco, com as datas como datas do Excel e o marco apontando para a linha da tarefa) e a aba "Projeto", sem nenhum e-mail

#### Scenario: Excel do projeto encerrado
- **WHEN** uma pessoa do projeto concluído escolhe "Exportar para Excel"
- **THEN** ela baixa a planilha com o cronograma como ele ficou

#### Scenario: Excel sem cronograma
- **WHEN** o cronograma não tem tarefas nem marcos
- **THEN** "Exportar para Excel" aparece desabilitado no menu "⋮", como o PDF

## MODIFIED Requirements

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
- **THEN** o menu tem só "Exportar em PDF" e "Exportar para Excel", sem o XML, sem refazer e sem a linha de base

#### Scenario: XML no menu da gestão
- **WHEN** o gerente de um projeto concluído abre o menu "⋮"
- **THEN** o menu tem "Exportar em PDF" e "Exportar para o MS Project (XML)"
