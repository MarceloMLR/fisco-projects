# Spec Delta

## Purpose

Define o cadastro de clientes da instalação, escolhidos como cliente opcional dos projetos: os dados e as regras de cada cliente, quem lista, inclui e altera, a tela Clientes da Administração e a auditoria dessas mudanças (ADR-0015, ADR-0043, ADR-0046).

## ADDED Requirements

### Requirement: Quem cuida dos clientes

O administrador e o gerente de projeto MUST poder listar e incluir clientes, porque o gerente inclui um cliente novo na criação do projeto. Só o administrador MUST poder alterar, inativar, reativar, excluir e trocar o logo. O usuário MUST receber 403 em tudo, e quem não tem sessão, 401. O logo MUST abrir para qualquer pessoa com sessão (ADR-0043, ADR-0046).

#### Scenario: Administrador cuida dos clientes
- **WHEN** o administrador lista, inclui, altera, inativa, reativa, exclui e troca o logo de clientes
- **THEN** todas as ações são aceitas

#### Scenario: Gerente de projeto inclui um cliente
- **WHEN** um gerente de projeto lista os clientes e inclui um cliente novo
- **THEN** as duas ações são aceitas

#### Scenario: Gerente de projeto não altera clientes
- **WHEN** um gerente de projeto pede para alterar, inativar, reativar, excluir ou trocar o logo de um cliente
- **THEN** a API responde 403 e nada muda

#### Scenario: Usuário não cuida dos clientes
- **WHEN** um usuário pede para listar, incluir ou alterar clientes
- **THEN** a API responde 403 e nada muda

#### Scenario: Clientes sem sessão
- **WHEN** alguém sem sessão pede para listar, incluir ou alterar clientes, ou para abrir um logo
- **THEN** a API responde 401 e nada muda

### Requirement: Dados do cliente

Um cliente MUST ter nome obrigatório de até 150 caracteres, sem os espaços das pontas, e pode ter CNPJ, logo e observação de até 500 caracteres. O cliente MUST começar ativo. Nome vazio ou longo e observação longa MUST ser recusados, com o motivo no campo (ADR-0015).

#### Scenario: Incluir cliente
- **WHEN** o administrador inclui "Alfa Distribuidora", com CNPJ e observação
- **THEN** o cliente aparece ativo na lista, com o CNPJ formatado

#### Scenario: Cliente só com o nome
- **WHEN** o administrador inclui um cliente sem CNPJ, logo nem observação
- **THEN** o cliente é aceito

#### Scenario: Cliente inválido
- **WHEN** alguém tenta incluir um cliente sem nome, com nome de mais de 150 caracteres ou observação de mais de 500
- **THEN** o campo mostra o motivo, a API recusa com 400 e nada é incluído

### Requirement: CNPJ do cliente

O CNPJ MUST ser aceito com ou sem pontuação, em letras maiúsculas ou minúsculas, e guardado só com os 14 caracteres. Ele MUST ter os dígitos verificadores corretos, valendo também o CNPJ alfanumérico (IN RFB nº 2.229/2024). Um CNPJ já usado por outro cliente, ativo ou inativo, MUST ser recusado, com o nome do cliente que já o usa (ADR-0015).

#### Scenario: CNPJ com e sem pontuação
- **WHEN** o administrador digita "11.222.333/0001-81" ou "11222333000181"
- **THEN** o CNPJ é aceito e aparece como 11.222.333/0001-81

#### Scenario: CNPJ alfanumérico
- **WHEN** o administrador digita "12.abc.345/01de-35"
- **THEN** o CNPJ é aceito e aparece como 12.ABC.345/01DE-35

#### Scenario: CNPJ inválido
- **WHEN** o CNPJ tem um dígito verificador errado, menos de 14 caracteres ou todos os algarismos iguais
- **THEN** o campo mostra o motivo, a API recusa com 400 e nada muda

#### Scenario: CNPJ repetido
- **WHEN** o administrador usa o CNPJ de outro cliente, mesmo inativo
- **THEN** a tela diz que já existe um cliente com esse CNPJ, com o nome dele, a API recusa com 409 e nada muda

### Requirement: Logo do cliente

O administrador MUST poder enviar, trocar e remover o logo de um cliente, um PNG ou JPG de até 1 MB, conferido pelo conteúdo. Outro formato e arquivo acima de 1 MB MUST ser recusados com o motivo. Sem logo, o cliente aparece com as iniciais do nome (ADR-0015, ADR-0030).

#### Scenario: Enviar o logo do cliente
- **WHEN** o administrador envia um PNG de logo para o cliente
- **THEN** a lista mostra o logo no lugar das iniciais

#### Scenario: Logo do cliente recusado
- **WHEN** o administrador envia um SVG, um arquivo acima de 1 MB ou um arquivo que não é PNG nem JPG
- **THEN** o envio é recusado com o motivo e o logo anterior continua

#### Scenario: Remover o logo do cliente
- **WHEN** o administrador remove o logo do cliente
- **THEN** a lista volta a mostrar as iniciais

### Requirement: Situação e exclusão do cliente

O administrador MUST poder inativar e reativar um cliente; um cliente inativo continua na lista, apagado. O administrador MUST poder excluir um cliente, depois de confirmar, e o logo dele sai do armazenamento (ADR-0015).

#### Scenario: Inativar e reativar cliente
- **WHEN** o administrador inativa um cliente e depois o reativa
- **THEN** o cliente aparece como Inativo e depois como Ativo

#### Scenario: Excluir cliente
- **WHEN** o administrador exclui um cliente e confirma
- **THEN** o cliente sai da lista e o logo dele é apagado

#### Scenario: Cliente que não existe
- **WHEN** alguém pede para alterar ou excluir um cliente que não existe
- **THEN** a API responde 404

### Requirement: Tela Clientes

A seção Cadastros da coluna da Administração MUST ter Clientes, com o total, e a lista em ordem alfabética: o logo ou as iniciais, o nome, o CNPJ formatado, a situação e a observação. A lista MUST ter busca por nome ou CNPJ, sem diferenciar maiúsculas e acentos, o filtro Todos, Ativos e Inativos e o rodapé com o total e os ativos (canvas AdminClientes).

#### Scenario: Clientes na Administração
- **WHEN** o administrador escolhe Clientes na coluna da Administração
- **THEN** a tela mostra os clientes em ordem alfabética, com os inativos apagados, e o rodapé com o total e os ativos

#### Scenario: Buscar cliente
- **WHEN** o administrador busca por "alfa" ou por parte do CNPJ, com ou sem pontuação
- **THEN** a lista mostra só os clientes que combinam

#### Scenario: Filtrar por situação
- **WHEN** o administrador escolhe Inativos
- **THEN** a lista mostra só os clientes inativos

#### Scenario: Novo cliente pela tela
- **WHEN** o administrador escolhe "Novo cliente", preenche os dados, escolhe um logo e escolhe "Incluir cliente"
- **THEN** o diálogo fecha e o cliente aparece na lista, com o logo

#### Scenario: Editar cliente pela tela
- **WHEN** o administrador escolhe Editar no menu de um cliente, troca a observação e salva
- **THEN** a lista mostra a nova observação

#### Scenario: Excluir cliente pela tela
- **WHEN** o administrador escolhe Excluir no menu de um cliente
- **THEN** a tela pede confirmação, dizendo que não dá para desfazer, e só exclui depois de "Excluir cliente"

### Requirement: Mudanças dos clientes na auditoria

Cada mudança num cliente MUST entrar na auditoria, com quem fez, quando e os campos antes e depois: "Incluiu um cliente", "Alterou um cliente", "Inativou um cliente", "Reativou um cliente" e "Excluiu um cliente". O logo aparece pelo nome do arquivo enviado. A tela de Auditoria MUST ter o tipo Clientes no filtro (ADR-0039).

#### Scenario: Cliente incluído na auditoria
- **WHEN** um gerente de projeto inclui um cliente
- **THEN** a auditoria ganha um registro "Incluiu um cliente", com o gerente como autor e o nome do cliente

#### Scenario: Cliente alterado na auditoria
- **WHEN** o administrador troca o CNPJ e o logo de um cliente
- **THEN** a auditoria ganha registros "Alterou um cliente", com o CNPJ antes e depois e o nome do arquivo do logo

#### Scenario: Cliente inativado na auditoria
- **WHEN** o administrador inativa um cliente
- **THEN** a auditoria ganha um registro "Inativou um cliente", com o nome do cliente
