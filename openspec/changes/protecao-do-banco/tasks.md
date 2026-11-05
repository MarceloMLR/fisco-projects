# Tasks

## 1. Trava do banco

- [ ] 1.1 `azurerm_management_lock` `CanNotDelete` no banco, no módulo `database`, com a variável `delete_lock` e a `database_delete_lock` da raiz (`true` por padrão); conferido pelos testes "Banco travado por padrão" e "Destroy com a trava" do `InfrastructureTests`.
- [ ] 1.2 Num servidor descartável do `dev` (grupo, servidor e banco Basic criados e apagados no mesmo dia), confirmar que o Azure recusa apagar o banco, o servidor e o grupo com a trava no banco ("Exclusão por fora do Terraform recusada") e que apaga um banco irmão sem trava ("Banco de teste do roteiro sem trava"); se o servidor não ficar protegido, levar a trava também para o servidor e ajustar o roteiro.
- [ ] 1.3 `terraform plan` e `apply` no `dev`, com a aprovação de Marcelo, só com a trava nova.

## 2. Roteiro e guia

- [ ] 2.1 `docs/restauracao.md`: a trava no "O que é guardado e onde", o `az lock delete` antes da troca do Cenário 2 e a trava de volta pelo `terraform apply`.
- [ ] 2.2 `docs/instalacao.md`: a trava, a permissão que ela pede de quem opera e o `az group delete` recusado enquanto ela existir.
- [ ] 2.3 Primeiro teste de restauração no `dev`, pelo roteiro do teste trimestral, registrado na tabela do roteiro (com a aprovação de Marcelo para criar e apagar o banco de teste).

## 3. Decisão e conferência

- [ ] 3.1 ADR-0154 como proposta, no índice, e o roadmap com a Operação-4.1.
- [ ] 3.2 `bash scripts/ci/infra.sh`, `dotnet test tests/FiscoProjects.Pipeline.Tests` e `openspec validate --all --strict` sem falhas.
