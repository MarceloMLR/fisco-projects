# Spec Delta

## MODIFIED Requirements

### Requirement: Regras de negócio sem tecnologia externa

As camadas `Domain` e `Application` MUST NOT depender de bibliotecas de acesso a banco, de framework web, de clientes do Microsoft 365 e do Azure nem do projeto compartilhado de persistência (`FiscoProjects.Persistence`); essas dependências ficam em `Infrastructure` e `Endpoints`.

#### Scenario: Domínio usa biblioteca de banco ou web
- **WHEN** um tipo de `Domain` ou de `Application` usa um tipo de biblioteca de banco, de framework web ou de cliente do Microsoft 365
- **THEN** a regra de independência tecnológica falha e aponta o tipo que causou a violação

#### Scenario: Domínio usa a persistência compartilhada
- **WHEN** um tipo de `Domain` ou de `Application` usa um tipo de `FiscoProjects.Persistence`
- **THEN** a regra de independência tecnológica falha e aponta o tipo que causou a violação

### Requirement: Contratos e núcleo compartilhado não dependem de módulos

O projeto de contratos de um módulo, o núcleo compartilhado (`FiscoProjects.SharedKernel`) e o projeto compartilhado de persistência (`FiscoProjects.Persistence`) MUST NOT depender do projeto principal de nenhum módulo. O núcleo compartilhado MUST NOT depender de framework web nem de biblioteca de banco.

#### Scenario: Contrato usa o interior de um módulo
- **WHEN** um tipo de um projeto de contratos usa um tipo do projeto principal de um módulo
- **THEN** a regra de contratos falha e aponta o tipo que causou a violação

#### Scenario: Núcleo compartilhado usa um módulo
- **WHEN** um tipo do núcleo compartilhado usa um tipo de qualquer módulo, de framework web ou de biblioteca de banco
- **THEN** a regra do núcleo compartilhado falha e aponta o tipo que causou a violação

#### Scenario: Persistência compartilhada usa um módulo
- **WHEN** um tipo de `FiscoProjects.Persistence` usa um tipo de qualquer módulo
- **THEN** a regra da persistência compartilhada falha e aponta o tipo que causou a violação

### Requirement: Regras valem para todo módulo novo

As regras de arquitetura MUST ser aplicadas automaticamente a todo módulo que siga a convenção de nomes, sem que seja preciso alterar os testes ao criar um módulo.

#### Scenario: Novo módulo segue a convenção de nomes
- **WHEN** um assembly chamado `FiscoProjects.<Modulo>` ou `FiscoProjects.<Modulo>.Contracts` passa a fazer parte da aplicação
- **THEN** os testes de arquitetura o identificam como módulo e aplicam a ele todas as regras

#### Scenario: Assemblies que não são módulos
- **WHEN** os testes de arquitetura procuram módulos
- **THEN** o host da API, o núcleo compartilhado, a persistência compartilhada e os projetos de teste não são tratados como módulos
