## ADDED Requirements

### Requirement: Comentários do item

O detalhe de cada tarefa, também a resumo e a subtarefa, e de cada marco MUST ter a seção Comentários, do mais antigo para o mais novo, com o autor, quando e o texto formatado, para todos que veem o projeto. O comentário de um item excluído MUST voltar com o desfazer da exclusão (ADR-0091, ADR-0157).

#### Scenario: Ver os comentários
- **WHEN** um leitor do projeto abre o detalhe de uma tarefa com dois comentários
- **THEN** a seção Comentários mostra os dois, o mais antigo primeiro, com o autor e quando

#### Scenario: Comentário do item excluído
- **WHEN** a gerente exclui uma tarefa com comentários e desfaz a exclusão
- **THEN** a tarefa volta com os comentários

### Requirement: Comentar um item

Todos do projeto menos o Leitor, e o administrador, MUST poder comentar com o texto formatado dos chamados, sem anexos, mencionando só as pessoas do projeto, com o projeto em aberto. Texto vazio e menção a outra pessoa MUST receber 400, o Leitor 403, o projeto encerrado 409 e quem não está no projeto 404 (ADR-0026, ADR-0157).

#### Scenario: Comentar com menção
- **WHEN** a colaboradora comenta numa tarefa mencionando o desenvolvedor
- **THEN** o comentário aparece na seção, com a menção

#### Scenario: Menção fora do projeto
- **WHEN** alguém menciona num comentário uma pessoa que não está no projeto
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Leitor não comenta
- **WHEN** um leitor do projeto abre o detalhe de uma tarefa
- **THEN** ele vê os comentários sem o editor, e a API responde 403 ao comentário dele

#### Scenario: Projeto encerrado sem comentários novos
- **WHEN** alguém comenta numa tarefa de um projeto cancelado
- **THEN** a API responde 409, e a tela mostra os comentários sem o editor

### Requirement: Apagar um comentário

Quem escreveu MUST poder apagar o próprio comentário, e o administrador e o gerente qualquer um, com o projeto em aberto; o comentário apagado sai de vez. As outras pessoas MUST receber 403 e o projeto encerrado 409 (ADR-0157).

#### Scenario: Apagar o próprio comentário
- **WHEN** a colaboradora apaga o comentário que escreveu
- **THEN** o comentário sai da seção

#### Scenario: Gerente apaga qualquer comentário
- **WHEN** a gerente apaga o comentário de um desenvolvedor
- **THEN** o comentário sai da seção

#### Scenario: Apagar o comentário de outra pessoa
- **WHEN** a colaboradora tenta apagar o comentário do desenvolvedor
- **THEN** a API responde 403, e a tela não mostra "Apagar" no comentário dele

### Requirement: Quantidade de comentários

Cada tarefa e marco do cronograma MUST trazer quantos comentários tem, e a Lista e o cartão do Quadro MUST mostrar a quantidade, por ícone e texto, quando há algum (ADR-0155, ADR-0157).

#### Scenario: Contagem na Lista e no Quadro
- **WHEN** uma tarefa tem três comentários
- **THEN** a Lista e o cartão do Quadro mostram "3 comentários" junto da tarefa

### Requirement: Comentários fora da auditoria

Os comentários MUST ficar fora da auditoria, como a conversa dos chamados, e MUST sair com o projeto excluído (ADR-0039, ADR-0041, ADR-0157).

#### Scenario: Comentário fora da auditoria
- **WHEN** alguém comenta e depois apaga o comentário
- **THEN** a auditoria não ganha registro

#### Scenario: Projeto excluído com comentários
- **WHEN** o administrador exclui um projeto com comentários
- **THEN** os comentários saem com ele
