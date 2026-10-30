## ADDED Requirements

### Requirement: Saldo na demonstração

A carga de demonstração MUST incluir horas contratadas e horas adicionais, cadastradas pelo administrador, de modo que Horas dos projetos mostre um projeto acima do contratado, um perto do limite, um dentro e um sem horas contratadas (ADR-0089, ADR-0128).

#### Scenario: Saldo na demonstração
- **WHEN** o comando carrega os dados de demonstração e o administrador abre Horas dos projetos
- **THEN** a visão Em aberto mostra os quatro estados do saldo, e o projeto principal tem horas adicionais com o motivo
