## MODIFIED Requirements

### Requirement: Nome da instalação

O administrador MUST poder trocar o nome exibido da instalação, obrigatório e com até 60 caracteres, sem os espaços das pontas. Enquanto ninguém o troca, o nome MUST ser "GerenWare", o do produto; a instalação que ainda mostrava o nome antigo do produto MUST passar ao novo, e a que escolheu outro nome MUST continuar com ele. O nome MUST aparecer no topo da barra lateral, no painel da página de entrada e no título da aba do navegador, escrito ou, quando é o do produto e não há logo, na logo (ADR-0003, ADR-0009, ADR-0159, ADR-0160).

#### Scenario: Nome padrão
- **WHEN** ninguém trocou o nome da instalação
- **THEN** a barra lateral e a página de entrada mostram a logo GerenWare, e o título da aba mostra "GerenWare"

#### Scenario: Trocar o nome
- **WHEN** o administrador troca o nome exibido e salva
- **THEN** a barra lateral, a página de entrada e o título da aba passam a mostrar o novo nome

#### Scenario: Nome inválido
- **WHEN** o nome fica vazio ou passa de 60 caracteres
- **THEN** o campo mostra o motivo, a API recusa com 400 e nada muda

#### Scenario: Nome antigo do produto
- **WHEN** a instalação ainda mostra "FiscoProjects" e a versão nova é instalada
- **THEN** ela passa a mostrar "GerenWare"

#### Scenario: Nome escolhido continua
- **WHEN** a instalação tem um nome escolhido pelo administrador e a versão nova é instalada
- **THEN** o nome dela continua o mesmo

### Requirement: Aplicativo do Teams

Só o administrador MUST baixar o pacote do aplicativo do Teams da instalação, na seção Microsoft 365 das Configurações gerais. O pacote MUST trazer o nome da instalação, o ícone colorido (o logo ou, sem ele, o símbolo da GerenWare), o ícone de contorno (o símbolo da GerenWare em branco), uma identificação que não muda, o aplicativo do Entra, o endereço das telas, os tipos de aviso na hora e as permissões do próprio aplicativo. A versão MUST subir quando o conteúdo muda (ADR-0003, ADR-0149, ADR-0160).

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

#### Scenario: Ícones da marca no pacote
- **WHEN** o administrador baixa o pacote numa instalação sem logo
- **THEN** o ícone colorido é o símbolo da GerenWare com o quadrado cheio, e o de contorno, o símbolo em branco, os dois desenhados dos arquivos da marca
