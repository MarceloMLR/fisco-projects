## ADDED Requirements

### Requirement: Iniciar atendimento assume o chamado

"Iniciar atendimento" num chamado sem responsável MUST tornar responsável quem fez a ação, se essa pessoa pode ser responsável pelo chamado (quem atende o time ou o gerente do projeto), e fazê-la acompanhar o chamado, com o evento na conversa. Com responsável, o responsável MUST continuar o mesmo. "Iniciar triagem" MUST NOT mudar o responsável (ADR-0068, ADR-0098).

#### Scenario: Iniciar atendimento sem responsável
- **WHEN** um desenvolvedor inicia o atendimento de um chamado de Desenvolvimento Em triagem e sem responsável
- **THEN** o chamado fica Em andamento, o desenvolvedor passa a ser o responsável e a acompanhar o chamado, e a conversa registra a atribuição

#### Scenario: Iniciar atendimento com responsável
- **WHEN** o líder técnico inicia o atendimento de um chamado Em triagem que já tem Diego como responsável
- **THEN** o chamado fica Em andamento e Diego continua o responsável

#### Scenario: Administrador de fora inicia o atendimento
- **WHEN** o administrador, sem estar no projeto, inicia o atendimento de um chamado sem responsável
- **THEN** o chamado fica Em andamento e continua sem responsável

#### Scenario: Iniciar triagem não assume
- **WHEN** um desenvolvedor inicia a triagem de um chamado Aberto sem responsável
- **THEN** o chamado fica Em triagem e continua sem responsável
