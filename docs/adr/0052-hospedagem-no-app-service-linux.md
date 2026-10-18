---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0052: Hospedagem no Azure App Service Linux, com Key Vault e identidade gerenciada

## Contexto e problema

O backend ASP.NET Core serve a API e o build do React num único deploy (ADR-0047), com processos em segundo plano dentro da aplicação. O banco é Azure SQL (ADR-0048). O uso esperado é de 5 a 6 pessoas por instalação. Onde a aplicação roda no Azure?

## Fatores de decisão

- Simplicidade de operação para uma pessoa.
- Processos em segundo plano sempre ativos.
- Nenhuma senha no código ou na configuração.
- Custo baixo para o uso esperado (`claude/custos-estimados.md`).

## Opções consideradas

- A. Azure App Service Linux
- B. Azure Container Apps
- C. Máquina virtual

## Resultado da decisão

Opção escolhida: **A. App Service Linux**, porque é o mais simples de operar, mantém os processos em segundo plano com o Always On e permite subir de plano sem migração.

Regras:

- Região Brazil South (menor latência e dados no Brasil).
- Ambientes: produção (App Service B1 e Azure SQL S0, ou Basic para começar); desenvolvimento e demonstração, opcional (App Service B1 e Azure SQL na oferta gratuita); desenvolvimento local com Docker.
- Always On ligado em produção.
- A aplicação acessa o Azure SQL e o Key Vault pela identidade gerenciada do App Service, sem senhas.
- Segredos no Key Vault: certificado do aplicativo no Entra (usado pelo Graph) e chave de licença do Syncfusion (ADR-0051).
- Só HTTPS; domínio próprio opcional, com certificado gerenciado gratuito.
- Monitoramento, backups e retenção são decididos em ADRs próprios.

### Consequências

- Bom, porque a Microsoft cuida do sistema operacional e das atualizações.
- Bom, porque não há senhas de banco nem de serviços na configuração.
- Ruim, porque o plano B1 não tem slots de deploy: cada publicação deixa o sistema indisponível por alguns segundos (aceitável para 5 a 6 pessoas; slots chegam com o plano P0v3).
- Ruim, porque a região Brazil South custa um pouco mais que regiões dos Estados Unidos.

### Confirmação

A infraestrutura criada pelo Terraform (ADR-0053) mostra App Service Linux com Always On, identidade gerenciada com acesso ao Azure SQL e ao Key Vault, HTTPS obrigatório e nenhuma senha nas configurações da aplicação.

## Prós e contras das opções

### A. App Service Linux

- Bom, porque é simples e gerenciado.
- Ruim, porque o plano B1 não tem slots de deploy.

### B. Azure Container Apps

- Bom, porque escala automaticamente, inclusive até zero.
- Ruim, porque exige registro de contêineres e mais peças, e os processos em segundo plano impedem a escala até zero.

### C. Máquina virtual

- Bom, porque dá controle total.
- Ruim, porque exige cuidar de sistema operacional, atualizações e segurança.
