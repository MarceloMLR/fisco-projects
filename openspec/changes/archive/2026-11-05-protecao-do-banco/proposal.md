## Why

Os backups do ADR-0059 estão no Terraform, mas duas lacunas apareceram ao pensar num cliente em produção na nuvem do fornecedor: apagar o servidor do banco ou o grupo de recursos, por engano ou num script errado, leva junto a restauração dos últimos 14 dias (sobram só os backups mensais), e o teste de restauração do roteiro nunca foi feito. E, com os bancos de vários clientes numa assinatura só, falta decidir uma cópia que sobreviva à perda da assinatura (Operação-4.1, a pedido de Marcelo, fora do roadmap).

## What Changes

- Trava do Azure contra exclusão (`CanNotDelete`) no banco de cada ambiente, pelo Terraform, ligada por padrão (`database_delete_lock`). Com ela, o Azure recusa apagar o banco, o servidor e o grupo de recursos; o `terraform destroy` continua funcionando, porque tira a trava antes.
- O roteiro de restauração passa a tirar a trava antes de trocar o banco (Cenário 2) e diz como pô-la de volta (o próximo `terraform apply`); o teste trimestral não precisa tirá-la, porque o banco de teste não tem trava.
- O primeiro teste de restauração, feito no `dev` e registrado no roteiro.
- O ADR-0154, como proposta, com as opções da cópia do banco fora da assinatura, para a decisão de Marcelo (pede uma segunda assinatura); a implementação dela fica para outra change, se for aceita.
- O guia de instalação e os custos dizem a trava e a permissão que ela pede de quem opera.

## Capabilities

### New Capabilities

### Modified Capabilities
- `infraestrutura`: o banco protegido contra exclusão.

## Impact

- `infra/terraform` (módulo `database`, variável nova, `dev.tfvars` sem mudança), `tests/FiscoProjects.Pipeline.Tests` (`InfrastructureTests`), `docs/restauracao.md`, `docs/instalacao.md`, ADR-0154 e o roadmap.
- No `dev`: um `terraform apply` para criar a trava, um teste da trava num servidor descartável e o teste de restauração, todos com a aprovação de Marcelo no chat.
- Nenhuma mudança na aplicação.
