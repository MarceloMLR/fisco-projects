## ADDED Requirements

### Requirement: Horas do projeto no relatório de status

Para o relatório de status com "Mostrar horas", o módulo das horas MUST entregar a quem gera o relatório (o administrador e o gerente do projeto) só os totais: as horas contratadas, as adicionais, as apontadas, o saldo e o estado do projeto, e as previstas e as apontadas de cada tarefa. Nunca as horas de cada pessoa nem os apontamentos. Fora do relatório, o saldo continua só do administrador (ADR-0128, ADR-0144, ADR-0166).

#### Scenario: Saldo do projeto no relatório pelo gerente
- **WHEN** a gerente do ERP-FIN gera o relatório com "Mostrar horas" e depois pede as horas dos projetos na Administração
- **THEN** o relatório traz o saldo do ERP-FIN, e o pedido na Administração recebe 403

#### Scenario: Horas do relatório sem as pessoas
- **WHEN** o administrador gera o relatório com "Mostrar horas" de um projeto com apontamentos de três pessoas
- **THEN** o que o módulo das horas entrega tem só os totais do projeto e das tarefas, sem nenhuma pessoa nem apontamento
