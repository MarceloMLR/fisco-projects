## Context

O banco guarda tudo o que o sistema registra, inclusive as chaves que cifram as senhas dos acessos; os arquivos ficam no SharePoint da empresa; os segredos, no Key Vault; a infraestrutura, no Terraform. A aplicação lê a connection string pela configuração do App Service, com o nome do banco definido pelo Terraform.

## Goals / Non-Goals

**Goals:**
- Um roteiro que alguém da empresa siga sem conhecer o código.
- O teste trimestral com registro.

**Non-Goals:**
- As definições dos backups no Terraform (Fundação-6).
- Backups próprios ou exportações (opção C, descartada no ADR-0059).

## Decisions

- **Trocar o banco pelo nome**: restaurar num banco novo e trocar os nomes (`az sql db rename`), em vez de mudar a connection string, para a configuração do App Service e do Terraform continuar a mesma; o banco antigo fica alguns dias antes de ser apagado.
- **Parar a aplicação durante a troca**, para nada ser gravado no banco errado.
- **Arquivos e banco fora de sincronia**: o roteiro diz o que acontece com os arquivos enviados ou excluídos depois do ponto restaurado e com os e-mails da fila, em vez de tentar sincronizar.
- **Teste trimestral num banco de teste**, sem mexer na aplicação, com as consultas de conferência e o registro na própria página.

## Risks / Trade-offs

- [Comandos e nomes antes do Terraform] → os nomes ficam entre `< >`, vindos do `.tfvars` da instalação; a Fundação-6 confere o roteiro contra a infraestrutura criada.
