## 1. Decisão

- [ ] 1.1 ADR-0123, índice dos ADRs e glossário

## 2. Servidor

- [ ] 2.1 `GraphEmailSender`, `EmailSendException` e o `HttpClient` `graph-mail`, com os testes "E-mail pela caixa compartilhada", "Caixa sem permissão de envio" e "Graph fora do ar"
- [ ] 2.2 Opções do Graph conferidas ao subir, com os testes "Graph sem caixa de envio" e "Domínios locais em produção"
- [ ] 2.3 Domínios locais na pasta (`EmailRouting`), com os testes "Endereço da demonstração na pasta" e "Endereço do tenant pelo Graph"

## 3. Infraestrutura e publicação

- [ ] 3.1 `email_mailbox` no Terraform, `dev.tfvars` com os domínios locais e o teste "Caixa de envio pelo Terraform" e "E-mails da demonstração no ambiente dev"
- [ ] 3.2 `infra/scripts/Grant-ExchangeMailbox.ps1`
- [ ] 3.3 `plan.sh` com a caixa mascarada, com o teste "Caixa de envio mascarada"

## 4. Documentação e conferência

- [ ] 4.1 `docs/instalacao.md`, `CLAUDE.md` e roadmap (M365-4)
- [ ] 4.2 Build sem avisos, testes, `dotnet format`, `infra.sh`, lint, typecheck e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
