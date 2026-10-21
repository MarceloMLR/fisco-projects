## ADDED Requirements

### Requirement: Primeira abertura mais rápida

A página do projeto MUST pedir a leitura da aba junto com o projeto, sem esperar por ele. Apontar com o mouse um projeto ou um chamado das listas, ou um projeto da barra lateral, MUST começar a ler o que a página vai mostrar, só o que ainda não está guardado. Abrir um projeto no computador MUST começar a carregar o código da Linha do tempo (ADR-0106).

#### Scenario: Aba pedida junto com o projeto
- **WHEN** a pessoa abre a Visão geral de um projeto
- **THEN** a visão geral é pedida antes de o projeto chegar

#### Scenario: Projeto pedido ao apontar a linha
- **WHEN** a pessoa aponta com o mouse um projeto da lista
- **THEN** o projeto e a visão geral são pedidos

#### Scenario: Linha do tempo carregada antes
- **WHEN** um projeto abre no computador
- **THEN** o código da Linha do tempo começa a carregar
