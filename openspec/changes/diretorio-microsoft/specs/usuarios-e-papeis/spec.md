## ADDED Requirements

### Requirement: Diretório da Microsoft

Com a entrada pelo Entra ligada, a busca de pessoas MUST trazer também os membros ativos do tenant pelo Microsoft Graph, sem convidados nem contas desativadas, e a pessoa incluída num projeto MUST ganhar o vínculo `entra` pelo identificador do objeto, para que a primeira entrada encontre o mesmo usuário. Uma falha do Graph MUST NOT impedir a busca, que segue com os usuários (ADR-0005, ADR-0119).

#### Scenario: Pessoas do diretório da Microsoft
- **WHEN** alguém busca "he" com Helena Duarte e Helga Moura no tenant, Helga desativada, e um convidado chamado Heitor Lins
- **THEN** a resposta traz Helena Duarte como pessoa do diretório, sem Helga nem Heitor

#### Scenario: Pessoa do diretório da Microsoft incluída antes de entrar
- **WHEN** o gerente inclui num projeto uma pessoa do tenant que nunca entrou e, depois, ela entra pelo Entra
- **THEN** a entrada encontra o usuário criado na inclusão, com o papel no projeto

#### Scenario: Diretório da Microsoft fora do ar
- **WHEN** o Graph não responde durante uma busca de pessoas
- **THEN** a busca traz os usuários do FiscoProjects, sem erro
