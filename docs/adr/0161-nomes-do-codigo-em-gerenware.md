---
status: aceita
date: 2026-11-17
decision-makers: Marcelo Ribeiro
---

# ADR-0161: Nomes do código em GerenWare, inclusive o da proteção das senhas

Complementa o ADR-0159 (a marca) e o ADR-0033 (a proteção das senhas dos acessos, cujo nome e propósito mudam). Substitui, no discovery ("Definições sem ADR"), os namespaces `FiscoProjects.*`. Onde os ADRs anteriores citam nomes com `FiscoProjects` ou `fiscoprojects` (projetos, cookie da sessão, medidores, tipos de erro), vale o nome novo; o texto deles continua como foi aprovado.

## Contexto e problema

Com a marca GerenWare nas telas, o código continua com o nome antigo: a solução, os projetos e os namespaces, os tipos de erro da API, o cookie da sessão, os medidores, a chave da conexão, a pasta do sistema no armazenamento e o nome usado para cifrar as senhas dos acessos. Alguns desses nomes são só texto no código. Outros estão gravados em dados: o nome e o propósito da proteção entram na cifra de cada senha, o cookie identifica as sessões abertas e a pasta do sistema guarda os logos. Troca-se tudo, ou só o que não toca dados?

## Fatores de decisão

- Um nome só no produto, para quem lê o código e para quem instala.
- Hoje só existem dados de demonstração, no ambiente `dev` e nas máquinas de desenvolvimento.
- O histórico (ADRs aceitos, changes arquivadas, migrações já aplicadas) continua legível.

## Opções consideradas

- A. Trocar todos os nomes, também os gravados em dados, e gravar de novo os dados de demonstração
- B. Trocar os nomes do código e manter os gravados em dados (proteção das senhas, cookie, pasta do sistema)
- C. Manter os nomes do código

## Resultado da decisão

Opção escolhida: **A**. Marcelo autorizou em 17/11/2026 trocar também o que guarda dados, porque só há dados de demonstração.

| Item | Antes | Depois |
| --- | --- | --- |
| Solução, projetos e namespaces | `FiscoProjects.slnx`, `FiscoProjects.<Modulo>` | `GerenWare.slnx`, `GerenWare.<Modulo>` |
| Tipos de erro da API | `urn:fiscoprojects:problem:…` | `urn:gerenware:problem:…` |
| Cookie da sessão | `__Host-fiscoprojects-session` | `__Host-gerenware-session` |
| Medidores e métricas | `FiscoProjects.*`, `fiscoprojects.emails.*` | `GerenWare.*`, `gerenware.emails.*` |
| Chave da conexão | `ConnectionStrings:FiscoProjects` | `ConnectionStrings:GerenWare` |
| Banco local de desenvolvimento | `Database=FiscoProjects` | `Database=GerenWare` |
| Pasta do sistema no armazenamento (logos) | `FiscoProjects` | `GerenWare` |
| Pasta interna do armazenamento local | `.fiscoprojects` | `.gerenware` |
| Proteção das senhas dos acessos | nome `FiscoProjects.Credentials`, propósito `FiscoProjects.Credentials.Password` | `GerenWare.Credentials` e `GerenWare.Credentials.Password` |
| Pacotes e projeto do Docker | `fisco-projects-web`, `fisco-projects-ci-tools`, `fisco-projects` | `gerenware-web`, `gerenware-ci-tools`, `gerenware` |

Regras:

- **Migrações.** Os namespaces e os nomes de tipo nas migrações e nos instantâneos mudam junto com o código, para o modelo continuar igual ao instantâneo. Os identificadores das migrações e os dados que elas gravam não mudam: a inicial continua gravando o nome "FiscoProjects", e a migração `ProductName` o troca.
- **Senhas dos acessos.** Uma senha cifrada com o nome antigo não abre com o novo. Onde houver dados, os acessos são gravados de novo; no `dev`, pela carga da demonstração depois de o ambiente ser recriado (Marca-6).
- **Sessões.** Quem está com sessão aberta entra de novo.
- **Arquivos.** Os logos enviados antes ficam na pasta antiga do armazenamento e são enviados de novo.
- **O que fica.** Os nomes dos recursos do Azure e do pacote da versão (`fiscoprojects-…`), até a Marca-6; o nome do repositório, até a Marca-7; e o histórico.

### Consequências

- Bom, porque o código, as telas e a marca passam a ter o mesmo nome.
- Ruim, porque quem tem um banco de desenvolvimento antigo precisa recriá-lo ou apontar a conexão nova para ele, e os acessos da demonstração precisam ser carregados de novo.
- Ruim, porque o ambiente `dev` fica sem conexão com o banco depois do merge, até o Terraform ser aplicado com a chave nova; a Marca-6 recria o ambiente.
- Neutro, porque nenhuma instalação de empresa existe ainda.

### Confirmação

- Build sem avisos e os testes de todos os módulos com os nomes novos, inclusive os de arquitetura, que acham os módulos pelo nome `GerenWare.<Modulo>`.
- O contrato da API (`openapi.json`) e os tipos do frontend gerados de novo.
- Os testes da migração `ProductName` conferem que o nome antigo gravado no banco vira o novo.

## Prós e contras das opções

### A. Trocar tudo

- Bom, porque não sobra nome antigo em uso.
- Ruim, porque os dados cifrados e as sessões de antes não valem mais.

### B. Manter os nomes gravados em dados

- Bom, porque nada precisa ser gravado de novo.
- Ruim, porque o nome antigo ficaria para sempre em três lugares, por causa de dados de demonstração.

### C. Manter os nomes do código

- Bom, porque não há trabalho.
- Ruim, porque o produto teria dois nomes.
