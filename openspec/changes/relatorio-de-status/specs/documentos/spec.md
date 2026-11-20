## MODIFIED Requirements

### Requirement: Pastas padrão

O administrador MUST incluir, renomear, desativar e reativar as pastas padrão, com nomes únicos, na ordem do nome. A migração MUST criar as pastas do MVP (de 01 Gestão do projeto a 06 Termos de aceite, esta marcada como a de termos) e "Relatórios de status", marcada como a dos relatórios e vista de início só pela gestão. Essas duas MUST não ser desativadas. Nenhum nome fica fixo no código, e as mudanças entram na auditoria (ADR-0003, ADR-0031, ADR-0046, ADR-0165).

#### Scenario: Incluir pasta padrão
- **WHEN** o administrador inclui a pasta padrão "07 Treinamentos"
- **THEN** a pasta aparece ativa na lista das pastas padrão, na ordem do nome

#### Scenario: Nome de pasta repetido
- **WHEN** o administrador inclui uma pasta padrão com o nome de outra
- **THEN** a API responde 409 e nada é gravado

#### Scenario: Pasta de termos sempre ativa
- **WHEN** o administrador tenta desativar a pasta de termos de aceite
- **THEN** a API responde 400 e a pasta continua ativa

#### Scenario: Pasta dos relatórios padrão
- **WHEN** a migração roda numa instalação que já tem as pastas padrão
- **THEN** a lista das pastas padrão ganha "Relatórios de status", ativa, marcada como a dos relatórios e vista só pela gestão

#### Scenario: Pasta dos relatórios sempre ativa
- **WHEN** o administrador renomeia a pasta dos relatórios para "08 Status reports" e depois tenta desativá-la
- **THEN** o nome muda, a desativação recebe 400 e a pasta continua ativa

#### Scenario: Pastas padrão só do administrador
- **WHEN** um gerente de projeto tenta incluir uma pasta padrão
- **THEN** a API responde 403

### Requirement: Pastas do projeto

Todo projeto novo MUST receber as pastas padrão ativas no momento da criação, com quem vê cada uma e as pastas de termos e dos relatórios marcadas. Uma mudança nas pastas padrão MUST valer só para os projetos novos. Um projeto criado antes do módulo MUST receber as pastas padrão ativas na primeira vez que a área de documentos é aberta. Um projeto sem a pasta dos relatórios MUST recebê-la, como está a padrão, no primeiro relatório de status (ADR-0030, ADR-0031, ADR-0092, ADR-0148, ADR-0165).

#### Scenario: Projeto novo recebe as pastas padrão
- **WHEN** a gerente cria o projeto ERP-FIN
- **THEN** a área de documentos do projeto mostra as sete pastas padrão, na ordem do nome

#### Scenario: Pasta desativada fica fora dos projetos novos
- **WHEN** o administrador desativa "04 Atas de reunião" e depois a gerente cria um projeto
- **THEN** o projeto novo não tem a pasta "04 Atas de reunião", e os projetos que já existiam continuam com ela

#### Scenario: Projeto novo recebe quem vê as pastas
- **WHEN** o administrador deixou "01 Gestão do projeto" só para o líder técnico e depois a gerente cria um projeto
- **THEN** no projeto novo a pasta "01 Gestão do projeto" é vista pela gestão e pelo líder técnico, e os projetos que já existiam continuam como estavam

#### Scenario: Projeto antigo recebe a pasta dos relatórios
- **WHEN** a gerente gera o primeiro relatório de status de um projeto que tem as pastas de antes da pasta dos relatórios
- **THEN** o projeto ganha a pasta "Relatórios de status", vista só pela gestão, com o relatório dentro, e as outras pastas continuam como estavam
