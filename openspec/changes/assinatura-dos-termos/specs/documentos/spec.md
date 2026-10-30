## MODIFIED Requirements

### Requirement: Permissões nos documentos

Todos do projeto e o administrador MUST ver e baixar os documentos. Enviar, vincular e mudar para Rascunho, Em revisão ou Obsoleto MUST ser de todos, menos o leitor; aprovar e excluir, do administrador, do gerente e do líder técnico. A pasta de termos e os termos assinados MUST aparecer só para o administrador e o gerente. Quem não está no projeto recebe 404, e o projeto encerrado MUST ser só leitura, com 409 (ADR-0016, ADR-0043, ADR-0045, ADR-0093, ADR-0129).

#### Scenario: Permissões nos documentos por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor tentam enviar, vincular, mudar para Em revisão, aprovar e excluir
- **THEN** o administrador, o gerente e o líder técnico fazem tudo; o colaborador e o desenvolvedor enviam, vinculam e mudam para Em revisão, e recebem 403 ao aprovar e excluir; o leitor recebe 403 em tudo

#### Scenario: Documentos de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede os documentos ou tenta enviar um
- **THEN** a API responde 404

#### Scenario: Leitor vê e não envia
- **WHEN** o leitor abre a área de documentos e tenta enviar um arquivo
- **THEN** ele vê as pastas e os documentos, e o envio responde 403

#### Scenario: Pasta de termos só da gestão
- **WHEN** o líder técnico abre a área de documentos
- **THEN** a pasta de termos de aceite não aparece para ele

#### Scenario: Pasta de termos de quem assinou
- **WHEN** a colaboradora que assinou um termo do fluxo anterior abre a área de documentos
- **THEN** a pasta de termos não aparece para ela, e o termo dela responde 404

#### Scenario: Projeto encerrado só leitura
- **WHEN** alguém tenta enviar um documento num projeto concluído
- **THEN** a API responde 409 e nada é gravado
