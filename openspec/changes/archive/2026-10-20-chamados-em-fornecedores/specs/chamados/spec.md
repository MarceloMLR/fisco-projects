## ADDED Requirements

### Requirement: Fornecedores

O administrador MUST incluir, editar, desativar e reativar fornecedores, com nome único e o endereço do portal de suporte opcional (http ou https). Fornecedor inativo MUST não aparecer para novos registros. Nenhum fornecedor fica fixo no código, e as mudanças entram na auditoria (ADR-0002, ADR-0028, ADR-0046).

#### Scenario: Incluir fornecedor
- **WHEN** o administrador inclui o fornecedor "Emissor Exemplo" com o portal de suporte
- **THEN** o fornecedor aparece ativo na lista e nas opções de registro

#### Scenario: Nome repetido
- **WHEN** o administrador inclui um fornecedor com o nome de outro
- **THEN** a API responde 409 e nada é gravado

#### Scenario: Fornecedor inativo
- **WHEN** o administrador desativa um fornecedor
- **THEN** ele some das opções de novos registros, e os registros que já existem continuam com o nome dele

#### Scenario: Fornecedores só do administrador
- **WHEN** um gerente de projeto tenta incluir um fornecedor
- **THEN** a API responde 403

### Requirement: Chamado no fornecedor

Quem atende o chamado MUST registrar nele um ou mais chamados abertos em fornecedores ativos, inclusive de fornecedores diferentes, com o número, a data de abertura, o status (Aberto no fornecedor, Em análise, Aguardando nosso retorno, Resolvido ou Fechado), o link e a observação, e MUST atualizar o status com uma anotação. O registro e cada atualização MUST entrar na conversa como eventos. O nosso prazo continua correndo. Os outros MUST receber 403 (ADR-0028, ADR-0044).

#### Scenario: Registrar no fornecedor
- **WHEN** o desenvolvedor responsável registra o chamado FX-20931 aberto no fornecedor "Emissor Exemplo"
- **THEN** o chamado mostra o registro, e a conversa mostra o evento

#### Scenario: Fornecedores diferentes
- **WHEN** quem atende registra no mesmo chamado um chamado em outro fornecedor
- **THEN** o chamado mostra os dois registros

#### Scenario: Atualizar o status no fornecedor
- **WHEN** quem atende muda o status do registro para "Aguardando nosso retorno", com uma anotação
- **THEN** o registro mostra o novo status, e a conversa mostra o evento com a anotação

#### Scenario: Solicitante não registra no fornecedor
- **WHEN** o solicitante tenta registrar um chamado no fornecedor
- **THEN** a API responde 403 e nada é gravado

### Requirement: Fornecedor parado

Um registro ainda em aberto no fornecedor e sem atualização há `Tickets:VendorStaleBusinessDays` dias úteis (5) do calendário da instalação MUST ser marcado com os dias úteis sem retorno (ADR-0028).

#### Scenario: Sem retorno do fornecedor
- **WHEN** um registro em análise fica 6 dias úteis sem atualização
- **THEN** o chamado mostra "Sem retorno do fornecedor há 6 dias úteis"

### Requirement: Fornecedores pela tela

O painel do chamado MUST ter a seção "No fornecedor", com cada registro (fornecedor, número, status por ícone, cor e texto, e o destaque de parado), "Registrar" e "Atualizar" para quem atende. A página Chamados MUST ter a visão "Com fornecedor", com a contagem, e a lista MUST mostrar o fornecedor de cada chamado. A Administração MUST ter "Fornecedores" (ADR-0028, ADR-0065).

#### Scenario: Registrar pela tela
- **WHEN** quem atende escolhe "Registrar" em "No fornecedor", preenche o fornecedor e o número e confirma
- **THEN** a seção mostra o registro, e a conversa, o evento

#### Scenario: Com fornecedor
- **WHEN** alguém escolhe a visão "Com fornecedor"
- **THEN** a lista traz só os chamados com registro em fornecedor

#### Scenario: Fornecedores na Administração
- **WHEN** o administrador inclui um fornecedor em "Fornecedores"
- **THEN** a lista mostra o fornecedor ativo, com o portal
