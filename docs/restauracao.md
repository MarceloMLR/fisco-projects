# Roteiro de restauração

Como recuperar o FiscoProjects de uma perda de dados e como testar a restauração a cada 3 meses (ADR-0059). Vale para a instalação no Azure (ADR-0052); os nomes entre `< >` vêm do `.tfvars` da instalação (ADR-0053).

Metas: perder no máximo alguns minutos de dados e voltar ao ar em até 4 horas.

## O que é guardado e onde

| O quê | Onde fica | Como volta |
| --- | --- | --- |
| Banco (projetos, chamados, documentos registrados, acessos, auditoria, chaves dos acessos) | Azure SQL, com restauração a qualquer ponto em 14 dias (Basic: 7) e um backup por mês guardado por 12 meses, com redundância por zona na Brazil South | Restauração a um ponto no tempo ou de um backup mensal |
| Arquivos (anexos, documentos, termos, logos) | SharePoint da empresa, na pasta da instalação (ADR-0030) | Histórico de versões e lixeira do Microsoft 365 |
| Segredos e certificados (chave do Syncfusion, certificado das chaves dos acessos) | Key Vault, com exclusão reversível e proteção contra exclusão definitiva | Recuperar o item excluído |
| Infraestrutura e configuração | Terraform, com o estado versionado, e os arquivos privados da instalação (ADR-0053, ADR-0127) | `terraform apply` com os parâmetros da instalação |
| Versão da aplicação | Pacote da versão (`fiscoprojects-vX.Y.Z.zip`), na página da versão no GitHub | Nova publicação pelo `Publish-Version.ps1` ou pelo workflow de produção (ADR-0055, ADR-0127) |

As chaves que cifram as senhas dos acessos ficam no próprio banco (`credentials.data_protection_keys`) e voltam com ele; quando a instalação usa o certificado de `Credentials:KeyCertificate`, ele precisa estar no Key Vault, senão as senhas não abrem (ADR-0033).

## Antes de começar

- Quem restaura: o responsável pela instalação, com acesso de colaborador ao grupo de recursos e ao Key Vault.
- Ferramentas: Azure CLI (`az login` e `az account set --subscription <assinatura>`) ou o portal do Azure.
- Anote a hora (em UTC) do ponto que quer recuperar: o último momento em que os dados estavam certos. A Auditoria do FiscoProjects ajuda a achar quando a mudança errada aconteceu.
- Avise as pessoas: durante a troca do banco, a aplicação fica fora do ar.

## Cenário 1: dados apagados ou alterados por engano

Use quando a aplicação está no ar e só parte dos dados se perdeu, como um projeto excluído pelo administrador.

1. Restaure o banco num banco novo, no ponto anterior ao erro:

   ```bash
   az sql db restore \
     --resource-group <grupo> --server <servidor> --name <banco> \
     --dest-name <banco>-restaurado \
     --time "2026-10-21T14:30:00"
   ```

2. Compare o banco restaurado com o atual e decida:
   - Se só alguns registros precisam voltar e há como copiá-los com segurança (por exemplo, uma tabela que só esse projeto usa), copie-os do banco restaurado.
   - Se a perda é grande ou espalhada por vários módulos, como numa exclusão de projeto (ADR-0111), troque o banco inteiro (Cenário 2, a partir do passo 2). O que foi feito depois do ponto escolhido se perde e precisa ser refeito.
3. Apague o banco restaurado quando não precisar mais dele:

   ```bash
   az sql db delete --resource-group <grupo> --server <servidor> --name <banco>-restaurado --yes
   ```

## Cenário 2: banco perdido ou corrompido

1. Restaure no ponto mais recente que estiver bom (como no Cenário 1). Se o banco foi excluído, use o momento da exclusão:

   ```bash
   az sql db restore \
     --resource-group <grupo> --server <servidor> --name <banco> \
     --dest-name <banco>-restaurado \
     --deleted-time "2026-10-21T14:30:00"
   ```

   Se o servidor inteiro foi excluído, só os backups mensais servem: liste-os com `az sql db ltr-backup list --location brazilsouth --server <servidor> --database <banco>` e restaure com `az sql db ltr-backup restore --dest-database <banco> --dest-server <servidor-novo> --dest-resource-group <grupo> --backup-id <id>`.
2. Pare a aplicação, para que nada seja gravado durante a troca:

   ```bash
   az webapp stop --resource-group <grupo> --name <app>
   ```

3. Troque os bancos pelo nome, para a aplicação continuar com a mesma configuração: renomeie o banco ruim e dê ao restaurado o nome original.

   ```bash
   az sql db rename --resource-group <grupo> --server <servidor> --name <banco> --new-name <banco>-antigo
   az sql db rename --resource-group <grupo> --server <servidor> --name <banco>-restaurado --new-name <banco>
   ```

4. Rode o `terraform plan` da instalação e confira que ele não quer mudar o banco; se quiser (retenção, redundância, backup mensal), aplique, para o banco restaurado voltar às definições do ADR-0059.
5. Suba a aplicação e confira:

   ```bash
   az webapp start --resource-group <grupo> --name <app>
   curl -s https://<endereço>/health
   ```

   Depois, entre como administrador e veja a Auditoria: o último registro deve ser do ponto restaurado.
6. Depois de alguns dias, apague o `<banco>-antigo`.

### Depois da troca

- **E-mails**: os que estavam na fila no ponto restaurado e já tinham saído podem sair de novo. Avise as pessoas que podem receber avisos repetidos.
- **Arquivos**: o banco volta no tempo, mas o SharePoint não. Um arquivo enviado depois do ponto restaurado fica no SharePoint sem registro no sistema, e não atrapalha. Um arquivo excluído depois do ponto restaurado aparece no sistema, mas não abre: restaure-o pela lixeira do SharePoint (Cenário 3) ou envie de novo pela tela.
- **Pessoas e papéis**: voltam como estavam no ponto restaurado. Confira os administradores em Usuários.

## Cenário 3: arquivo apagado ou alterado no SharePoint

1. Na biblioteca da instalação, abra a lixeira do site e restaure o arquivo, ou abra o histórico de versões do arquivo e restaure a versão certa.
2. Abra o documento ou o anexo pelo FiscoProjects para conferir. Se o sistema não encontrar o arquivo restaurado, envie-o de novo pela tela.

O prazo da lixeira e do histórico segue a política de retenção do Microsoft 365 da empresa (ADR-0059).

## Cenário 4: segredo ou certificado excluído no Key Vault

1. Liste os itens excluídos e recupere o que faltar:

   ```bash
   az keyvault secret list-deleted --vault-name <cofre>
   az keyvault secret recover --vault-name <cofre> --name <segredo>
   az keyvault certificate list-deleted --vault-name <cofre>
   az keyvault certificate recover --vault-name <cofre> --name <certificado>
   ```

2. Reinicie a aplicação (`az webapp restart`), para ela ler de novo os segredos.

Se o cofre inteiro foi excluído, recupere-o com `az keyvault recover --name <cofre>`; a proteção contra exclusão definitiva impede que ele suma antes do prazo.

## Cenário 5: a região Brazil South inteira fora do ar

Com a redundância por zona (o padrão do ADR-0059), os backups ficam na Brazil South: não há como restaurar em outra região, e a aplicação volta quando a região voltar. Uma instalação que preferir restaurar em outra região troca o parâmetro de redundância do Terraform para geo-redundante, sabendo que a cópia fica nos Estados Unidos (ADR-0052, ADR-0059).

## Teste trimestral

A cada 3 meses, restaure o banco de produção num banco de teste e confira que ele abre. Leva menos de uma hora e não mexe na aplicação.

1. Restaure o ponto de uma hora atrás num banco de teste:

   ```bash
   az sql db restore \
     --resource-group <grupo> --server <servidor> --name <banco> \
     --dest-name <banco>-teste-restauracao \
     --time "<agora menos uma hora, em UTC>"
   ```

2. Anote quanto tempo a restauração levou.
3. Conecte no banco de teste (pelo editor de consultas do portal) e confira que os dados estão lá:

   ```sql
   SELECT COUNT(*) AS Projetos FROM projects.projects;
   SELECT COUNT(*) AS Chamados FROM tickets.tickets;
   SELECT COUNT(*) AS Documentos FROM documents.documents;
   SELECT MAX(OccurredAt) AS UltimoRegistro FROM audit.entries;
   ```

   As quantidades devem bater com as telas, e o último registro da auditoria deve ser de pouco antes do ponto restaurado.
4. Confira que existe pelo menos um backup mensal: `az sql db ltr-backup list --location brazilsouth --server <servidor> --database <banco>`.
5. Apague o banco de teste:

   ```bash
   az sql db delete --resource-group <grupo> --server <servidor> --name <banco>-teste-restauracao --yes
   ```

6. Registre o teste na tabela abaixo. Se algo falhou, corrija antes do próximo trimestre e registre o que mudou.

### Registro dos testes

| Data | Quem | Ponto restaurado (UTC) | Tempo da restauração | Resultado |
| --- | --- | --- | --- | --- |
| | | | | |
