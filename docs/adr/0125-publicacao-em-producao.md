---
status: aceita
date: 2026-10-28
decision-makers: Marcelo Ribeiro
---

# ADR-0125: Publicação em produção por versão, com aprovação e migrações pelo pipeline

Substitui a linha "Produção" do ADR-0055 e completa o ADR-0115, que deixou as migrações da produção para o workflow manual (Operação-5).

## Contexto e problema

O ADR-0055 decidiu que a produção é publicada por um workflow acionado à mão, por versão, com a revisão do plano, o `terraform apply`, as migrações e a publicação, e que a aprovação obrigatória antes do deploy só existia nos planos pagos do GitHub. Depois, o ADR-0115 tirou o `apply` do pipeline, que só confere a infraestrutura, e o ADR-0114 tirou da identidade da aplicação, na produção, o poder de mudar o banco: as migrações ficaram para a publicação. O repositório agora é público, e nele os environments do GitHub com aprovação obrigatória são gratuitos. Como publicar uma versão na produção, quem aprova, e quem aplica as migrações e dá à aplicação o acesso aos dados?

## Fatores de decisão

- Nenhum segredo no GitHub (ADR-0055) e o menor poder possível para quem publica.
- Repositório público: um workflow alterado e aceito num merge não pode chegar à produção sozinho.
- A versão que vai para a produção precisa ser conhecida e repetível.
- A aplicação na produção só lê e grava os dados; as mudanças do banco vêm da publicação (ADR-0114).
- O mesmo código de migração do desenvolvimento, sem uma segunda ferramenta.

## Opções consideradas

Para aplicar as migrações na produção:

- A. Um comando da própria API (`migrate`), rodado pelo pipeline com a identidade dele, que aplica as migrações de todos os módulos pelo mesmo `ModuleMigrator` do desenvolvimento.
- B. Um pacote de migração do EF Core (`migrations bundle`) por módulo, rodado pelo pipeline.
- C. Scripts SQL idempotentes gerados por módulo e rodados pelo `sqlcmd`.

## Resultado da decisão

Opção escolhida: **A. O comando `migrate` da API**, porque aplica as migrações na mesma ordem e com o mesmo código do desenvolvimento, sai do mesmo pacote que vai para o App Service e não pede outra ferramenta no pipeline.

Regras:

- **Workflow `deploy-prod`**, acionado à mão pela aba Actions na tag da versão (`v1.2.3`), sem gatilho automático. O script `version.sh` recusa qualquer outra referência antes de tudo.
- **Environment `prod` do GitHub**, preparado pelo `infra/scripts/Set-GitHubEnvironment.ps1`: aceita só as tags `v*` e exige a aprovação de quem opera a instalação antes de cada publicação. Quem dispara pode aprovar a própria publicação, porque numa instalação pequena é a mesma pessoa.
- **Ordem**: a versão, a conferência da infraestrutura, o pacote, as migrações e a publicação, que espera o `/health`. Na produção, a infraestrutura diferente do código para a publicação, antes de mudar qualquer coisa, pedindo o `apply` de quem opera (ADR-0115); no `dev`, continua só um aviso.
- **Migrações**: o pipeline roda `dotnet FiscoProjects.Api.dll migrate` com a conexão pelo Entra ID (`Authentication=Active Directory Default`, a conta do Azure CLI do pipeline). A API monta os serviços, aplica as migrações de todos os módulos e termina, sem subir o servidor nem os processos em segundo plano. Aplicar de novo não muda nada.
- **Identidade do pipeline da produção** no grupo dos administradores do banco, pelo Terraform, quando a aplicação não é administradora (`app_is_database_admin = false`). Ela continua sem dar papéis no Azure e sem mudar o Entra ID: quem a põe no grupo é o `apply` de quem opera.
- **Acesso da aplicação**: o mesmo comando cria o usuário da identidade gerenciada da aplicação no banco (`CREATE USER ... FROM EXTERNAL PROVIDER WITH OBJECT_ID`, sem dar ao servidor a leitura do diretório) e o põe só em `db_datareader` e `db_datawriter`, que cobrem as tabelas e as sequências de todos os módulos, inclusive as das migrações futuras. A aplicação não altera o banco.
- **Sem dados de demonstração** na produção: só o ambiente em `Development` os carrega.

### Consequências

- Bom, porque nenhuma publicação na produção acontece sem a versão marcada e a aprovação de quem opera.
- Bom, porque as migrações da produção usam o mesmo código testado no desenvolvimento.
- Bom, porque a aplicação no ar só lê e grava dados, e um defeito nela não muda o banco.
- Ruim, porque a identidade do pipeline da produção administra o banco; a proteção é o environment, que só aceita as tags e pede aprovação.
- Ruim, porque as migrações rodam antes da nova versão subir: por alguns minutos, a versão anterior roda no banco novo. As migrações do projeto só acrescentam; uma que remova algo precisa ser feita em duas versões.
- Neutro, porque o banco precisa aceitar a conexão do executor do GitHub; o servidor aceita os serviços do Azure, onde ficam os executores.

### Confirmação

Testes do pipeline com substitutos do `az`, do `dotnet` e do `terraform`: a ordem dos passos, a recusa fora de uma tag de versão, a parada com a infraestrutura diferente, a chamada do comando com a conexão e a identidade da aplicação, a produção sem dados de demonstração e o environment com aprovação e só as tags. Testes da API com o SQL Server em contêiner: o comando aplica as migrações de todos os módulos e não muda nada na segunda vez, e um usuário com o acesso dado à aplicação lê, grava e pega o número de um chamado, mas não altera o banco.

## Prós e contras das opções

### A. Comando `migrate` da API

- Bom, porque reaproveita o registro dos módulos e a ordem das migrações.
- Ruim, porque a API ganha um modo que não sobe o servidor.

### B. Pacotes de migração do EF Core

- Bom, porque é o caminho documentado pelo EF Core.
- Ruim, porque seria um executável por módulo, montado a cada publicação, com a ordem mantida à mão.

### C. Scripts SQL idempotentes

- Bom, porque o SQL fica visível antes de rodar.
- Ruim, porque depende do `sqlcmd` com o Entra ID no executor e de manter a ordem dos módulos fora do código.
