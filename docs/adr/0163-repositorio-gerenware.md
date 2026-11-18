---
status: aceita
date: 2026-11-17
decision-makers: Marcelo Ribeiro
---

# ADR-0163: Repositório renomeado para gerenware

Complementa o ADR-0055 (CI/CD), o ADR-0113 (Codespaces), o ADR-0115 (publicação do `dev`) e o ADR-0159 (a marca). Substitui, no discovery ("Definições sem ADR"), o nome do repositório, `fisco-projects`.

## Contexto e problema

O repositório no GitHub se chama `fisco-projects`. O nome aparece no endereço, nos Codespaces, no pacote de cada versão e no subject dos tokens que o GitHub Actions usa para entrar no Azure (`github_repository`, com os números do dono e do repositório). O produto agora se chama GerenWare. O repositório muda de nome, e quando?

## Opções consideradas

- A. Renomear o repositório para `gerenware`, junto com a credencial federada do deploy
- B. Manter `fisco-projects` como nome do repositório
- C. Criar um repositório novo e copiar o código

## Resultado da decisão

Opção escolhida: **A**, pedida por Marcelo em 17/11/2026.

Regras:

- **Quem renomeia.** Marcelo, nas configurações do repositório, depois do merge desta change. O GitHub redireciona os endereços antigos, e o número do repositório não muda.
- **Credencial federada.** O subject dos tokens leva o nome do repositório. O `github_repository` dos `environments/*.tfvars` passa a `MarceloMLR@106113485/gerenware@1423675463`, e o Terraform é aplicado logo depois da renomeação; entre uma coisa e outra, o `deploy-dev` não entra no Azure.
- **Environments do GitHub.** O `Set-GitHubEnvironment.ps1` passa a apontar para `MarceloMLR/gerenware`. Os environments `dev` e `prod`, as regras e os segredos acompanham o repositório na renomeação.
- **Codespaces.** Os Codespaces criados antes continuam abrindo, com a pasta antiga; um Codespace novo já nasce em `/workspaces/gerenware`. A demonstração usa um Codespace novo.
- **Clones.** Cada clone troca o endereço com `git remote set-url origin https://github.com/MarceloMLR/gerenware.git`; a pasta local pode ser renomeada à vontade.
- **Ordem com a Marca-6.** A renomeação e o `apply` da credencial entram no mesmo roteiro da recriação dos ambientes, antes do passo de publicar.

### Consequências

- Bom, porque o endereço do código passa a ter o nome do produto.
- Ruim, porque o deploy fica sem acesso ao Azure entre a renomeação e o `apply`.
- Neutro, porque os links antigos continuam funcionando pelo redirecionamento, enquanto ninguém criar outro repositório com o nome antigo.

### Confirmação

Os testes do pipeline conferem o `github_repository` novo nos dois ambientes. Depois da renomeação e do `apply`, o `deploy-dev` entra no Azure e publica.

## Prós e contras das opções

### A. Renomear

- Bom, porque mantém o histórico, os PRs e os números.
- Ruim, porque pede um `apply` logo em seguida.

### B. Manter o nome

- Bom, porque nada muda.
- Ruim, porque o nome antigo ficaria no endereço e nos pacotes.

### C. Repositório novo

- Bom, porque começa limpo.
- Ruim, porque perde os PRs, as versões e os environments.
