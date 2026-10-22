## ADDED Requirements

### Requirement: Demonstração no Codespaces

Um Codespace criado do repositório MUST se preparar sozinho, com o SDK do .NET e o Node das versões fixadas, o banco local com uma senha só dele, os pacotes e as telas compiladas. Ao ser aberto, MUST subir o banco e a API com as telas na porta 5175, carregar os dados de demonstração que faltam, tornar o endereço público e mostrá-lo com as pessoas para entrar. A chave do Syncfusion MUST vir só de um segredo do Codespaces, nunca do repositório (ADR-0089, ADR-0113).

#### Scenario: Demonstração no Codespaces
- **WHEN** Marcelo cria um Codespace do repositório e o abre
- **THEN** em alguns minutos a API está no ar com os dados de demonstração, e o terminal mostra o endereço público e as pessoas para entrar

#### Scenario: Codespace aberto de novo
- **WHEN** Marcelo abre de novo um Codespace com a demonstração já no ar
- **THEN** o terminal mostra o endereço, sem subir uma segunda API

#### Scenario: Versões do Codespace
- **WHEN** a versão do SDK do .NET, a do Node ou a porta da API mudam
- **THEN** o Codespace usa as mesmas do `global.json`, do `.nvmrc` e do `launchSettings.json`, e a verificação reprova a configuração que ficar diferente
