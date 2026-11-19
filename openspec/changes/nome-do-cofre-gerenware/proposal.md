## Why

Na recriação do `dev` pelo roteiro da Marca-6, o Key Vault novo saiu com o nome antigo, `kv-fpmlrdev`: o nome curto do Terraform (`fp<instalação><ambiente>`) ficou com as letras do FiscoProjects. O cofre antigo, com esse nome, está na exclusão reversível, e o grupo de recursos dele não existe mais, então o Azure não o recupera e recusa criar outro com o mesmo nome por 7 dias.

## What Changes

- Nome curto do Terraform `gw<instalação><ambiente>`, como a conta do estado (`stgw<instalação>tfstate`): o cofre passa a ser `kv-gw<instalação><ambiente>`.
- Spec `infraestrutura`: o cenário do ambiente criado pelo Terraform diz também o nome do cofre.

## Capabilities

### New Capabilities

### Modified Capabilities

- `infraestrutura`: o nome do Key Vault de cada ambiente.

## Non-goals

- Mudar outro nome de recurso: o resto já leva `gerenware`.
- Purgar o cofre antigo, que sai sozinho ao fim da exclusão reversível (7 dias no `dev`), sem custo.

## Impact

- `infra/terraform/main.tf` (`local.short`).
- No `dev`, o `apply` em andamento termina com o cofre `kv-gwmlrdev`; não há `prod`.
