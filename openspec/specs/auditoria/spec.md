# auditoria Specification

## Purpose

Define o que a auditoria registra automaticamente e como eventos de segurança, o que nunca entra nela, quem pode consultá-la e o que a tela de Auditoria da Administração mostra (ADR-0039, ADR-0033, ADR-0046, ADR-0075).

## Requirements

### Requirement: Registro automático das alterações

Toda criação, alteração e exclusão salva nos dados de qualquer módulo MUST gerar um registro de auditoria com o autor, a data e hora, o tipo e a identificação do item e, para cada campo, o valor antes e depois. O autor MUST ser a pessoa da sessão que fez a alteração, ou Sistema quando não há sessão (ADR-0039, ADR-0075).

#### Scenario: Criar um item
- **WHEN** um item é criado
- **THEN** a auditoria registra a criação, com o item, o autor e o valor de cada campo depois

#### Scenario: Alterar um item
- **WHEN** campos de um item são alterados
- **THEN** a auditoria registra a alteração só com os campos que mudaram, cada um com o valor antes e depois

#### Scenario: Excluir um item
- **WHEN** um item é excluído
- **THEN** a auditoria registra a exclusão, com o valor de cada campo antes

#### Scenario: Autor da alteração
- **WHEN** uma pessoa com a sessão aberta altera um item
- **THEN** o registro tem essa pessoa como autor

#### Scenario: Alteração sem sessão
- **WHEN** um item é alterado sem pessoa na sessão, como o usuário criado na primeira entrada
- **THEN** o registro tem Sistema como autor

### Requirement: Registro junto com a alteração

O registro de auditoria MUST ser gravado junto com a alteração: se a alteração não for salva, não fica registro; se o registro não puder ser gravado, a alteração não é salva (ADR-0039).

#### Scenario: Alteração que não é salva
- **WHEN** a gravação de uma alteração falha
- **THEN** nenhum registro de auditoria é gravado

#### Scenario: Registro que não é gravado
- **WHEN** o registro de auditoria de uma alteração não pode ser gravado
- **THEN** a alteração também não é salva

### Requirement: Campos técnicos fora da auditoria

Campos técnicos marcados como fora da auditoria MUST NOT aparecer nos registros, e uma alteração só nesses campos MUST NOT gerar registro (ADR-0039).

#### Scenario: Campo técnico fora da auditoria
- **WHEN** muda só um campo técnico marcado como fora da auditoria, como o último acesso de um usuário
- **THEN** nenhum registro é gravado

### Requirement: Valores secretos fora da auditoria

Um campo marcado como secreto, como a senha de um acesso, MUST NOT ter o valor gravado na auditoria, nem cifrado: o registro MUST mostrar só que o campo foi alterado (ADR-0033, ADR-0039).

#### Scenario: Senha alterada
- **WHEN** um campo secreto é criado ou alterado
- **THEN** o registro indica que o campo foi alterado, sem o valor antes nem o valor depois

### Requirement: Eventos de segurança

O sistema MUST registrar na auditoria os eventos de segurança: a entrada de uma pessoa, com a forma de entrada; a entrada recusada, com a identidade usada, o motivo e Sistema como autor; e a troca de papel, a desativação e a reativação de um usuário, com quem fez, a pessoa e o valor antes e depois (ADR-0039).

#### Scenario: Entrada registrada
- **WHEN** uma pessoa entra no sistema
- **THEN** a auditoria registra a entrada, com a pessoa como autor e a forma de entrada

#### Scenario: Entrada recusada registrada
- **WHEN** uma entrada é recusada porque a identidade não está no diretório ou a conta está desativada
- **THEN** a auditoria registra a recusa, com Sistema como autor, a identidade usada e o motivo

#### Scenario: Troca de papel registrada
- **WHEN** o administrador troca o papel de uma pessoa
- **THEN** a auditoria registra a troca, com o administrador como autor, a pessoa e o papel antes e depois

#### Scenario: Desativação e reativação registradas
- **WHEN** o administrador desativa e depois reativa uma pessoa
- **THEN** a auditoria registra as duas alterações da situação, com o administrador como autor e a pessoa

### Requirement: Auditoria somente leitura

Ninguém MUST poder alterar nem apagar registros de auditoria pelo sistema: a API MUST NOT ter rota para isso, e a gravação da auditoria MUST só acrescentar registros (ADR-0039).

#### Scenario: Sem rota para alterar ou apagar
- **WHEN** alguém tenta alterar ou apagar um registro de auditoria pela API
- **THEN** a rota não existe e o registro continua igual

#### Scenario: Gravação só acrescenta
- **WHEN** a camada de dados da auditoria recebe a alteração ou a exclusão de um registro existente
- **THEN** a gravação é recusada e o registro continua igual

### Requirement: Auditoria só do administrador

Consultar a auditoria MUST ser permitido ao Administrador e negado, com 403, ao Gerente de projeto e ao Usuário. A seção Auditoria MUST aparecer só na Administração, que só o Administrador vê (ADR-0039, ADR-0046).

#### Scenario: Administrador na auditoria
- **WHEN** o Administrador consulta a auditoria
- **THEN** a consulta é aceita

#### Scenario: Gerente de projeto na auditoria
- **WHEN** o Gerente de projeto tenta consultar a auditoria
- **THEN** a consulta recebe 403

#### Scenario: Usuário na auditoria
- **WHEN** o Usuário tenta consultar a auditoria
- **THEN** a consulta recebe 403

### Requirement: Tela de Auditoria

A seção Auditoria da Administração MUST listar os registros do mais novo para o mais antigo, cada um com a data e a hora, o autor (ou Sistema), o que foi feito e o item, com o total no período e paginação. A lista MUST ter filtros por pessoa, por tipo de item e por período, que começa nos últimos 30 dias. Os detalhes MUST aparecer só ao escolher um registro, na gaveta de detalhes (ADR-0039, ADR-0081).

#### Scenario: Lista da auditoria
- **WHEN** o administrador abre a seção Auditoria
- **THEN** ele vê os registros dos últimos 30 dias, do mais novo para o mais antigo, com data e hora, autor, o que foi feito e o item, e o total no período, sem nenhum detalhe aberto

#### Scenario: Troca de papel na lista
- **WHEN** a lista tem a troca de papel de uma pessoa
- **THEN** o registro mostra "Trocou um papel", o nome da pessoa e o papel antes e depois

#### Scenario: Filtro por pessoa
- **WHEN** o administrador filtra por uma pessoa
- **THEN** a lista mostra só os registros feitos por ela, e o filtro mostra o nome da pessoa inteiro ou, se não couber, terminado em reticências

#### Scenario: Filtro por tipo de item
- **WHEN** o administrador filtra por um tipo de item, como as entradas
- **THEN** a lista mostra só os registros desse tipo

#### Scenario: Filtro por período
- **WHEN** o administrador escolhe um período
- **THEN** a lista mostra só os registros entre o primeiro e o último dia escolhidos, inclusive

#### Scenario: Detalhes de um registro
- **WHEN** o administrador escolhe um registro
- **THEN** a gaveta de detalhes mostra quem fez o quê, quando, o item e a tabela dos campos com o valor antes e depois, sem quebrar palavras no meio, e a linha do registro fica marcada enquanto a gaveta está aberta

### Requirement: Referências a outros itens

Um campo que aponta para outro item, como o cliente de um projeto ou a pessoa incluída num projeto, MUST ser gravado com o identificador e o tipo do item, sem o nome. A tela de Auditoria MUST mostrar o nome atual do item, pedido ao módulo dono do tipo, no campo e no rótulo do registro. Um item que não existe mais MUST aparecer como "—" (ADR-0084).

#### Scenario: Referência pelo nome
- **WHEN** a auditoria tem a troca do cliente de um projeto
- **THEN** a tela mostra o nome do cliente antes e depois, e não o identificador

#### Scenario: Nome atual na referência
- **WHEN** o item de uma referência muda de nome depois do registro
- **THEN** a tela mostra o nome atual

#### Scenario: Item que não existe mais
- **WHEN** o item de uma referência foi excluído
- **THEN** a tela mostra "—" no lugar do nome

### Requirement: Relatório da auditoria

Só o administrador MUST exportar a auditoria, com o período em dias da instalação (até 366 dias) e o tipo e a pessoa opcionais. A API MUST devolver todos os registros do filtro, do mais novo para o mais antigo, até 10.000, e acima disso responder 400 com o total. Toda exportação MUST virar o registro "Exportou a auditoria", com os filtros e a quantidade. Valor secreto MUST NOT sair (ADR-0039, ADR-0046, ADR-0099).

#### Scenario: Exportar a auditoria
- **WHEN** o administrador exporta os registros de entrada dos últimos 30 dias
- **THEN** a API devolve só as entradas do período, da mais nova para a mais antiga, e a auditoria passa a ter o registro da exportação, com o administrador, o período, o tipo e a quantidade

#### Scenario: Gerente de projeto não exporta
- **WHEN** o gerente de projeto tenta exportar a auditoria
- **THEN** a API responde 403 e nada é registrado

#### Scenario: Período inválido para exportar
- **WHEN** o administrador pede um período de mais de 366 dias, ou sem o início
- **THEN** a API responde 400 e nada é registrado

#### Scenario: Registros demais para exportar
- **WHEN** o filtro tem mais registros que o limite
- **THEN** a API responde 400 com o total, e nada é registrado

### Requirement: Relatório pela tela

A tela Auditoria MUST ter "Baixar planilha", que baixa um `.xlsx` com os filtros da tela: a aba Auditoria com a data e a hora no fuso da instalação, quem, o que, o item, os detalhes e as alterações com antes e depois, com os textos da tela, e a aba Filtros. A exportação MUST aparecer na lista como "Exportou a auditoria" (ADR-0099).

#### Scenario: Baixar a planilha
- **WHEN** o administrador escolhe "Baixar planilha" com o filtro de uma pessoa
- **THEN** a tela baixa a planilha com os registros da pessoa, com os mesmos textos da lista, e o filtro na aba Filtros

#### Scenario: Muitos registros para a planilha
- **WHEN** o administrador escolhe "Baixar planilha" num período com registros demais
- **THEN** a tela pede um período menor, com o total

#### Scenario: Exportação na lista
- **WHEN** a lista tem uma exportação da auditoria
- **THEN** o registro mostra "Exportou a auditoria", a quantidade e o período
