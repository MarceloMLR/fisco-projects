---
status: aceita
date: 2026-11-17
decision-makers: Marcelo Ribeiro
---

# ADR-0162: Recursos do Azure recriados com o prefixo gerenware

Complementa o ADR-0053 (Terraform), o ADR-0127 (pacote da versão) e o ADR-0159 (a marca).

## Contexto e problema

Os recursos do Azure de cada instalação levam o nome do produto: `rg-fiscoprojects-<instalação>-<ambiente>`, `app-fiscoprojects-…`, `sqldb-fiscoprojects`, o grupo e a conta do estado do Terraform (`rg-fiscoprojects-<instalação>-tfstate`, `stfp<instalação>tfstate`) e o pacote da versão (`fiscoprojects-vX.Y.Z.zip`). O Azure não renomeia grupo de recursos, App Service, servidor SQL, Key Vault nem conta de armazenamento. Hoje existem só os ambientes `dev` e `prod` de Marcelo, com dados de demonstração. Como os nomes mudam?

## Opções consideradas

- A. Destruir os ambientes e recriá-los com o prefixo `gerenware`, inclusive o estado do Terraform
- B. Manter os nomes atuais dos recursos e usar o prefixo novo só nas instalações futuras
- C. Mover recurso a recurso para grupos novos e recriar só o que o Azure não move

## Resultado da decisão

Opção escolhida: **A**, autorizada por Marcelo em 17/11/2026, porque só há dados de demonstração e nenhuma instalação de empresa.

| Item | Antes | Depois |
| --- | --- | --- |
| Prefixo dos recursos | `fiscoprojects-<instalação>-<ambiente>` | `gerenware-<instalação>-<ambiente>` |
| Banco | `sqldb-fiscoprojects` | `sqldb-gerenware` |
| Estado do Terraform | `rg-fiscoprojects-<instalação>-tfstate`, `stfp<instalação>tfstate` | `rg-gerenware-<instalação>-tfstate`, `stgw<instalação>tfstate` |
| Nome curto do grupo de ações dos alertas | `fiscoproj` | `gerenware` |
| Pacote da versão | `fiscoprojects-vX.Y.Z.zip` | `gerenware-vX.Y.Z.zip` |

Regras:

- O `destroy` roda com o código e o estado antigos, antes de o código novo entrar em uso; o estado antigo é apagado depois.
- O registro do aplicativo no Entra ID é recriado com os recursos. Por isso as permissões dadas fora do Terraform são dadas de novo ao registro novo: o papel no site do SharePoint (`Grant-SharePointSite.ps1`) e o envio pela caixa (`Grant-ExchangeMailbox.ps1`); as do registro antigo são tiradas.
- O endereço da aplicação muda (`app-gerenware-…`): o pacote do Teams é baixado e instalado de novo, e o aviso da Autentique aponta para o endereço novo.
- Os segredos criados à mão no Key Vault são criados de novo no cofre novo.
- A demonstração é carregada de novo pela publicação do `dev`.
- O `destroy` e o `apply` continuam com quem opera (ADR-0115): esta decisão não dá ao pipeline o poder de criar ou apagar recursos.

### Consequências

- Bom, porque não sobra recurso com o nome antigo.
- Ruim, porque os ambientes ficam fora do ar entre o `destroy` e a publicação seguinte.
- Neutro, porque o Key Vault antigo fica recuperável pelo prazo da exclusão reversível; o nome novo não entra em conflito com ele.

### Confirmação

Os testes do pipeline conferem os nomes novos no Terraform, nos scripts de publicação e no pacote da versão; o `terraform validate` passa. Depois da operação, o `/health` do endereço novo responde e a demonstração abre.

## Prós e contras das opções

### A. Destruir e recriar

- Bom, porque é o caminho que o Terraform já sabe fazer.
- Ruim, porque refaz os passos manuais do Microsoft 365.

### B. Manter os nomes atuais

- Bom, porque nada para.
- Ruim, porque os ambientes de Marcelo ficariam com o nome antigo para sempre.

### C. Mover recurso a recurso

- Bom, porque preserva os dados.
- Ruim, porque não há dados a preservar, e quase tudo precisaria ser recriado do mesmo jeito.
