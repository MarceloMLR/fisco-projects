## Context

A chave chega hoje por três caminhos:

- **Produção e `dev`:** segredo `Syncfusion--LicenseKey` criado à mão no Key Vault. O Terraform o liga ao App Service pela configuração `Syncfusion__LicenseKey`, só com `syncfusion_license_in_key_vault = true`, que o `dev.tfvars` liga.
- **Codespaces:** segredo `Syncfusion__LicenseKey` da conta de Marcelo.
- **Máquina:** segredos do usuário (`Syncfusion:LicenseKey`).

A API devolve a chave em `GET /api/client-settings` (`ClientSettings.cs`, ADR-0087), e desde a Gantt-1 nenhuma tela a pede. O `InfrastructureTests` exige a referência no módulo `app`. O `DevContainerTests` confere que o `.devcontainer` não traz chave.

## Goals / Non-Goals

**Goals:**

- Nenhum lugar do sistema, dos scripts ou dos guias pede, guarda ou entrega a chave.
- Instalação existente que ainda tem a chave passa pela atualização sem erro e sabe o que apagar.

**Non-Goals:**

- Mexer no Key Vault, no Terraform aplicado ou nos segredos de Marcelo: são operações dele.

## Decisions

- **A rota sai inteira.** `GET /api/client-settings` só levava a chave. Mantê-la vazia, à espera de outra configuração do navegador, seria uma rota sem uso. Uma configuração nova terá a própria decisão. Pedida depois, a rota responde 404, como qualquer rota que não existe.
- **A variável sai do Terraform, sem período de transição.** Uma instalação cujo `local.auto.tfvars` ainda tenha `syncfusion_license_in_key_vault` recebe só um aviso do Terraform de valor para variável não declarada, não um erro. O guia manda tirar a linha. O `apply` tira a configuração `Syncfusion__LicenseKey` do App Service, e a aplicação, que não a lê mais, não percebe. A alternativa de manter a variável sem efeito deixaria lixo no código e no guia.
- **O segredo é apagado à mão, depois do `apply`.** O Terraform nunca criou nem apagou segredos (ADR-0053), então o guia diz o comando (`az keyvault secret delete`). Com a exclusão reversível, ele ainda fica recuperável pelo prazo do cofre.
- **Os testes mudam de lado.** O `InfrastructureTests` passa a conferir que nada do Terraform nem do guia cita o Syncfusion. Um teste da API confere que a rota não existe mais, com o nome do cenário novo. O `DevContainerTests` continua conferindo que o `.devcontainer` não traz chave.
- **Os ADRs não mudam de conteúdo.** O ADR-0051 e o ADR-0087 só ganham o status "substituída pelo ADR-0134", no arquivo e no índice, como os ADRs substituídos antes.
- **A v2 no roadmap.** O título passa a "Roadmap" e a seção "Depois da v1" passa a "Acréscimos da v1", com os grupos 11 e 12. Uma seção "v2" abre antes do grupo 13. A nota de 31/10/2026 registra a divisão que Marcelo fez.

## Risks / Trade-offs

- [O `dev` continua com a configuração e o segredo até Marcelo aplicar] → a aplicação não os lê. O `plan.sh` de cada publicação avisa a diferença até o `apply`, e o resumo da change diz os passos.
- [Uma versão anterior à Gantt-1 publicada de novo sem a chave mostra o aviso de avaliação do Syncfusion] → voltar a uma versão anterior é exceção. O guia já manda conferir o plano, e a chave pode ser recuperada da exclusão reversível.

## Migration Plan

1. Merge e publicação no `dev`. A aplicação não lê a chave, e o `plan.sh` avisa que a configuração `Syncfusion__LicenseKey` sairá.
2. Marcelo, pelo guia:
   - tira `syncfusion_license_in_key_vault` do `dev.tfvars`, já feito nesta change;
   - roda o `plan` e o `apply`;
   - apaga o segredo `Syncfusion--LicenseKey` do Key Vault.
3. Opcional: apagar o segredo `Syncfusion__LicenseKey` do Codespaces e o `Syncfusion:LicenseKey` dos segredos do usuário.
4. Para voltar atrás: publicar a versão anterior pelo workflow. A Linha do tempo própria não precisa da chave.
