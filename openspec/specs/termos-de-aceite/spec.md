# termos-de-aceite Specification

## Purpose

Termos de aceite dos marcos do cronograma: o pedido a uma pessoa do projeto com o termo em PDF, a resposta com o PDF assinado fora do sistema ou a recusa com motivo, o termo assinado anexado pela gestão, o lembrete e o cancelamento, o termo assinado na pasta de termos com o marco atingido, as permissões e a tela (ADR-0035, ADR-0045, ADR-0093, ADR-0095).

## Requirements

### Requirement: Pedido de aceite

O administrador e o gerente MUST pedir o aceite de um marco do cronograma a uma pessoa do projeto, em qualquer papel, com o título (até 120 caracteres), a mensagem opcional (até 2.000) e o termo em PDF (só PDF, até o limite dos documentos). O pedido MUST ficar Pendente, com quem pediu e quando, e o termo a assinar MUST ficar guardado com o pedido (ADR-0035, ADR-0045, ADR-0095).

#### Scenario: Pedir aceite
- **WHEN** a gerente pede a Carla o aceite do marco "Homologação concluída", com o título, a mensagem e o termo em PDF
- **THEN** o pedido aparece Pendente na aba Termos de aceite, para Carla, com o marco e o termo a assinar

#### Scenario: Pedido sem marco do projeto
- **WHEN** a gerente pede o aceite de uma tarefa ou de um marco de outro projeto
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Termo que não é PDF
- **WHEN** a gerente envia um `.docx` como termo a assinar
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Pedido para quem não está no projeto
- **WHEN** a gerente pede o aceite a quem não está no projeto
- **THEN** a API responde 400 e nada é gravado

### Requirement: Resposta ao pedido

Só a pessoa do pedido MUST responder, uma vez, enquanto o pedido está pendente: enviando o termo assinado em PDF ou recusando com o motivo (até 1.000 caracteres). O termo assinado MUST registrar quem enviou e quando; a recusa, o motivo, quem recusou e quando. Pedido já respondido MUST responder 409 (ADR-0035, ADR-0095).

#### Scenario: Enviar o termo assinado
- **WHEN** Carla envia o PDF assinado do pedido
- **THEN** o pedido fica Assinado, com Carla e a hora no histórico

#### Scenario: Termo assinado só em PDF
- **WHEN** Carla envia uma imagem como termo assinado
- **THEN** a API responde 400 e o pedido continua Pendente

#### Scenario: Recusar com motivo
- **WHEN** Carla recusa o pedido com o motivo "Falta o relatório de testes"
- **THEN** o pedido fica Recusado, com o motivo, e o marco continua em aberto

#### Scenario: Recusa sem motivo
- **WHEN** Carla recusa o pedido sem motivo
- **THEN** a API responde 400 e o pedido continua Pendente

#### Scenario: Só a pessoa do pedido responde
- **WHEN** outra pessoa do projeto, inclusive a gerente, tenta enviar o termo assinado ou recusar pelo caminho da resposta
- **THEN** a API responde 403 ou 404, e o pedido continua Pendente

#### Scenario: Pedido já respondido
- **WHEN** Carla tenta responder de novo um pedido recusado
- **THEN** a API responde 409

### Requirement: Termo assinado

O termo assinado MUST virar um documento Aprovado na pasta de termos do projeto, com o nome do arquivo enviado, vinculado ao marco, que não muda de status nem é excluído. O marco MUST ficar atingido no dia da instalação, se ainda não estava, e o termo recusado MUST NOT mudar o marco (ADR-0035, ADR-0093).

#### Scenario: Termo assinado na pasta de termos
- **WHEN** Carla envia o termo assinado
- **THEN** a pasta de termos tem o documento Aprovado, vinculado ao marco, e a exclusão dele responde 409

#### Scenario: Marco atingido pelo termo
- **WHEN** Carla envia o termo assinado do marco "Homologação concluída"
- **THEN** o marco fica atingido no dia, e o cronograma mostra o marco atingido a todos que o veem

### Requirement: Termo anexado pelo gestor

O administrador e o gerente MUST anexar um termo assinado recebido por fora num pedido pendente, em nome da pessoa do pedido, ou sem pedido, com o marco, o título e a pessoa do projeto responsável. O termo anexado MUST seguir as regras do termo assinado e registrar quem anexou (ADR-0035, ADR-0095).

#### Scenario: Anexar em nome da pessoa do pedido
- **WHEN** a gerente anexa o termo assinado no pedido pendente de Carla
- **THEN** o pedido fica Assinado, com "anexado por Marina Costa em nome de Carla Mendes" no histórico

#### Scenario: Anexar termo sem pedido
- **WHEN** a gerente anexa um termo assinado do marco "Escopo aprovado", com Carla como responsável
- **THEN** o termo aparece Assinado para Carla, com a gerente como quem anexou, e o marco fica atingido

### Requirement: Lembrar e cancelar

O administrador e o gerente MUST lembrar a pessoa de um pedido pendente, o que manda o aviso de novo e entra no histórico, e MUST cancelar um pedido pendente, que sai da lista com o termo a assinar, ficando só na auditoria (ADR-0095).

#### Scenario: Lembrar a pessoa
- **WHEN** a gerente escolhe "Lembrar Carla" num pedido pendente
- **THEN** Carla recebe o e-mail do pedido de novo, e o histórico registra o lembrete

#### Scenario: Cancelar pedido
- **WHEN** a gerente cancela um pedido pendente
- **THEN** o pedido sai da lista, e a auditoria registra a exclusão

#### Scenario: Cancelar pedido respondido
- **WHEN** a gerente tenta cancelar um pedido assinado
- **THEN** a API responde 409

### Requirement: Permissões nos termos

O administrador e o gerente MUST ver todos os termos do projeto e o andamento. A pessoa do pedido MUST ver só os próprios termos e responder aos pendentes, em qualquer papel. As demais pessoas do projeto MUST NOT ver os termos nem os termos a assinar; quem não está no projeto recebe 404; e o projeto concluído ou cancelado MUST ser só leitura, com 409 (ADR-0016, ADR-0045).

#### Scenario: Permissões nos termos por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor pedem os termos, pedem aceite, anexam, lembram e cancelam
- **THEN** o administrador e o gerente fazem tudo; os outros veem só os termos endereçados a eles e recebem 403 nas outras ações

#### Scenario: Leitor responde o próprio pedido
- **WHEN** a gerente pede o aceite ao leitor do projeto, e ele envia o termo assinado
- **THEN** o pedido fica Assinado

#### Scenario: Termos de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede os termos
- **THEN** a API responde 404

#### Scenario: Termos de projeto encerrado
- **WHEN** a pessoa do pedido tenta responder num projeto concluído
- **THEN** a API responde 409 e o pedido continua Pendente

### Requirement: Termos pela tela

O projeto MUST ter a aba "Termos de aceite", com a contagem, para quem vê algum termo, como no canvas ProjetoTermos: a lista com o termo e o marco, para quem, o pedido e a situação (Pendente, Assinado ou Recusado, por ícone, cor e texto), o filtro por situação, "Pedir aceite" e "Anexar termo assinado" para a gestão (ADR-0065, ADR-0066).

#### Scenario: Pedir aceite pela tela
- **WHEN** a gerente escolhe "Pedir aceite", escolhe o marco, a pessoa, o título, a mensagem e o PDF e confirma
- **THEN** o pedido aparece Pendente na lista

#### Scenario: Aba só para quem vê termos
- **WHEN** o líder técnico, sem termo endereçado a ele, abre o projeto
- **THEN** a aba Termos de aceite não aparece

### Requirement: Gaveta do termo

A gaveta de um termo (ADR-0081) MUST mostrar o marco, para quem, o pedido, a mensagem, o termo a assinar, o termo assinado, o motivo da recusa, o histórico e as ações de cada pessoa. A pessoa do pedido MUST responder pela gaveta, também no celular, como no canvas CelularTermo, e o link dos avisos MUST abrir a gaveta do termo.

#### Scenario: Responder pela tela
- **WHEN** Carla abre o pedido pelo link do e-mail, escolhe o PDF assinado e "Enviar termo"
- **THEN** o pedido aparece Assinado, com o termo assinado na gaveta

#### Scenario: Recusar pela tela
- **WHEN** Carla escolhe "Recusar", escreve o motivo e confirma
- **THEN** o pedido aparece Recusado, com o motivo na gaveta

#### Scenario: Lembrar e cancelar pela tela
- **WHEN** a gerente abre um pedido pendente e escolhe "Lembrar Carla" e, depois, "Cancelar pedido"
- **THEN** o histórico mostra o lembrete, e o pedido cancelado sai da lista
