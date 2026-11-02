## ADDED Requirements

### Requirement: Aplicativo do Teams

Só o administrador MUST baixar o pacote do aplicativo do Teams da instalação, na seção Microsoft 365 das Configurações gerais. O pacote MUST trazer o nome da instalação, o ícone (o logo ou, sem ele, o selo na cor principal), uma identificação que não muda, o aplicativo do Entra, o endereço das telas, os tipos de aviso na hora e as permissões do próprio aplicativo. A versão MUST subir quando o conteúdo muda (ADR-0003, ADR-0149).

#### Scenario: Pacote do administrador
- **WHEN** o administrador escolhe "Baixar pacote do Teams"
- **THEN** a tela salva um `.zip` com o manifesto da instalação, o ícone colorido e o ícone de contorno

#### Scenario: Conteúdo do pacote
- **WHEN** o administrador pede o pacote do aplicativo
- **THEN** o manifesto traz a identificação da instalação, o aplicativo do Entra, o endereço das telas na página do Teams, um tipo de aviso para cada aviso na hora e as permissões de avisar e de ler a instalação do aplicativo para a pessoa

#### Scenario: Pacote de outro papel
- **WHEN** um gerente de projeto, um líder técnico ou um usuário pede o pacote do aplicativo
- **THEN** a API responde 403

#### Scenario: Versão nova do pacote
- **WHEN** o administrador troca o logo e pede o pacote de novo
- **THEN** o manifesto vem com uma versão maior que a anterior e a mesma identificação

#### Scenario: Pacote sem o Teams na instalação
- **WHEN** o administrador pede o pacote com o envio pelo Teams desligado na configuração da aplicação
- **THEN** a API responde 409, e a seção diz que o Teams não está disponível nesta instalação

### Requirement: Avisar pelo Teams

As Configurações gerais MUST ter "Avisar pelo Teams", desligado de início, na seção Microsoft 365, gravado na hora e só do administrador. A mudança MUST entrar na auditoria. A opção MUST NOT ser ligada sem o envio pelo Teams na configuração da aplicação (ADR-0039, ADR-0149).

#### Scenario: Ligar os avisos pelo Teams
- **WHEN** o administrador marca "Avisar pelo Teams"
- **THEN** a opção fica gravada, a auditoria registra a mudança e os próximos avisos na hora vão para o Teams de quem tem o Teams no canal

#### Scenario: Ligar sem o Teams na instalação
- **WHEN** o administrador tenta ligar "Avisar pelo Teams" sem o envio pelo Teams na configuração da aplicação
- **THEN** a API responde 400 no campo, e nada muda

#### Scenario: Opção de outro papel
- **WHEN** um gerente de projeto, um líder técnico ou um usuário tenta mudar "Avisar pelo Teams"
- **THEN** a API responde 403, e nada muda
