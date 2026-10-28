## ADDED Requirements

### Requirement: E-mail na escolha de pessoa

Toda lista para escolher uma pessoa MUST mostrar o nome e, embaixo, o e-mail, como a menção: o gerente e o líder técnico do Novo projeto, a inclusão no projeto, o responsável de uma tarefa, o responsável e o solicitante de chamados (inclusive em "Passar pendências"), a pessoa do pedido de aceite e o filtro por pessoa da Auditoria. Quem nunca entrou MUST continuar marcado "Nunca entrou", e o campo MUST mostrar só o nome. As opções de pessoa dos chamados MUST trazer o e-mail (ADR-0027, ADR-0107).

#### Scenario: Pessoas de mesmo nome na inclusão
- **WHEN** o gerente busca "mar" para incluir no projeto e há duas pessoas chamadas Marina Costa
- **THEN** cada uma aparece com o próprio e-mail embaixo do nome

#### Scenario: Responsável com o e-mail
- **WHEN** alguém que atende abre "Atribuir" num chamado
- **THEN** a API traz cada pessoa com o nome e o e-mail, e a lista mostra o e-mail embaixo do nome

#### Scenario: Pessoa do pedido de aceite com o e-mail
- **WHEN** o gerente abre "Pedir aceite"
- **THEN** a lista das pessoas do projeto mostra o e-mail embaixo de cada nome

#### Scenario: Responsável da tarefa com o e-mail
- **WHEN** o gerente escolhe o responsável de uma tarefa no cronograma
- **THEN** cada pessoa do projeto aparece com o e-mail embaixo do nome, e o campo mostra só o nome escolhido
