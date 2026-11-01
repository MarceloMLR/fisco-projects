## ADDED Requirements

### Requirement: Notas da tarefa e do marco

Cada tarefa, também a resumo e a subtarefa, e cada marco MAY ter uma nota de texto simples, com quebras de linha, de até 2.000 caracteres. Só quem edita o cronograma MUST escrever, trocar e apagar a nota, pelo pedido de incluir ou de editar o item; todos do projeto MUST vê-la. A nota ausente ou nula no pedido MUST ficar como estava, e a nota vazia MUST ser apagada (ADR-0043, ADR-0139).

#### Scenario: Escrever a nota
- **WHEN** a gerente escreve na tarefa "Configurar plano de contas" a nota "Aguardando o cliente mandar os arquivos do plano de contas"
- **THEN** o cronograma traz a tarefa com a nota, para todos do projeto

#### Scenario: Nota mantida sem o campo
- **WHEN** a gerente arrasta a tarefa com nota na Linha do tempo, num pedido sem a nota
- **THEN** a tarefa muda de datas e continua com a nota

#### Scenario: Apagar a nota
- **WHEN** a gerente salva a tarefa com a nota vazia
- **THEN** a tarefa fica sem nota

#### Scenario: Nota longa demais
- **WHEN** a gerente escreve uma nota de 2.001 caracteres
- **THEN** a API recusa com 400 no campo da nota, e nada muda

### Requirement: Notas no desfazer e na auditoria

A nota MUST entrar no desfazer de quem a mudou e na auditoria, e MUST NOT mudar as datas, o progresso, a linha de base nem os modelos de projeto (ADR-0039, ADR-0091, ADR-0139).

#### Scenario: Desfazer a nota
- **WHEN** a gerente escreve a nota de uma tarefa e escolhe "Desfazer"
- **THEN** a tarefa volta à nota de antes, e a auditoria tem as duas mudanças

### Requirement: Notas no MS Project

A exportação para o MS Project MUST gravar a nota de cada item no campo de notas da tarefa, e a importação MUST trazer a nota de cada tarefa e marco do arquivo, cortada em 2.000 caracteres com um aviso que diz os itens cortados (ADR-0135, ADR-0139).

#### Scenario: Nota exportada para o MS Project
- **WHEN** a gerente baixa o XML do MS Project de um cronograma com uma tarefa com nota
- **THEN** a tarefa do arquivo tem a mesma nota

#### Scenario: Nota importada do MS Project
- **WHEN** quem cria projetos importa um arquivo com uma tarefa com nota e outra com uma nota de 2.500 caracteres
- **THEN** o projeto nasce com a primeira nota inteira e a segunda cortada em 2.000 caracteres, e a prévia avisa o corte com o nome da tarefa

### Requirement: Notas pela tela

A Lista e a tabela da Linha do tempo MUST mostrar um ícone de nota ao lado do nome do item com nota, com o texto da nota ao apontar e para quem lê a tela; o detalhe MUST mostrar a nota, e quem edita MUST ter o campo "Notas" no formulário do item; o Excel MUST ter a coluna "Notas" (ADR-0139).

#### Scenario: Nota na Lista
- **WHEN** alguém do projeto abre a Lista de um cronograma com uma tarefa com nota
- **THEN** o nome da tarefa tem o ícone de nota, com o texto da nota

#### Scenario: Escrever a nota pela tela
- **WHEN** a gerente abre o detalhe da tarefa, escreve a nota no campo "Notas" e salva
- **THEN** a tarefa passa a mostrar o ícone de nota, e a tela confirma

#### Scenario: Nota no detalhe de quem só vê
- **WHEN** quem não edita o cronograma abre o detalhe de uma tarefa com nota
- **THEN** o detalhe mostra a nota, sem campo para mudá-la

#### Scenario: Nota no Excel
- **WHEN** alguém do projeto baixa o Excel do cronograma
- **THEN** a aba Cronograma tem a coluna "Notas", com a nota de cada item
