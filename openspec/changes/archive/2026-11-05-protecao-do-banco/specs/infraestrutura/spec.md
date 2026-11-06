## ADDED Requirements

### Requirement: Banco protegido contra exclusão

O banco de cada ambiente MUST ter, por padrão, a trava do Azure contra exclusão (`CanNotDelete`), criada pelo Terraform, para o Azure recusar apagar o banco, o servidor e o grupo de recursos por qualquer caminho até alguém tirar a trava de propósito; o `terraform destroy` e o roteiro de restauração MUST continuar funcionando (ADR-0059).

#### Scenario: Banco travado por padrão
- **WHEN** um ambiente não muda o padrão de `database_delete_lock`
- **THEN** o Terraform cria a trava `CanNotDelete` no banco

#### Scenario: Exclusão por fora do Terraform recusada
- **WHEN** alguém tenta apagar o banco, o servidor ou o grupo de recursos pelo portal ou pela CLI, com a trava no banco
- **THEN** o Azure recusa a exclusão, e nada é apagado

#### Scenario: Destroy com a trava
- **WHEN** quem opera roda o `terraform destroy` de um ambiente
- **THEN** o Terraform tira a trava antes de apagar o banco

#### Scenario: Banco de teste do roteiro sem trava
- **WHEN** o teste trimestral restaura o banco num banco de teste e o apaga no fim
- **THEN** o banco de teste é apagado sem tirar a trava do banco da instalação
