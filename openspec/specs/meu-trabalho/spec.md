# meu-trabalho Specification

## Purpose

Meu trabalho, a página inicial: a fila por urgência dos chamados, das tarefas e dos pedidos de aceite da pessoa nos projetos que ela vê, montada no servidor com os itens de cada módulo, os grupos Atrasado, Aguardando você, Hoje e o próximo dia útil e Próximos 7 dias, a ação de cada linha, as visões Para mim, Abertos por mim e Que acompanho, os meus projetos, os próximos marcos e a tela no computador e no celular (ADR-0070, ADR-0096).

## Requirements

### Requirement: Grupos da fila

A fila MUST agrupar cada item pelo prazo, no fuso e no calendário da instalação: Atrasado (antes de hoje), Hoje e o próximo dia útil (de hoje até o próximo dia útil), e Próximos 7 dias (depois disso, até 7 dias corridos depois de hoje). Item com prazo depois dos 7 dias MUST ficar fora da fila (ADR-0070, ADR-0096).

#### Scenario: Prazo vencido no grupo Atrasado
- **WHEN** hoje é segunda, 19/10/2026, e uma tarefa da pessoa terminou em 16/10/2026 com 80%
- **THEN** a tarefa aparece em Atrasado

#### Scenario: Sexta-feira com o próximo dia útil na segunda
- **WHEN** hoje é sexta, 16/10/2026, e há itens da pessoa com prazo hoje, na segunda, 19/10, e na terça, 20/10
- **THEN** os de hoje e de segunda aparecem em Hoje e o próximo dia útil, e o de terça em Próximos 7 dias

#### Scenario: Véspera de feriado
- **WHEN** hoje é sexta, 30/10/2026, segunda, 02/11, é feriado nacional, e um chamado vence na terça, 03/11
- **THEN** o chamado aparece em Hoje e o próximo dia útil

#### Scenario: Sete dias corridos
- **WHEN** hoje é segunda, 19/10/2026, e há tarefas da pessoa que terminam em 26/10 e em 27/10
- **THEN** a de 26/10 aparece em Próximos 7 dias, e a de 27/10 fica fora da fila

### Requirement: Aguardando você

O grupo Aguardando você MUST reunir, qualquer que seja o prazo, os chamados abertos pela pessoa que aguardam a resposta dela e os pedidos de aceite pendentes para ela, da espera mais antiga para a mais nova (ADR-0070, ADR-0096).

#### Scenario: Chamado aguardando a resposta da pessoa
- **WHEN** Diego, responsável por um chamado aberto por Marina, pede uma informação e o chamado fica Aguardando solicitante
- **THEN** o chamado aparece em Aguardando você na fila de Marina, com "Responder"

#### Scenario: Pedido de aceite para a pessoa
- **WHEN** a gerente pede a Carla o aceite de um marco que vence daqui a 30 dias
- **THEN** o pedido aparece em Aguardando você na fila de Carla, com "Responder"

### Requirement: Itens da visão Para mim

"Para mim" MUST trazer os chamados de que a pessoa é responsável, enquanto não aguardam o solicitante; os chamados que ela abriu e aguardam a resposta dela; os chamados sem responsável dos times dela (ADR-0068); as tarefas dela abaixo de 100%, com datas próprias; os pedidos de aceite pendentes para ela; e os pedidos pendentes que ela fez, pelo dia do marco (ADR-0096).

#### Scenario: Chamado do responsável
- **WHEN** Diego é o responsável por um chamado Em andamento cuja resolução vence amanhã
- **THEN** o chamado aparece em "Para mim" de Diego, em Hoje e o próximo dia útil

#### Scenario: Chamado aguardando o solicitante fica fora para o responsável
- **WHEN** o chamado de que Diego é responsável fica Aguardando solicitante
- **THEN** o chamado sai de "Para mim" de Diego

#### Scenario: Chamado sem responsável do time
- **WHEN** um chamado do time Desenvolvimento, sem responsável, tem a primeira resposta para hoje
- **THEN** o chamado aparece em "Para mim" do gerente, do líder técnico e dos desenvolvedores, e não na do colaborador

#### Scenario: Primeira resposta e resolução
- **WHEN** hoje é 19/10/2026 e um chamado ainda sem resposta tem a primeira resposta para amanhã e a resolução para 26/10
- **THEN** o chamado entra em Hoje e o próximo dia útil, pela primeira resposta, e, depois de respondido, em Próximos 7 dias, pela resolução

#### Scenario: Tarefa concluída e tarefa com subtarefas ficam fora
- **WHEN** a pessoa é responsável por uma tarefa a 100% e por uma tarefa com subtarefas
- **THEN** nenhuma das duas aparece na fila, e as subtarefas dela aparecem

#### Scenario: Pedido feito pela pessoa
- **WHEN** a gerente pediu o aceite de um marco de amanhã, ainda pendente
- **THEN** o pedido aparece em "Para mim" da gerente, em Hoje e o próximo dia útil, com "Lembrar"

### Requirement: Visões Abertos por mim e Que acompanho

"Abertos por mim" MUST trazer os chamados que a pessoa abriu, não resolvidos, e os pedidos de aceite pendentes que ela fez; "Que acompanho", os chamados que ela acompanha, não resolvidos. Cada visão MUST ter a contagem dos itens da fila (ADR-0070, ADR-0096).

#### Scenario: Abertos por mim
- **WHEN** Carla abriu dois chamados com resolução nesta semana e um já resolvido
- **THEN** "Abertos por mim" de Carla traz os dois chamados não resolvidos, com a contagem 2

#### Scenario: Que acompanho
- **WHEN** Lucas acompanha um chamado que vence amanhã, sem ser solicitante nem responsável
- **THEN** o chamado aparece em "Que acompanho" de Lucas, e não em "Para mim"

### Requirement: Só itens da pessoa nos projetos que ela vê

A fila MUST trazer só itens da pessoa na sessão, de projetos que ela vê e que não estão concluídos nem cancelados (ADR-0010, ADR-0070).

#### Scenario: Item de outra pessoa
- **WHEN** Diego tem uma tarefa atrasada
- **THEN** a tarefa não aparece na fila de Carla

#### Scenario: Projeto que a pessoa não vê
- **WHEN** Sofia, fora do projeto, é solicitante de um chamado antigo dele
- **THEN** o chamado não aparece na fila de Sofia

#### Scenario: Projeto encerrado
- **WHEN** o projeto de uma tarefa atrasada da pessoa é concluído
- **THEN** a tarefa sai da fila

### Requirement: Ação de cada linha

Cada linha MUST ter uma ação: Responder (o chamado e o pedido que aguardam a pessoa), Atribuir (o chamado sem responsável, só para quem troca o responsável: o gerente, o líder técnico e o administrador, ADR-0068), Abrir (os outros chamados), Iniciar (a tarefa em 0%), Concluir (a tarefa atrasada, de hoje ou do próximo dia útil), Atualizar (as outras tarefas) e Lembrar (o pedido que a pessoa fez) (ADR-0070, ADR-0096).

#### Scenario: Atribuir só para o gerente e o líder técnico
- **WHEN** um chamado sem responsável do time Desenvolvimento aparece na fila do gerente, do líder técnico e de um desenvolvedor
- **THEN** a ação é "Atribuir" para o gerente e o líder técnico, e "Abrir" para o desenvolvedor

#### Scenario: Ações das tarefas
- **WHEN** a pessoa tem uma tarefa em 0% para hoje, uma atrasada em 80% e uma em 50% para daqui a 5 dias
- **THEN** as ações são "Iniciar", "Concluir" e "Atualizar"

### Requirement: Fila pela API

`GET /api/my-work` MUST responder, para a pessoa na sessão, o dia de hoje e o próximo dia útil da instalação e as três visões, cada uma com a contagem e os grupos não vazios, na ordem Atrasado, Aguardando você, Hoje e o próximo dia útil, e Próximos 7 dias; em cada grupo, os itens pelo prazo, do mais antigo para o mais novo (ADR-0096).

#### Scenario: Fila pela API
- **WHEN** a pessoa pede a fila
- **THEN** a API responde as três visões, com a contagem, os grupos não vazios na ordem e, em cada item, o tipo, o projeto, o título, o prazo e a ação

#### Scenario: Fila sem sessão
- **WHEN** alguém pede a fila sem sessão
- **THEN** a API responde 401

### Requirement: Meu trabalho na tela inicial

A tela inicial MUST mostrar o título Meu trabalho com a data de hoje, as visões no `SegmentedControl` com a contagem, a fila agrupada com o ícone e a contagem de cada grupo e, em cada linha, o ícone do tipo, o título, o código do projeto, o detalhe, a situação (chamado e termo), o prazo com ícone, cor e texto e a ação. A tela MUST NOT ter "Abrir chamado" nem "Novo projeto" (ADR-0065, ADR-0070).

#### Scenario: Fila na tela inicial
- **WHEN** a pessoa abre o Início
- **THEN** a tela mostra Meu trabalho com a data, "Para mim" escolhida e os grupos da fila, cada linha com o código do projeto, o prazo e a ação

#### Scenario: Trocar a visão
- **WHEN** a pessoa escolhe "Abertos por mim"
- **THEN** a fila mostra os itens dessa visão

#### Scenario: Sem atalhos na tela inicial
- **WHEN** um gerente de projeto abre o Início
- **THEN** a tela não tem "Abrir chamado" nem "Novo projeto"

#### Scenario: Fila vazia
- **WHEN** a visão escolhida não tem itens
- **THEN** a fila mostra uma mensagem de que nada pede a ação da pessoa nos próximos dias

### Requirement: Ações pela tela

Concluir MUST levar a tarefa a 100% ali mesmo; Iniciar e Atualizar MUST pedir a porcentagem num diálogo; Atribuir MUST abrir a troca do responsável do chamado; Abrir, Responder e Lembrar MUST levar ao chamado ou ao termo. O título de cada linha MUST levar ao item: o chamado, os detalhes da tarefa no cronograma ou o termo. Depois de uma ação, a fila MUST se atualizar (ADR-0070, ADR-0081).

#### Scenario: Concluir pela fila
- **WHEN** a pessoa escolhe "Concluir" numa tarefa atrasada
- **THEN** a tarefa vai a 100% e sai da fila

#### Scenario: Atualizar pela fila
- **WHEN** a pessoa escolhe "Atualizar" numa tarefa e informa 70%
- **THEN** a tarefa fica com 70% e a linha mostra o novo progresso

#### Scenario: Atribuir pela fila
- **WHEN** o gerente escolhe "Atribuir" num chamado sem responsável e escolhe Diego
- **THEN** o chamado fica com Diego e a fila se atualiza

#### Scenario: Título leva ao item
- **WHEN** a pessoa escolhe o título de uma tarefa na fila
- **THEN** o cronograma do projeto abre com os detalhes da tarefa

### Requirement: Meus projetos pela tela

O cartão Meus projetos MUST listar os projetos abertos de que a pessoa participa, como a barra lateral, com o nome, o código, a faixa de fases (ADR-0071), a porcentagem concluída e o atraso em dias úteis do item mais atrasado, com ícone, cor e texto; sem cronograma, "Sem cronograma"; e "Ver todos", para Projetos (ADR-0070, ADR-0096).

#### Scenario: Projeto atrasado em Meus projetos
- **WHEN** um projeto da pessoa tem uma tarefa com 2 dias úteis de atraso e um marco com 1
- **THEN** o cartão mostra a faixa, a porcentagem concluída e "2 dias úteis de atraso"

#### Scenario: Projeto sem cronograma em Meus projetos
- **WHEN** um projeto da pessoa não tem tarefas nem marcos
- **THEN** o cartão mostra "Sem cronograma", sem a faixa

#### Scenario: Sem projetos
- **WHEN** a pessoa não está em nenhum projeto aberto
- **THEN** o cartão mostra uma mensagem de que ela ainda não está em nenhum projeto

### Requirement: Próximos marcos pela tela

O cartão Próximos marcos MUST listar os próximos marcos dos projetos da pessoa, com o dia em destaque, o nome, os dias úteis que faltam, "aceite pedido" quando há um pedido pendente e o código do projeto; sem marcos, uma mensagem (ADR-0070, ADR-0096).

#### Scenario: Próximos marcos na tela
- **WHEN** hoje é 19/10/2026 e um projeto da pessoa tem o marco Homologação em 03/11/2026, com um pedido de aceite pendente
- **THEN** o cartão mostra "03" e "nov", Homologação, "Em 10 dias úteis · aceite pedido" e o código do projeto

#### Scenario: Sem próximos marcos
- **WHEN** os projetos da pessoa não têm marcos de hoje em diante
- **THEN** o cartão mostra uma mensagem de que não há marcos pela frente

### Requirement: Meu trabalho no celular

No celular, o Meu trabalho MUST mostrar as visões, a fila em linhas de duas camadas (o título; o código do projeto, o tipo e o prazo) e, depois da fila, Meus projetos e Próximos marcos, como o canvas CelularMeuTrabalho (ADR-0066).

#### Scenario: Meu trabalho no celular
- **WHEN** a pessoa abre o Início no celular
- **THEN** a tela mostra as visões e a fila em duas camadas, com o código do projeto, o tipo e o prazo
