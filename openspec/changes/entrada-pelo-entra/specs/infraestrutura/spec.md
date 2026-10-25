## ADDED Requirements

### Requirement: Aplicativo do Entra pronto para a entrada

O Terraform MUST configurar o aplicativo do Entra da instalação para a entrada: o endereço de retorno da aplicação, as declarações de conta convidada e de e-mail, o consentimento do administrador só para `openid`, `profile` e `email`, a atribuição obrigatória, ligada por padrão, e a confiança na identidade gerenciada da aplicação. A aplicação MUST receber o tenant e o cliente do aplicativo e montar o endereço de retorno em HTTPS atrás do App Service (ADR-0005, ADR-0117).

#### Scenario: Aplicativo do Entra para a entrada
- **WHEN** quem opera aplica o Terraform de um ambiente
- **THEN** o aplicativo do Entra aceita a volta só no endereço da aplicação, pede só `openid`, `profile` e `email`, exige atribuição e confia na identidade gerenciada da aplicação, que recebe o tenant e o cliente

## MODIFIED Requirements

### Requirement: Instalação sem senhas

O banco MUST aceitar só identidades do Entra ID, com um grupo de administradores, e a aplicação MUST entrar nele e no Key Vault pela identidade gerenciada, sem senha. O aplicativo do Entra MUST confiar na identidade gerenciada da aplicação (credencial federada), sem segredo nem certificado do aplicativo. O Terraform MUST NOT criar segredos, certificados ou chaves: eles são criados à mão no Key Vault e lidos pela aplicação por referência. Os valores pessoais da instalação e o estado MUST ficar fora do repositório (ADR-0052, ADR-0053, ADR-0114, ADR-0117).

#### Scenario: Banco sem senha
- **WHEN** a aplicação se conecta ao banco
- **THEN** ela entra com a identidade gerenciada, e o servidor não tem usuário nem senha do SQL

#### Scenario: Segredos fora do Terraform
- **WHEN** o código do Terraform e os parâmetros dos ambientes são conferidos
- **THEN** nenhum cria segredo, certificado ou chave, nenhum guarda os valores pessoais da instalação, e o estado e os valores pessoais são ignorados pelo Git

#### Scenario: Aplicativo do Entra sem segredo
- **WHEN** o aplicativo do Entra é conferido no código do Terraform
- **THEN** a única credencial dele é a confiança na identidade gerenciada da aplicação, sem senha nem certificado
