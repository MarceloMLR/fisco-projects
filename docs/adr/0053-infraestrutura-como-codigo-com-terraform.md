---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0053: Infraestrutura como código com Terraform

## Contexto e problema

Cada empresa tem a própria instalação do FiscoProjects (ADR-0003), no Azure, com App Service, Azure SQL, Key Vault, monitoramento, identidade gerenciada e o registro do aplicativo no Entra ID. Criar tudo à mão no portal deixa cada instalação um pouco diferente e difícil de reproduzir. Como descrever e criar a infraestrutura?

## Fatores de decisão

- Instalações reproduzíveis com um comando.
- Cobrir o Azure e o registro do aplicativo no Entra ID.
- Valor de mercado da ferramenta e conhecimento de quem vai manter.
- Esforço de operar a ferramenta em cada instalação.

## Opções consideradas

- A. Terraform
- B. OpenTofu (fork de código aberto do Terraform)
- C. Bicep
- D. Criação manual pelo portal, seguindo um guia

## Resultado da decisão

Opção escolhida: **A. Terraform**, por preferência de Marcelo: é o padrão de mercado, funciona em várias nuvens e gerencia o Azure e o Entra ID na mesma ferramenta. O custo é cuidar do arquivo de estado de cada instalação, tratado nas regras abaixo.

Regras:

- O código fica em `infra/terraform/`, com módulos (aplicação, banco, Key Vault, monitoramento, aplicativo no Entra) e uma configuração por instalação e ambiente.
- Provedores: `azurerm` para o Azure e `azuread` para o registro do aplicativo no Entra ID, permissões e credenciais. Versões do Terraform e dos provedores fixadas.
- Estado remoto em uma conta de armazenamento do Azure por instalação, com versionamento, exclusão reversível e bloqueio de estado, com acesso restrito. Arquivos de estado nunca vão para o repositório.
- Um script de preparação cria a conta de armazenamento do estado antes da primeira aplicação.
- Parâmetros de cada instalação em arquivos `.tfvars`, sem segredos.
- Nenhum segredo é gerado ou passado pelo Terraform, porque o arquivo de estado guarda valores em texto; certificados e chaves são criados direto no Key Vault.
- O que o Terraform não cobre (restrição da caixa de envio no Exchange, permissão do aplicativo no site do SharePoint, personalização da tela de login) fica em scripts PowerShell versionados em `infra/scripts`, citados no guia de instalação.
- `terraform fmt`, `validate` e `plan` rodam em todo pull request; `apply` roda pelo pipeline de CI/CD, depois de revisado o plano.
- Nada é criado à mão no portal sem estar no código.
- O código deve continuar compatível com o OpenTofu, caso a licença do Terraform (BSL) passe a afetar o uso.

### Consequências

- Bom, porque cada instalação sai igual, a partir do mesmo código e de parâmetros próprios.
- Bom, porque Azure e Entra ID ficam na mesma ferramenta, conhecida no mercado.
- Ruim, porque cada instalação tem um arquivo de estado para guardar e proteger.
- Ruim, porque quem mantiver a instalação precisa conhecer Terraform.
- Neutro, porque a licença BSL não afeta quem usa o Terraform para a própria infraestrutura.

### Confirmação

O CI executa `fmt`, `validate` e `plan`; nenhum arquivo de estado ou `.terraform` está no repositório; uma instalação completa pode ser criada do zero numa assinatura nova seguindo o guia (teste de instalação limpa antes de cada versão).

## Prós e contras das opções

### A. Terraform

- Bom, porque é padrão de mercado e cobre Azure e Entra ID.
- Ruim, porque exige cuidar do arquivo de estado.

### B. OpenTofu

- Bom, porque é código aberto, com a mesma linguagem e provedores.
- Ruim, porque tem o mesmo cuidado com o estado e uma comunidade menor.

### C. Bicep

- Bom, porque é nativo do Azure e não tem arquivo de estado.
- Ruim, porque só funciona no Azure.

### D. Criação manual

- Bom, porque dispensa aprender uma ferramenta.
- Ruim, porque cada instalação sai diferente e a configuração deriva com o tempo.
