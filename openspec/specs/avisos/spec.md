# avisos Specification

## Purpose

Avisos do sistema: a fila de e-mails no banco, enviada em segundo plano com novas tentativas e uma implementação local fora da produção, a retenção do conteúdo dos e-mails e o teste de envio pela Administração (ADR-0004, ADR-0036, ADR-0058).

## Requirements

### Requirement: Fila de e-mails

Todo e-mail MUST ser gravado numa fila no banco e enviado em segundo plano, de modo que a ação que o gera não dependa do envio e não falhe quando o envio está indisponível (ADR-0036).

#### Scenario: E-mail na fila com o envio fora do ar
- **WHEN** o administrador pede o teste de envio e o envio está indisponível
- **THEN** a API responde 202, e o e-mail fica na fila, Na fila

### Requirement: Envio em segundo plano

Um processo da aplicação MUST enviar os e-mails da fila, marcando cada um como Enviado. Uma falha MUST ser tentada de novo depois de 1 min, 5 min, 15 min, 1 h e 4 h, e a sexta falha MUST deixar o e-mail como Falhou, com o erro registrado. Um e-mail MUST ser enviado por um processo só de cada vez (ADR-0036).

#### Scenario: Envio e marcação
- **WHEN** o processo encontra um e-mail na fila e o envio funciona
- **THEN** o e-mail sai e fica Enviado, com a hora do envio

#### Scenario: Falha e nova tentativa
- **WHEN** o envio de um e-mail falha
- **THEN** o e-mail continua na fila, com a próxima tentativa 1 minuto depois e o erro registrado

#### Scenario: Falha definitiva
- **WHEN** o envio de um e-mail falha pela sexta vez
- **THEN** o e-mail fica Falhou e não é tentado de novo

### Requirement: Implementação local

Fora da produção, os e-mails MUST ficar gravados como arquivos `.eml`, sem sair para ninguém, e a aplicação MUST não subir em produção com a implementação local (ADR-0004, ADR-0036).

#### Scenario: E-mail gravado sem envio
- **WHEN** o processo envia um e-mail com a implementação local
- **THEN** um arquivo `.eml` com o destinatário, o assunto e os textos fica na pasta configurada

#### Scenario: Implementação local em produção
- **WHEN** a aplicação sobe em produção com `Email:Provider` = `Local`
- **THEN** a aplicação não sobe e diz por quê

### Requirement: Retenção do conteúdo dos e-mails

O conteúdo de um e-mail enviado ou que falhou há mais de `Email:KeepDays` dias (30) MUST ser apagado da fila, ficando só a data e o tipo (ADR-0058).

#### Scenario: Conteúdo apagado depois de 30 dias
- **WHEN** passam 31 dias do envio de um e-mail
- **THEN** a fila guarda só a data e o tipo dele, sem destinatário, assunto nem textos

### Requirement: Teste de envio

O administrador MUST ver nas Configurações gerais a caixa de envio e o resultado do último teste, e MUST poder enviar um e-mail de teste para o próprio endereço. As outras pessoas MUST receber 403 (ADR-0036, ADR-0046).

#### Scenario: Enviar teste pela tela
- **WHEN** o administrador escolhe "Enviar teste" na seção Microsoft 365
- **THEN** a tela avisa que o e-mail está na fila para o endereço dele, e o resultado do teste aparece depois do envio

#### Scenario: Teste só do administrador
- **WHEN** um gerente de projeto pede o teste de envio
- **THEN** a API responde 403
