## ADDED Requirements

### Requirement: Cores das mensagens

O administrador MUST poder escolher uma cor, em hexadecimal, para as mensagens de cada papel da conversa dos chamados (solicitante, responsável, gerente do projeto, líder técnico, desenvolvimento e funcional), ou voltar ao padrão. Com a cor, nos dois temas, o cartão, o selo, o nome e o avatar do papel MUST segui-la, com os cartões claros no tema claro e escuros no escuro, e o selo, o nome e as iniciais do avatar com 4,5:1. Uma cor fora do formato MUST ser recusada (ADR-0101).

#### Scenario: Cores padrão das mensagens
- **WHEN** ninguém escolheu as cores das mensagens
- **THEN** a conversa usa as cores do design system

#### Scenario: Trocar as cores das mensagens
- **WHEN** o administrador escolhe as cores do responsável e do desenvolvimento e salva
- **THEN** a marca da instalação traz as duas cores, e depois "Restaurar padrão" de todas volta ao design system

#### Scenario: Contraste das mensagens
- **WHEN** a cor de um papel é clara, escura, branca ou preta
- **THEN** o selo e o nome têm 4,5:1 sobre o selo e o cartão, as iniciais do avatar têm 4,5:1, e os cartões continuam claros no tema claro e escuros no escuro

#### Scenario: Cor de mensagem inválida
- **WHEN** o administrador envia uma cor de mensagem fora do formato
- **THEN** a API responde 400 e nada muda

#### Scenario: Cores das mensagens pela tela
- **WHEN** o administrador escolhe a cor do responsável em "Mensagens dos chamados"
- **THEN** a prévia mostra a mensagem do responsável na cor antes de salvar, e depois de salvar a cor vale em todas as telas

#### Scenario: Cor de mensagem fora do formato
- **WHEN** o administrador digita uma cor de mensagem que não é hexadecimal
- **THEN** o campo mostra o erro e nada é enviado

#### Scenario: Cores das mensagens na auditoria
- **WHEN** o administrador troca a cor das mensagens do líder técnico
- **THEN** a auditoria registra "Cor das mensagens do líder técnico" com a cor nova
