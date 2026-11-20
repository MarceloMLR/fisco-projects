## MODIFIED Requirements

### Requirement: Precisa de atenção

A visão geral MUST listar o que precisa de atenção no projeto: as tarefas atrasadas, com o responsável, o progresso e o que depende delas; os itens além da data-limite; os chamados em aberto que ameaçam um marco, uma vez só, com o marco mais próximo e o prazo (ADR-0164); os chamados vencidos, que vencem até o próximo dia útil ou sem responsável; os aceites pendentes que a pessoa vê (ADR-0045); e os chamados em fornecedores parados há 5 dias úteis (ADR-0028). Só itens do projeto (ADR-0073).

#### Scenario: Tarefa atrasada na atenção
- **WHEN** a tarefa Validar cadastro de produtos, de Diego, em 80%, venceu e Configurar plano de contas depende dela
- **THEN** a lista mostra a tarefa com "Tarefa de Diego Torres · 80% · bloqueia Configurar plano de contas" e o atraso

#### Scenario: Data-limite na atenção
- **WHEN** Testes integrados passou 1 dia útil da data-limite de 30/10/2026
- **THEN** a lista mostra o item com a data-limite e "1 dia útil depois da data-limite"

#### Scenario: Chamado que ameaça um marco na atenção
- **WHEN** o chamado #104, vencido, está ligado a Testes integrados, de que dependem os marcos Homologação concluída e Go-live, ainda não atingidos
- **THEN** a lista mostra o chamado uma vez só, com "ameaça Homologação concluída" e o dia do marco, o prazo vencido e "e mais 1 marco"

#### Scenario: Chamados na atenção
- **WHEN** o projeto tem um chamado que vence amanhã, um sem responsável e um com prazo para daqui a uma semana
- **THEN** a lista mostra os dois primeiros e não o terceiro

#### Scenario: Aceites e fornecedores na atenção
- **WHEN** o projeto tem um pedido de aceite pendente e um chamado no fornecedor sem atualização há 6 dias úteis
- **THEN** a gerente vê os dois na lista, e um desenvolvedor do projeto vê só o chamado no fornecedor

#### Scenario: Só itens do projeto
- **WHEN** outro projeto da pessoa tem tarefas atrasadas
- **THEN** a visão geral deste projeto não as mostra
