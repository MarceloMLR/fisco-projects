## ADDED Requirements

### Requirement: Envio pelo Microsoft Graph

Com `Email:Provider` = `Graph`, os e-mails da fila MUST sair pelo `sendMail` do Microsoft Graph a partir da caixa compartilhada de `Email:Mailbox`, com o token do aplicativo, o corpo em HTML e o envio guardado nos itens enviados da caixa. Um envio recusado ou limitado pelo Graph MUST ser falha da tentativa, com o código do Graph no erro e sem o destinatário (ADR-0036, ADR-0123).

#### Scenario: E-mail pela caixa compartilhada
- **WHEN** o administrador pede o teste de envio com o envio pelo Graph
- **THEN** o Graph recebe o e-mail da caixa de envio para o endereço do administrador, com o assunto e o HTML, e o teste fica Enviado

#### Scenario: Caixa sem permissão de envio
- **WHEN** o Graph recusa o envio pela caixa com 403
- **THEN** o e-mail continua na fila para a próxima tentativa, com o erro do Graph sem o endereço do destinatário

#### Scenario: Graph fora do ar
- **WHEN** o Graph responde 503 a um envio
- **THEN** o e-mail continua na fila para a próxima tentativa

### Requirement: Configuração do envio conferida

A aplicação MUST não subir com o envio pelo Graph sem uma caixa de envio válida, nem com domínios locais em produção (ADR-0036, ADR-0123).

#### Scenario: Graph sem caixa de envio
- **WHEN** a aplicação sobe com `Email:Provider` = `Graph` e sem `Email:Mailbox`
- **THEN** a aplicação não sobe e diz que falta a caixa de envio

#### Scenario: Domínios locais em produção
- **WHEN** a aplicação sobe em produção com `Email:Graph:LocalDomains`
- **THEN** a aplicação não sobe e diz por quê

### Requirement: E-mails da demonstração na pasta local

Fora da produção, com o envio pelo Graph, os e-mails para os domínios de `Email:Graph:LocalDomains` MUST ficar como `.eml` na pasta local, sem passar pelo Graph, e os outros MUST sair pelo Graph (ADR-0123).

#### Scenario: Endereço da demonstração na pasta
- **WHEN** a fila envia um e-mail para `marina.costa@empresa.example` com `empresa.example` nos domínios locais
- **THEN** o e-mail fica na pasta local, e o Graph não recebe nada

#### Scenario: Endereço do tenant pelo Graph
- **WHEN** a fila envia um e-mail para uma pessoa de outro domínio com domínios locais configurados
- **THEN** o e-mail sai pelo Graph
