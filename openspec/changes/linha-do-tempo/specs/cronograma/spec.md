# Spec Delta

## MODIFIED Requirements

### Requirement: Aba Cronograma

A página do projeto MUST ter a aba Cronograma, entre Visão geral e Membros, que alterna entre Linha do tempo e Lista; no computador abre na Linha do tempo e no celular, na Lista. A Lista MUST ter as fases em linhas de grupo, com datas e progresso, e abaixo delas as tarefas e os marcos em ordem de início, com início, fim, duração, responsável, progresso e dependências. O marco mostra Atingido, Pendente ou Atrasado; a tarefa atrasada, "Atrasada" (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma pela tela
- **WHEN** uma pessoa do projeto abre a Lista da aba Cronograma de um projeto com fases, tarefas, um marco e uma tarefa atrasada
- **THEN** a tela mostra as fases com as datas e o progresso, as tarefas e o marco abaixo delas, a tarefa atrasada com o texto "Atrasada", e a trilha mostra Projetos, o código e Cronograma

#### Scenario: Cronograma vazio
- **WHEN** o gerente abre a aba Cronograma de um projeto sem fases
- **THEN** a tela explica que o cronograma ainda não tem fases e oferece "Nova fase"

#### Scenario: Estados do marco pela tela
- **WHEN** hoje é 16/10/2026 e o cronograma tem um marco atingido em 09/10/2026, um pendente em 03/11/2026 e um não atingido de 15/10/2026
- **THEN** a Lista mostra "Atingido em 09/10", "Pendente" e "Atrasado", este com ícone e cor, e o resumo conta um marco atrasado

#### Scenario: Alternar Linha do tempo e Lista
- **WHEN** uma pessoa abre a aba Cronograma no computador e depois escolhe "Lista"
- **THEN** a aba abre na Linha do tempo e passa para a Lista, e no celular a aba abre na Lista

## ADDED Requirements

### Requirement: Linha do tempo

A Linha do tempo MUST mostrar à esquerda o nome e o progresso de cada fase, tarefa e marco e, à direita, as fases como barras de resumo, as tarefas como barras com a parte concluída em verde, os marcos como losangos, as dependências como setas, os dias não úteis sombreados e a linha de hoje com "Hoje". A tarefa atrasada MUST ter ícone e cor. A palavra "Gantt" MUST NOT aparecer (ADR-0051, ADR-0065, ADR-0067, ADR-0071).

#### Scenario: Linha do tempo pela tela
- **WHEN** hoje é 16/10/2026 e uma pessoa do projeto abre a Linha do tempo de um projeto com fases, tarefas, um marco, dependências e uma tarefa atrasada
- **THEN** a tela mostra cada fase com as tarefas e o marco abaixo dela, com o progresso, a tarefa atrasada com ícone e cor, as barras nas datas do cronograma e a linha de hoje em 16/10/2026

#### Scenario: Dias não úteis na linha do tempo
- **WHEN** a instalação considera o feriado de 12/10/2026 e trabalha de segunda a sexta
- **THEN** a Linha do tempo sombreia os sábados, os domingos e 12/10/2026

### Requirement: Escala e ajuste ao espaço

A Linha do tempo MUST ter as escalas Dias, Semanas e Meses. O período do cronograma MUST ocupar toda a largura disponível e a grade descer até o fim da área; se o período não couber com a largura mínima de cada dia, semana ou mês, a rolagem lateral MUST ficar só dentro da linha do tempo. "Hoje" MUST levar a linha do tempo até o dia de hoje (ADR-0071).

#### Scenario: Período ajustado ao espaço
- **WHEN** um cronograma de 05/10/2026 a 13/11/2026 é aberto na escala Dias num espaço que cabe o período
- **THEN** o período ocupa toda a largura da linha do tempo, sem espaço em branco nem rolagem lateral

#### Scenario: Período que não cabe
- **WHEN** um cronograma de um ano é aberto na escala Dias
- **THEN** cada dia fica com a largura mínima e a rolagem lateral aparece só dentro da linha do tempo

#### Scenario: Mudar a escala
- **WHEN** a pessoa escolhe Semanas e depois Meses
- **THEN** a linha do tempo passa a mostrar as semanas e depois os meses do período, ainda ocupando a largura

### Requirement: Arrastar na linha do tempo

No computador, quem edita o cronograma MUST poder arrastar a barra de uma tarefa ou de um marco para mudar o início, mantendo a duração em dias úteis, e puxar a borda da barra de uma tarefa para mudar a duração. O servidor MUST recalcular as datas e empurrar as seguintes, e a Linha do tempo MUST mostrar as datas que ele devolve, com uma mensagem dos itens empurrados. Quem não edita e o celular MUST NOT arrastar (ADR-0019, ADR-0021, ADR-0043, ADR-0051).

#### Scenario: Arrastar uma tarefa
- **WHEN** o gerente arrasta uma tarefa de 3 dias úteis que começa em 13/10/2026 para começar em 16/10/2026
- **THEN** a tarefa passa a ir de 16/10/2026 a 20/10/2026, as seguintes são empurradas e a mensagem diz quantas

#### Scenario: Puxar a borda de uma tarefa
- **WHEN** o gerente puxa o fim de uma tarefa que vai de 13/10/2026 a 15/10/2026 até 20/10/2026
- **THEN** a tarefa passa a durar 6 dias úteis, terminando em 20/10/2026

#### Scenario: Arrastar sem permissão
- **WHEN** o líder técnico abre a Linha do tempo
- **THEN** as barras não se arrastam nem se ligam

### Requirement: Ligar duas barras

Na Linha do tempo, quem edita o cronograma MUST poder ligar o fim de uma barra ao início de outra para criar a dependência; o servidor empurra a seguinte se preciso. A dependência circular MUST ser recusada com uma mensagem, sem mudar o cronograma (ADR-0019, ADR-0021).

#### Scenario: Ligar duas barras
- **WHEN** o gerente liga o fim de "Configurar plano de contas" ao início de "Testes integrados"
- **THEN** "Testes integrados" passa a depender de "Configurar plano de contas", e a seta aparece

#### Scenario: Ligação circular pela linha do tempo
- **WHEN** o gerente liga o fim de uma tarefa ao início de outra que vem antes dela na mesma cadeia
- **THEN** a tela diz que a dependência formaria um ciclo, e nada muda

### Requirement: Novas datas pela API

A API MUST aceitar a mudança das datas de uma tarefa ou de um marco pelo início e, numa tarefa, também pelo fim. Só com o início, a duração em dias úteis fica a mesma; com o fim, a duração passa a ser os dias úteis entre o início e o fim, de 1 a 999. O fim antes do início, o fim num marco ou a duração fora dos limites MUST receber 400; quem edita, o projeto encerrado e as seguintes empurradas seguem a alteração da tarefa (ADR-0018, ADR-0019, ADR-0043).

#### Scenario: Mudar o início pela API
- **WHEN** o gerente muda pela API o início de uma tarefa de 3 dias úteis para 16/10/2026, sem o fim
- **THEN** a tarefa vai de 16/10/2026 a 20/10/2026 e a resposta diz quantos itens foram empurrados

#### Scenario: Mudar o fim pela API
- **WHEN** o gerente muda pela API o fim de uma tarefa que começa em 13/10/2026 para 20/10/2026, mantendo o início
- **THEN** a tarefa passa a durar 6 dias úteis

#### Scenario: Fim antes do início
- **WHEN** o gerente manda o fim antes do início, ou um fim para um marco
- **THEN** a API responde 400 e nada muda

### Requirement: Dias não úteis no cronograma

O cronograma MUST trazer os dias não úteis do calendário da instalação, do mês anterior ao início ao mês seguinte ao fim, para a Linha do tempo sombreá-los sem repetir a regra do calendário; sem tarefas nem marcos, nenhum (ADR-0018, ADR-0051).

#### Scenario: Dias não úteis no cronograma
- **WHEN** a instalação considera o feriado de 12/10/2026 e o cronograma vai de 05/10/2026 a 13/11/2026
- **THEN** o cronograma traz 12/10/2026 e os fins de semana de setembro a dezembro de 2026, e nenhum dia útil

### Requirement: Detalhe pela linha do tempo

Escolher uma tarefa ou um marco na Linha do tempo MUST abrir o mesmo detalhe da Lista, e escolher uma fase, para quem edita, MUST abrir a fase (ADR-0081).

#### Scenario: Detalhe pela linha do tempo
- **WHEN** uma pessoa do projeto escolhe uma tarefa na Linha do tempo
- **THEN** o detalhe da tarefa abre na gaveta, como na Lista

### Requirement: Expandir o cronograma

"Expandir" MUST abrir um diálogo que cobre quase a tela inteira, com o título Cronograma, o código e o nome do projeto, a escala, "Hoje", "Nova tarefa" para quem edita e "Recolher", e a Linha do tempo com a tabela completa: nome, início, fim, responsável e progresso (ADR-0071).

#### Scenario: Expandir o cronograma
- **WHEN** o gerente escolhe "Expandir" na Linha do tempo
- **THEN** o diálogo cobre quase a tela inteira, com a tabela completa ao lado da linha do tempo, e "Recolher" volta à aba
