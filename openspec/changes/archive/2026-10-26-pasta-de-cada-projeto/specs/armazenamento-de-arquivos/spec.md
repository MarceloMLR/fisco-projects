## ADDED Requirements

### Requirement: Renomear pasta

O armazenamento MUST renomear uma pasta mantendo o id dela e os ids de tudo o que está dentro, com o nome ajustado às regras do SharePoint, nas duas implementações. Quando já houver um arquivo ou uma pasta com o nome novo ao lado, nada MUST mudar. A raiz MUST nunca ser renomeada. Na implementação local, os ids entregues antes desta mudança MUST continuar valendo, inclusive dentro de uma pasta renomeada depois (ADR-0079, ADR-0121).

#### Scenario: Renomear pasta
- **WHEN** um módulo renomeia a pasta "ERP-FIN - Implantação" para "ERP-FIN - Implantação do ERP"
- **THEN** a pasta aparece com o nome novo, com o mesmo id, e os arquivos de dentro abrem pelos ids que já tinham, com as versões

#### Scenario: Nome já usado ao lado
- **WHEN** um módulo renomeia uma pasta para o nome de outra pasta que está ao lado
- **THEN** o armazenamento responde que o nome está em uso, e as duas pastas ficam como estavam

#### Scenario: Pasta que não existe
- **WHEN** um módulo renomeia uma pasta que não existe ou que está fora da raiz
- **THEN** o armazenamento responde que a pasta não existe, e nada muda

#### Scenario: Ids antigos da implementação local
- **WHEN** um arquivo foi guardado antes desta mudança e a pasta dele é renomeada depois
- **THEN** o arquivo abre pelo id antigo, e a pasta dele também é achada pelo id antigo

## MODIFIED Requirements

### Requirement: Pastas vazias acima

O armazenamento MUST dizer em que pasta está um arquivo ou uma pasta, para que, ao apagar o que registrou, um módulo apague também as pastas que ficaram vazias acima, uma a uma, até a raiz ou até a pasta indicada por quem apaga, que fica. A raiz, a pasta indicada e as pastas com qualquer conteúdo MUST nunca ser apagadas (ADR-0041, ADR-0111, ADR-0121).

#### Scenario: Pasta de um arquivo
- **WHEN** um módulo pergunta a pasta de um arquivo ou de uma pasta
- **THEN** recebe a pasta onde ele está, nenhuma quando ele está na raiz, e nenhuma quando ele não existe

#### Scenario: Pastas vazias acima
- **WHEN** um módulo apaga o último arquivo de "ERP-FIN - Projeto/Chamados/#104" e pede as pastas vazias
- **THEN** "#104", "Chamados" e "ERP-FIN - Projeto" saem, se nada mais estiver nelas, e a raiz fica

#### Scenario: Até a pasta indicada
- **WHEN** um módulo apaga o último arquivo de "Projetos/ERP-FIN - Projeto/Chamados/#104" e pede as pastas vazias até "ERP-FIN - Projeto"
- **THEN** "#104" e "Chamados" saem, se nada mais estiver nelas, e "ERP-FIN - Projeto" e "Projetos" ficam
