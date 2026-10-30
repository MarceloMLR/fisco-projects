# termos-de-aceite Specification

## Purpose

Termos de aceite dos marcos do cronograma: o pedido a uma pessoa do projeto com o termo em PDF, a resposta com o PDF assinado fora do sistema ou a recusa com motivo, o termo assinado anexado pela gestão, o lembrete e o cancelamento, o termo assinado na pasta de termos com o marco atingido, as permissões e a tela (ADR-0035, ADR-0045, ADR-0093, ADR-0095).

## Requirements

### Requirement: Pedido de aceite

O administrador e o gerente MUST pedir a assinatura do termo de um marco do cronograma, com o título (até 120 caracteres), a mensagem opcional (até 2.000), o termo em PDF (só PDF, até o limite dos documentos) e de 1 a 10 signatários, cada um com o nome (até 120) e o e-mail, sem e-mail repetido. O pedido MUST ir para o provedor e ficar Pendente, com quem pediu e quando; todos assinam ao mesmo tempo (ADR-0129).

#### Scenario: Pedir aceite
- **WHEN** a gerente pede a assinatura do marco "Homologação concluída" a dois signatários do cliente, com o título, a mensagem e o termo em PDF
- **THEN** o pedido aparece Pendente na aba Termos de aceite, com os dois signatários pendentes, e o provedor recebe o termo com os dois

#### Scenario: Pedido sem marco do projeto
- **WHEN** a gerente pede a assinatura de uma tarefa ou de um marco de outro projeto
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Termo que não é PDF
- **WHEN** a gerente envia um `.docx` como termo a assinar
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Pedido para quem não está no projeto
- **WHEN** a gerente pede a assinatura a uma pessoa do cliente, que não está no projeto nem no sistema, com o nome e o e-mail
- **THEN** o pedido é gravado com ela como signatária, sem criar usuário

#### Scenario: Signatários inválidos
- **WHEN** a gerente pede a assinatura sem signatário, com 11 signatários, com um e-mail inválido ou com o mesmo e-mail duas vezes
- **THEN** a API responde 400 no campo dos signatários e nada é gravado

#### Scenario: Provedor recusa o pedido
- **WHEN** o provedor responde com erro ao receber o termo
- **THEN** a API responde 502, nada é gravado, e a tela pede para tentar de novo

### Requirement: Termo assinado

Quando todos os signatários assinam, o PDF assinado devolvido pelo provedor MUST virar um documento Aprovado na pasta de termos do projeto, vinculado ao marco, que não muda de status nem é excluído. O marco MUST ficar atingido no dia da instalação, se ainda não estava. A recusa de um signatário, com o motivo do provedor, MUST recusar o termo, encerrar o documento no provedor e não mudar o marco (ADR-0093, ADR-0095, ADR-0129).

#### Scenario: Termo assinado na pasta de termos
- **WHEN** o último signatário assina o termo
- **THEN** o pedido fica Assinado, a pasta de termos tem o PDF assinado do provedor, Aprovado e vinculado ao marco, e a exclusão dele responde 409

#### Scenario: Marco atingido pelo termo
- **WHEN** o termo do marco "Homologação concluída" fica assinado
- **THEN** o marco fica atingido no dia, e o cronograma mostra o marco atingido a todos que o veem

#### Scenario: Recusa de um signatário
- **WHEN** um de dois signatários recusa o termo com o motivo "Falta o relatório de testes"
- **THEN** o pedido fica Recusado, com o signatário e o motivo, o documento é encerrado no provedor, e o marco continua em aberto

### Requirement: Termo anexado pelo gestor

O administrador e o gerente MUST anexar um termo assinado recebido por fora num pedido pendente, que fica Assinado e é cancelado no provedor, ou sem pedido, com o marco, o título e o PDF. O termo anexado MUST seguir as regras do termo assinado e registrar quem anexou; quem assinou fica no próprio PDF (ADR-0095, ADR-0129).

#### Scenario: Anexar em nome da pessoa do pedido
- **WHEN** a gerente anexa o termo assinado num pedido pendente do fluxo anterior, feito a Carla
- **THEN** o pedido fica Assinado, com "anexado por Marina Costa em nome de Carla Mendes" no histórico

#### Scenario: Anexar num pedido pendente
- **WHEN** a gerente anexa o termo assinado num pedido pendente
- **THEN** o pedido fica Assinado, com "anexado por Marina Costa" no histórico, e o documento é cancelado no provedor

#### Scenario: Anexar termo sem pedido
- **WHEN** a gerente anexa um termo assinado do marco "Escopo aprovado"
- **THEN** o termo aparece Assinado, com a gerente como quem anexou, e o marco fica atingido

### Requirement: Lembrar e cancelar

O administrador e o gerente MUST lembrar os signatários de um pedido pendente, o que pede ao provedor o link de novo para quem ainda não assinou e entra no histórico, e MUST cancelar um pedido pendente, o que cancela o documento no provedor e tira o pedido da lista, ficando só na auditoria (ADR-0095, ADR-0129).

#### Scenario: Lembrar a pessoa
- **WHEN** a gerente escolhe "Lembrar" num pedido com um signatário pendente
- **THEN** o provedor manda o link de novo só a ele, e o histórico registra o lembrete

#### Scenario: Cancelar pedido
- **WHEN** a gerente cancela um pedido pendente
- **THEN** o documento é cancelado no provedor, o pedido sai da lista, e a auditoria registra a exclusão

#### Scenario: Cancelar pedido respondido
- **WHEN** a gerente tenta cancelar um pedido assinado
- **THEN** a API responde 409

### Requirement: Provedor de assinatura

O sistema MUST mandar o termo para assinatura, lembrar, cancelar, saber a situação de cada signatário e baixar o PDF assinado pelo provedor escolhido na configuração da instalação, sem regra de negócio diferente por provedor. Sem provedor configurado, o pedido de assinatura MUST responder 409, e a tela MUST oferecer só "Anexar termo assinado" (ADR-0004, ADR-0129).

#### Scenario: Pedido sem provedor configurado
- **WHEN** a instalação não tem provedor de assinatura e a gerente pede a assinatura de um termo
- **THEN** a API responde 409, nada é gravado, e a aba mostra só "Anexar termo assinado"

#### Scenario: Mesmas regras em cada provedor
- **WHEN** os testes do contrato do provedor rodam contra cada implementação
- **THEN** todas mandam, lembram, cancelam, dizem a situação de cada signatário e devolvem o PDF assinado do mesmo jeito

### Requirement: Provedor local

Fora da produção, o provedor local MUST assinar numa página do próprio sistema, sem sessão, aberta pelo link de cada signatário, com o termo, "Assinar" e "Recusar" com o motivo. A gaveta do termo MUST mostrar o link de cada signatário pendente só com o provedor local. A aplicação MUST NOT subir em produção com o provedor local (ADR-0078, ADR-0129).

#### Scenario: Assinar pela página local
- **WHEN** um signatário abre o link do provedor local e escolhe "Assinar"
- **THEN** a página confirma a assinatura, e a gaveta do termo mostra o signatário como assinado

#### Scenario: Link local de outro termo ou já usado
- **WHEN** alguém abre um link local inexistente ou de um signatário que já respondeu
- **THEN** a página diz que o link não vale mais, sem mostrar o termo

#### Scenario: Provedor local em produção
- **WHEN** a aplicação sobe em produção com o provedor local configurado
- **THEN** ela não sobe, e o erro diz que o provedor local só vale fora da produção

### Requirement: Situação das assinaturas

O sistema MUST conferir no provedor, em segundo plano e quando o provedor avisa, a situação de cada pedido aberto, e aplicar cada mudança uma vez só: o signatário que assinou, com a data e a hora; o que recusou, com o motivo; e, quando todos assinaram, o termo assinado (ADR-0047, ADR-0129).

#### Scenario: Assinatura parcial
- **WHEN** um de dois signatários assina no provedor
- **THEN** o pedido continua Pendente, a gaveta mostra o signatário que assinou e quando, e o histórico registra a assinatura

#### Scenario: Conferência repetida
- **WHEN** a mesma assinatura chega pelo aviso do provedor e pela conferência em segundo plano
- **THEN** o histórico registra a assinatura uma vez só, e o termo assinado é gravado uma vez só

#### Scenario: Provedor fora do ar
- **WHEN** o provedor não responde durante a conferência
- **THEN** o pedido continua como estava, e a próxima conferência tenta de novo

### Requirement: Dados dos signatários

O sistema MUST guardar o nome e o e-mail de cada signatário só enquanto o pedido está aberto. Com o termo assinado, recusado ou cancelado, o e-mail MUST ser apagado, e o nome MUST ficar no histórico. A auditoria MUST registrar o e-mail como alterado, sem o valor (ADR-0039, ADR-0058, ADR-0129).

#### Scenario: E-mail apagado ao fechar o pedido
- **WHEN** o último signatário assina o termo
- **THEN** a gaveta mostra o nome de cada signatário, sem o e-mail, e nenhuma tabela guarda mais o e-mail deles

#### Scenario: E-mail fora da auditoria
- **WHEN** a gerente pede a assinatura com dois signatários
- **THEN** a auditoria registra os signatários com o e-mail como alterado, sem o endereço

### Requirement: Pedidos anteriores

Os termos assinados e recusados do fluxo anterior MUST continuar como estavam, com a pessoa do projeto a quem foram pedidos. Os pedidos pendentes do fluxo anterior MUST ficar na lista, sem provedor, e a gestão MUST só cancelá-los ou anexar o termo assinado; o lembrete MUST responder 409 (ADR-0095, ADR-0129).

#### Scenario: Pedido anterior pendente
- **WHEN** a gerente abre um pedido pendente feito antes da assinatura pelo provedor
- **THEN** a gaveta mostra a pessoa do pedido, "Anexar termo assinado" e "Cancelar pedido", sem "Lembrar"

### Requirement: Termos na exclusão do projeto

A exclusão do projeto MUST cancelar no provedor os documentos dos pedidos abertos e pedir a exclusão dos documentos do projeto que o provedor guarda, quando ele permite, antes de apagar os termos; uma falha no provedor MUST NOT impedir a exclusão e MUST ir para o log, sem dados pessoais (ADR-0111, ADR-0129).

#### Scenario: Excluir projeto com pedido aberto
- **WHEN** o administrador exclui um projeto com um pedido de assinatura pendente
- **THEN** o documento é cancelado no provedor, e o projeto é excluído com os termos

### Requirement: Termos só da gestão

Só o administrador e o gerente do projeto MUST ver os termos, os signatários e o andamento, pedir assinatura, anexar, lembrar e cancelar. As demais pessoas do projeto, em qualquer papel, MUST receber 403, sem ver os termos; quem não está no projeto recebe 404; e o projeto concluído ou cancelado MUST ser só leitura, com 409 (ADR-0016, ADR-0129).

#### Scenario: Permissões nos termos por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor pedem os termos, pedem assinatura, anexam, lembram e cancelam
- **THEN** o administrador e o gerente fazem tudo, e os outros recebem 403 em cada ação, sem ver os termos

#### Scenario: Termos de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede os termos
- **THEN** a API responde 404

#### Scenario: Termos de projeto encerrado
- **WHEN** a gerente tenta pedir assinatura num projeto concluído
- **THEN** a API responde 409 e nada é gravado

### Requirement: Aba dos termos da gestão

O projeto MUST ter a aba "Termos de aceite", com a contagem, só para o administrador e o gerente: a lista com o termo e o marco, os signatários ("2 de 3 assinaram"), o pedido e a situação (Pendente, Assinado ou Recusado, por ícone, cor e texto), o filtro por situação, "Pedir assinatura" e "Anexar termo assinado". O pedido MUST ter os signatários em linhas de nome e e-mail, com "Incluir signatário" (ADR-0065, ADR-0066).

#### Scenario: Pedir assinatura pela tela
- **WHEN** a gerente escolhe "Pedir assinatura", escolhe o marco, o título, a mensagem e o PDF, inclui dois signatários e confirma
- **THEN** o pedido aparece Pendente na lista, com "0 de 2 assinaram"

#### Scenario: Aba só da gestão
- **WHEN** o líder técnico abre o projeto
- **THEN** a aba Termos de aceite não aparece

### Requirement: Gaveta do termo com os signatários

A gaveta de um termo (ADR-0081) MUST mostrar o marco, o pedido, a mensagem, o termo a assinar, cada signatário com a situação (Pendente, Assinou em, Recusou com o motivo), o e-mail só enquanto o pedido está aberto, o termo assinado, o histórico e "Lembrar", "Anexar termo assinado" e "Cancelar pedido" no pedido pendente; o link dos avisos MUST abrir a gaveta do termo.

#### Scenario: Signatários na gaveta
- **WHEN** a gerente abre um pedido com um signatário que assinou e outro pendente
- **THEN** a gaveta mostra o primeiro com "Assinou em" a data e a hora, e o segundo Pendente, com o e-mail

#### Scenario: Lembrar e cancelar pela tela
- **WHEN** a gerente abre um pedido pendente e escolhe "Lembrar" e, depois, "Cancelar pedido"
- **THEN** o histórico mostra o lembrete, e o pedido cancelado sai da lista

### Requirement: Assinatura pela Autentique

Com o provedor `Autentique`, o sistema MUST mandar o termo à Autentique pela API dela, com a chave da instalação e cada signatário pelo nome e pelo e-mail; o signatário recebe o link por e-mail da Autentique, e o FiscoProjects MUST NOT mandar e-mail a ele. O cancelamento MUST bloquear o documento na Autentique. A chave e o e-mail dos signatários MUST NOT aparecer nos logs nem nos erros (ADR-0057, ADR-0129, ADR-0130).

#### Scenario: Pedido pela Autentique
- **WHEN** a gerente pede a assinatura de um termo a dois signatários com a Autentique configurada
- **THEN** a Autentique recebe o termo com o título, a mensagem e os dois signatários por e-mail, com a recusa permitida, o pedido fica Pendente com o documento da Autentique, e o FiscoProjects não manda e-mail

#### Scenario: Termo assinado pela Autentique
- **WHEN** os dois signatários assinam na Autentique e a conferência roda
- **THEN** o PDF assinado baixado da Autentique vira o documento Aprovado da pasta de termos, e o marco fica atingido

#### Scenario: Cancelar na Autentique
- **WHEN** a gerente cancela um pedido pendente na Autentique
- **THEN** o documento fica bloqueado na Autentique, e o pedido sai da lista

#### Scenario: Falha da Autentique sem dados pessoais
- **WHEN** a Autentique recusa a chave, limita as chamadas ou fica fora do ar
- **THEN** a API responde 502 ao pedido, nada é gravado, e o erro não traz a chave nem o e-mail dos signatários

### Requirement: Sandbox da Autentique

Fora da produção, a instalação MAY criar os documentos no sandbox da Autentique, de teste, sem custo e sem validade. A aplicação MUST NOT subir em produção com o sandbox ligado, nem com a Autentique sem a chave (ADR-0129, ADR-0130).

#### Scenario: Sandbox fora da produção
- **WHEN** a gerente pede uma assinatura num ambiente com o sandbox ligado
- **THEN** a Autentique recebe o documento como de teste

#### Scenario: Sandbox em produção
- **WHEN** a aplicação sobe em produção com o sandbox da Autentique ligado
- **THEN** ela não sobe, e o erro diz que o sandbox só vale fora da produção

#### Scenario: Autentique sem a chave
- **WHEN** a aplicação sobe com o provedor `Autentique` e sem a chave
- **THEN** ela não sobe, e o erro diz qual configuração falta

### Requirement: Aviso do provedor

O sistema MUST receber os avisos do provedor num endereço público, sem sessão, conferidos pela assinatura do aviso com o segredo da instalação, e MUST responder 200 sem esperar a conferência. O aviso válido MUST só pôr na fila a conferência do pedido do documento: a mudança vem da situação que o provedor dá. Com a assinatura errada, MUST responder 401 sem mudar nada; sem o segredo do aviso, 404 (ADR-0047, ADR-0130).

#### Scenario: Aviso de assinatura
- **WHEN** a Autentique avisa, com a assinatura certa, que os signatários de um pedido aberto assinaram
- **THEN** o endereço responde 200, e o pedido fica Assinado logo depois, sem esperar a conferência em segundo plano

#### Scenario: Aviso forjado
- **WHEN** alguém manda um aviso sem a assinatura certa
- **THEN** o endereço responde 401, e nenhum pedido muda

#### Scenario: Aviso de documento de fora
- **WHEN** chega um aviso válido de um documento que não é de nenhum pedido aberto
- **THEN** o endereço responde 200, e nada muda

#### Scenario: Aviso sem segredo configurado
- **WHEN** a instalação não tem o segredo do aviso, ou o provedor não avisa
- **THEN** o endereço responde 404

### Requirement: Signatários da demonstração no provedor local

Fora da produção, com domínios da demonstração configurados, um pedido com todos os signatários nesses domínios MUST ir para o provedor local, com o link de cada um na gaveta, e os outros pedidos MUST ir para o provedor da instalação. A aplicação MUST NOT subir em produção com domínios da demonstração (ADR-0123, ADR-0129, ADR-0130).

#### Scenario: Pedido aos signatários da demonstração
- **WHEN** a gerente pede a assinatura a signatários só dos domínios da demonstração, com a Autentique configurada
- **THEN** o pedido fica no provedor local, a gaveta mostra o link de cada signatário, e nada vai à Autentique

#### Scenario: Pedido com signatário de fora da demonstração
- **WHEN** um dos signatários do pedido não é de um domínio da demonstração
- **THEN** o pedido inteiro vai à Autentique

#### Scenario: Domínios da demonstração em produção
- **WHEN** a aplicação sobe em produção com domínios da demonstração para a assinatura
- **THEN** ela não sobe, e o erro diz que eles só valem fora da produção
