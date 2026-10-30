## MODIFIED Requirements

### Requirement: Aguardando você

O grupo Aguardando você MUST reunir, qualquer que seja o prazo, os chamados abertos pela pessoa que aguardam a resposta dela, da espera mais antiga para a mais nova. Os pedidos de aceite MUST NOT entrar como aguardando a pessoa: os signatários assinam pelo provedor (ADR-0070, ADR-0096, ADR-0129).

#### Scenario: Chamado aguardando a resposta da pessoa
- **WHEN** Diego, responsável por um chamado aberto por Marina, pede uma informação e o chamado fica Aguardando solicitante
- **THEN** o chamado aparece em Aguardando você na fila de Marina, com "Responder"

#### Scenario: Pedido de aceite para a pessoa
- **WHEN** Carla é a pessoa de um pedido de aceite pendente do fluxo anterior
- **THEN** o pedido não aparece em Aguardando você na fila de Carla

### Requirement: Itens da visão Para mim

"Para mim" MUST trazer os chamados de que a pessoa é responsável, enquanto não aguardam o solicitante; os chamados que ela abriu e aguardam a resposta dela; os chamados sem responsável dos times dela (ADR-0068); as tarefas dela abaixo de 100%, com datas próprias; e os pedidos de assinatura pendentes que ela fez, pelo dia do marco (ADR-0096, ADR-0129).

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
- **WHEN** a gerente pediu a assinatura do termo de um marco de amanhã, ainda pendente
- **THEN** o pedido aparece em "Para mim" da gerente, em Hoje e o próximo dia útil, com "Lembrar"

### Requirement: Ação de cada linha

Cada linha MUST ter uma ação: Responder (o chamado que aguarda a pessoa), Atribuir (o chamado sem responsável, só para quem troca o responsável: o gerente, o líder técnico e o administrador, ADR-0068), Abrir (os outros chamados), Iniciar (a tarefa em 0%), Concluir (a tarefa atrasada, de hoje ou do próximo dia útil), Atualizar (as outras tarefas) e Lembrar (o pedido de assinatura que a pessoa fez) (ADR-0070, ADR-0096, ADR-0129).

#### Scenario: Atribuir só para o gerente e o líder técnico
- **WHEN** um chamado sem responsável do time Desenvolvimento aparece na fila do gerente, do líder técnico e de um desenvolvedor
- **THEN** a ação é "Atribuir" para o gerente e o líder técnico, e "Abrir" para o desenvolvedor

#### Scenario: Ações das tarefas
- **WHEN** a pessoa tem uma tarefa em 0% para hoje, uma atrasada em 80% e uma em 50% para daqui a 5 dias
- **THEN** as ações são "Iniciar", "Concluir" e "Atualizar"
