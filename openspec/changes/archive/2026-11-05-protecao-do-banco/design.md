## Context

O módulo `database` cria o servidor (`azurerm_mssql_server.main`) e o banco (`azurerm_mssql_database.main`) com a restauração a qualquer ponto, os backups mensais e a redundância do ADR-0059. Apagado o servidor, o Azure apaga os bancos e os backups da restauração a qualquer ponto, e não há como restaurar o servidor (a retenção do servidor apagado está em preview e desligada nos servidores com menos de dois anos); sobram os backups mensais, que voltam num servidor novo da mesma assinatura. O Key Vault já tem proteção contra exclusão definitiva (ADR-0059). O roteiro (`docs/restauracao.md`) restaura num banco novo do mesmo servidor e, no Cenário 2, troca os bancos pelo nome.

## Goals / Non-Goals

**Goals:**
- O Azure recusa apagar o banco, o servidor e o grupo de recursos da instalação, por qualquer caminho (portal, CLI, script), até alguém tirar a trava de propósito.
- O roteiro e o `terraform destroy` continuam funcionando.
- Um teste de restauração registrado.

**Non-Goals:**
- A cópia fora da assinatura (ADR-0154, proposta).
- Travar o App Service, o Key Vault ou o monitoramento: são recriados pelo Terraform, e o Key Vault já é recuperável.
- Proteger os dados dentro do banco (exclusão pela aplicação): para isso existem a restauração a qualquer ponto e o Cenário 1.

## Decisions

- **Trava no banco, e não no servidor nem no grupo de recursos.** Uma trava num recurso pai vale para os filhos: no servidor ou no grupo, ela também travaria o banco de teste do roteiro, que o teste trimestral apaga no fim. No banco, ela recusa apagá-lo e recusa apagar o grupo de recursos que o contém (o Azure não faz exclusão parcial). Que ela também recusa apagar o servidor, que levaria o banco junto, fica confirmado num servidor descartável antes do merge; se não recusar, a trava vai também para o servidor, e o teste trimestral passa a tirá-la para apagar o banco de teste.
- **`azurerm_management_lock`** no módulo `database`, com o nível `CanNotDelete`, o nome `nao-apagar-banco` e uma nota que manda ao roteiro, criada só com `delete_lock = true` (variável `database_delete_lock` na raiz, `true` por padrão, o padrão da produção). O `dev` fica com a trava também: ela não atrapalha o `terraform destroy` nem a recarga da demonstração, que apaga só as tabelas.
- **O `terraform destroy` tira a trava antes do banco**, porque a trava depende dele. O guia diz que a exclusão por fora do Terraform (`az group delete`) é recusada enquanto ela existir.
- **Permissão:** criar e tirar travas pede `Microsoft.Authorization/locks/*` (Proprietário ou Administrador de Acesso do Usuário), que quem opera já tem para o `apply`; a identidade do pipeline só lê as travas no `plan`.
- **Cenário 2 do roteiro:** antes de renomear os bancos, `az lock delete` da trava; o passo do `terraform plan` e `apply` que já existe a cria de novo no banco restaurado, que ficou com o nome original.

## Risks / Trade-offs

- [A trava atrasa uma restauração urgente] → é um comando a mais no Cenário 2, escrito no roteiro.
- [Alguém com permissão tira a trava e apaga o banco] → a trava protege do engano, não de quem tem a permissão e a intenção; a conta invadida é o caso do ADR-0154.
- [A trava no banco não segura o servidor] → confirmado antes do merge; se for o caso, a trava vai para o servidor também.
