# arquitetura-modular Specification

## Purpose

Garante que o backend do FiscoProjects seja um monolito modular: cada assunto num módulo, cada módulo em camadas, e as fronteiras entre módulos e camadas verificadas automaticamente, para o sistema não virar um emaranhado difícil de manter (ADR-0047).

## Requirements

### Requirement: Módulos só se conhecem pelos contratos

Cada módulo do backend MUST ser formado por um projeto principal (`FiscoProjects.<Modulo>`) e um projeto de contratos (`FiscoProjects.<Modulo>.Contracts`). Um módulo MUST usar de outro módulo apenas o projeto de contratos; nunca tipos do projeto principal de outro módulo.

#### Scenario: Módulo usa só o contrato de outro módulo
- **WHEN** os testes de arquitetura rodam sobre um módulo que usa apenas tipos do projeto de contratos de outro módulo
- **THEN** a regra de fronteira entre módulos passa

#### Scenario: Módulo usa o interior de outro módulo
- **WHEN** os testes de arquitetura rodam sobre um módulo que usa um tipo do projeto principal de outro módulo
- **THEN** a regra de fronteira entre módulos falha e aponta o tipo que causou a violação

### Requirement: Camadas dependem só das camadas internas

Todo tipo do projeto principal de um módulo MUST ficar numa das camadas `Domain`, `Application`, `Infrastructure` ou `Endpoints`. A camada `Domain` MUST NOT depender de nenhuma outra camada; `Application` MUST depender só de `Domain`; `Infrastructure` e `Endpoints` MUST depender só das camadas internas, nunca uma da outra.

#### Scenario: Módulo que segue as camadas
- **WHEN** os testes de arquitetura rodam sobre um módulo em que cada camada usa apenas as camadas internas
- **THEN** todas as regras de camadas passam

#### Scenario: Domínio usa uma camada externa
- **WHEN** um tipo da camada `Domain` usa um tipo de `Application`, `Infrastructure` ou `Endpoints`
- **THEN** a regra de camadas falha e aponta o tipo que causou a violação

#### Scenario: Aplicação usa infraestrutura
- **WHEN** um tipo da camada `Application` usa um tipo de `Infrastructure` ou `Endpoints`
- **THEN** a regra de camadas falha e aponta o tipo que causou a violação

#### Scenario: Endpoints usam infraestrutura direto
- **WHEN** um tipo da camada `Endpoints` usa um tipo de `Infrastructure`
- **THEN** a regra de camadas falha e aponta o tipo que causou a violação

#### Scenario: Tipo fora das camadas
- **WHEN** um tipo do projeto principal de um módulo fica fora das pastas `Domain`, `Application`, `Infrastructure` e `Endpoints`
- **THEN** a regra de camadas falha e aponta o tipo

### Requirement: Regras de negócio sem tecnologia externa

As camadas `Domain` e `Application` MUST NOT depender de bibliotecas de acesso a banco, de framework web ou de clientes do Microsoft 365 e do Azure; essas dependências ficam em `Infrastructure` e `Endpoints`.

#### Scenario: Domínio usa biblioteca de banco ou web
- **WHEN** um tipo de `Domain` ou de `Application` usa um tipo de biblioteca de banco, de framework web ou de cliente do Microsoft 365
- **THEN** a regra de independência tecnológica falha e aponta o tipo que causou a violação

### Requirement: Contratos e núcleo compartilhado não dependem de módulos

O projeto de contratos de um módulo e o núcleo compartilhado (`FiscoProjects.SharedKernel`) MUST NOT depender do projeto principal de nenhum módulo. O núcleo compartilhado MUST NOT depender de framework web nem de biblioteca de banco.

#### Scenario: Contrato usa o interior de um módulo
- **WHEN** um tipo de um projeto de contratos usa um tipo do projeto principal de um módulo
- **THEN** a regra de contratos falha e aponta o tipo que causou a violação

#### Scenario: Núcleo compartilhado usa um módulo
- **WHEN** um tipo do núcleo compartilhado usa um tipo de qualquer módulo, de framework web ou de biblioteca de banco
- **THEN** a regra do núcleo compartilhado falha e aponta o tipo que causou a violação

### Requirement: Regras valem para todo módulo novo

As regras de arquitetura MUST ser aplicadas automaticamente a todo módulo que siga a convenção de nomes, sem que seja preciso alterar os testes ao criar um módulo.

#### Scenario: Novo módulo segue a convenção de nomes
- **WHEN** um assembly chamado `FiscoProjects.<Modulo>` ou `FiscoProjects.<Modulo>.Contracts` passa a fazer parte da aplicação
- **THEN** os testes de arquitetura o identificam como módulo e aplicam a ele todas as regras

#### Scenario: Assemblies que não são módulos
- **WHEN** os testes de arquitetura procuram módulos
- **THEN** o host da API, o núcleo compartilhado e os projetos de teste não são tratados como módulos

### Requirement: Identificadores de entidade em GUID versão 7

Toda entidade MUST receber, ao ser criada, um identificador único no formato GUID versão 7 (ADR-0061).

#### Scenario: Entidade nova
- **WHEN** uma entidade é criada
- **THEN** ela recebe um identificador GUID na versão 7

#### Scenario: Duas entidades
- **WHEN** duas entidades são criadas
- **THEN** os identificadores das duas são diferentes
