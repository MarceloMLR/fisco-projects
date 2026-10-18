---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0055: CI/CD com GitHub Actions, preparado para migrar ao Azure DevOps

## Contexto e problema

O código fica num repositório pessoal no GitHub (ADR-0002). A infraestrutura é Terraform (ADR-0053) e a aplicação roda no App Service (ADR-0052). O fluxo de Git de Marcelo exige branch por mudança e pull request para a `main`. Se uma empresa adquirir o código, o correto é migrar código e pipelines para o Azure DevOps dela. Onde rodam build, testes e deploy?

## Fatores de decisão

- Pipeline junto do código.
- Nada chega à produção sem passar pelos testes.
- Acesso ao Azure sem segredos guardados.
- Facilidade de migrar para o Azure DevOps em caso de venda.

## Opções consideradas

- A. GitHub Actions
- B. Azure DevOps Pipelines
- C. Publicação manual

## Resultado da decisão

Opção escolhida: **A. GitHub Actions**, por enquanto, com o pipeline escrito para ser migrado ao Azure DevOps se uma empresa adquirir o código.

Fluxo:

| Quando | O que roda |
| --- | --- |
| Em todo pull request | Build do backend e do frontend; testes unitários, de arquitetura e de integração (SQL Server em contêiner); lint, formatação e checagem de tipos; dependências vulneráveis; validação das specs do OpenSpec; `terraform fmt`, `validate` e `plan`, com o plano comentado no PR |
| Regra da `main` | Só recebe código por PR, com todas as verificações aprovadas |
| Merge na `main` | Deploy automático no ambiente de desenvolvimento e demonstração: `terraform apply`, migrações do banco e publicação da aplicação |
| Produção | Workflow acionado manualmente, por versão: revisão do plano, `terraform apply`, migrações do banco e publicação |

Regras:

- Acesso ao Azure por identidade federada (OIDC), sem segredos guardados no GitHub.
- A lógica de build, testes, migrações e deploy fica em scripts versionados no repositório; os arquivos de workflow apenas chamam esses scripts, para que a migração ao Azure DevOps troque só a "casca" do pipeline.
- Em caso de venda, repositório e pipelines são migrados para o Azure DevOps da empresa compradora, mantendo o histórico do Git.

### Consequências

- Bom, porque o pipeline fica junto do código e o plano gratuito (2.000 minutos por mês em repositório privado) atende o projeto.
- Bom, porque a lógica em scripts facilita a migração para o Azure DevOps.
- Ruim, porque aprovação obrigatória entre etapas de deploy, em repositório privado, só existe nos planos pagos do GitHub; por isso a produção é acionada manualmente.

### Confirmação

A `main` está protegida, exigindo PR com verificações aprovadas; nenhum segredo do Azure está nos segredos do GitHub (só a identidade federada); os workflows apenas chamam scripts do repositório.

## Prós e contras das opções

### A. GitHub Actions

- Bom, porque fica junto do código e conecta ao Azure sem senha.
- Ruim, porque o plano gratuito não tem aprovação obrigatória entre etapas em repositório privado.

### B. Azure DevOps Pipelines

- Bom, porque Marcelo já conhece e é o destino natural em caso de venda.
- Ruim, porque separaria pipeline e código sem ganho para um projeto pessoal.

### C. Publicação manual

- Bom, porque é rápida no começo.
- Ruim, porque nada garante que o que foi para produção passou pelos testes.
