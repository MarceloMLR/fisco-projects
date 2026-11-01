# dados-pessoais Specification

## Purpose
Define como o sistema trata os dados pessoais pela LGPD: a anonimização definitiva de uma pessoa desativada, que tira o nome e o e-mail dela de todas as tabelas, e o prazo da auditoria, com a limpeza mensal dos registros mais antigos (ADR-0058, ADR-0110).

## Requirements

### Requirement: Anonimizar uma pessoa

O administrador MUST poder anonimizar uma pessoa desativada: o nome MUST virar o identificador "Pessoa removida nº N" (da configuração, em sequência), o e-mail MUST ficar vazio e os vínculos com o provedor de identidade MUST ser apagados. A anonimização MUST ser definitiva: a pessoa não volta a ser ativada nem muda de papel. Pessoa ativa ou já anonimizada MUST ser recusada (ADR-0058, ADR-0110).

#### Scenario: Anonimizar pessoa desativada
- **WHEN** o administrador anonimiza uma pessoa desativada
- **THEN** a pessoa passa a se chamar "Pessoa removida nº 1", sem e-mail nem vínculo, com a situação Anonimizado

#### Scenario: Pessoa ativa não é anonimizada
- **WHEN** o administrador tenta anonimizar uma pessoa ativa
- **THEN** a API recusa com 409, e nada muda

#### Scenario: Anonimização definitiva
- **WHEN** o administrador tenta reativar ou mudar o papel de uma pessoa anonimizada
- **THEN** a API recusa com 409

#### Scenario: Só o administrador anonimiza
- **WHEN** quem não é administrador tenta anonimizar uma pessoa
- **THEN** a API recusa com 403

### Requirement: Pessoa anonimizada em todo o sistema

Na mesma transação da anonimização, o nome, o e-mail e o vínculo da pessoa MUST sair de todas as tabelas: na auditoria, o rótulo vira o identificador e os campos com eles passam a dizer só que mudaram; nas menções das mensagens, o identificador; os e-mails para ela são esquecidos e, nos guardados de outras pessoas, o nome vira o identificador; as notificações e os itens do resumo dela são apagados. Os chamados e as mensagens MUST continuar acessíveis (ADR-0058, ADR-0110).

#### Scenario: Nada da pessoa nas tabelas
- **WHEN** uma pessoa com projetos, chamados, menções, e-mails, notificações e registros na auditoria é anonimizada
- **THEN** nenhuma coluna de texto de nenhuma tabela tem o nome ou o e-mail dela

#### Scenario: Chamados e mensagens acessíveis
- **WHEN** uma pessoa que abriu chamados e foi mencionada é anonimizada
- **THEN** os chamados e as mensagens continuam abertos a quem os via, com o identificador no lugar do nome

#### Scenario: E-mail esperando envio
- **WHEN** a pessoa anonimizada tinha um e-mail esperando envio
- **THEN** o e-mail não sai, fica como Falhou e sem o conteúdo

### Requirement: Registro da anonimização

A anonimização MUST ficar na auditoria com quem fez e quando, e os campos de nome e e-mail MUST aparecer só como alterados, sem o nome da pessoa (ADR-0058, ADR-0110).

#### Scenario: Registro sem o nome
- **WHEN** o administrador anonimiza uma pessoa
- **THEN** a auditoria mostra que ele alterou "Pessoa removida nº 1", com o nome e o e-mail só como alterados

### Requirement: Prazo da auditoria

O administrador MUST escolher nas Configurações gerais por quanto tempo a auditoria é guardada, de 1 a 10 anos, com 5 anos de padrão (ADR-0058, ADR-0110).

#### Scenario: Prazo da auditoria
- **WHEN** o administrador muda "Guardar auditoria por" para 7 anos
- **THEN** a limpeza seguinte usa 7 anos, e um valor fora de 1 a 10 é recusado com a mensagem do campo

### Requirement: Limpeza mensal da auditoria

Uma vez por mês, no fuso da instalação, o sistema MUST apagar os registros da auditoria mais antigos que o prazo e, quando apagar algum, MUST deixar um registro da limpeza, do sistema, com o primeiro dia guardado e quantos apagou (ADR-0058, ADR-0110).

#### Scenario: Limpeza mensal
- **WHEN** chega um mês sem limpeza e há registros mais antigos que o prazo
- **THEN** o sistema apaga só esses registros, uma vez no mês, e registra a limpeza na auditoria

### Requirement: Retenção das ausências

A ausência de uma pessoa MUST ser apagada sozinha, numa limpeza em segundo plano, um ano depois do fim, no fuso da instalação, e MUST ser apagada na anonimização da pessoa (ADR-0058, ADR-0110, ADR-0141).

#### Scenario: Ausência apagada depois de um ano
- **WHEN** a limpeza roda com uma ausência que terminou há mais de um ano e outra que terminou há onze meses
- **THEN** a primeira é apagada e a segunda fica

#### Scenario: Ausências na anonimização
- **WHEN** o administrador anonimiza uma pessoa com ausências
- **THEN** as ausências dela são apagadas na mesma transação
