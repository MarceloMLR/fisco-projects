## ADDED Requirements

### Requirement: Relatório da auditoria

Só o administrador MUST exportar a auditoria, com o período em dias da instalação (até 366 dias) e o tipo e a pessoa opcionais. A API MUST devolver todos os registros do filtro, do mais novo para o mais antigo, até 10.000, e acima disso responder 400 com o total. Toda exportação MUST virar o registro "Exportou a auditoria", com os filtros e a quantidade. Valor secreto MUST NOT sair (ADR-0039, ADR-0046, ADR-0099).

#### Scenario: Exportar a auditoria
- **WHEN** o administrador exporta os registros de entrada dos últimos 30 dias
- **THEN** a API devolve só as entradas do período, da mais nova para a mais antiga, e a auditoria passa a ter o registro da exportação, com o administrador, o período, o tipo e a quantidade

#### Scenario: Gerente de projeto não exporta
- **WHEN** o gerente de projeto tenta exportar a auditoria
- **THEN** a API responde 403 e nada é registrado

#### Scenario: Período inválido para exportar
- **WHEN** o administrador pede um período de mais de 366 dias, ou sem o início
- **THEN** a API responde 400 e nada é registrado

#### Scenario: Registros demais para exportar
- **WHEN** o filtro tem mais registros que o limite
- **THEN** a API responde 400 com o total, e nada é registrado

### Requirement: Relatório pela tela

A tela Auditoria MUST ter "Baixar planilha", que baixa um `.xlsx` com os filtros da tela: a aba Auditoria com a data e a hora no fuso da instalação, quem, o que, o item, os detalhes e as alterações com antes e depois, com os textos da tela, e a aba Filtros. A exportação MUST aparecer na lista como "Exportou a auditoria" (ADR-0099).

#### Scenario: Baixar a planilha
- **WHEN** o administrador escolhe "Baixar planilha" com o filtro de uma pessoa
- **THEN** a tela baixa a planilha com os registros da pessoa, com os mesmos textos da lista, e o filtro na aba Filtros

#### Scenario: Muitos registros para a planilha
- **WHEN** o administrador escolhe "Baixar planilha" num período com registros demais
- **THEN** a tela pede um período menor, com o total

#### Scenario: Exportação na lista
- **WHEN** a lista tem uma exportação da auditoria
- **THEN** o registro mostra "Exportou a auditoria", a quantidade e o período
