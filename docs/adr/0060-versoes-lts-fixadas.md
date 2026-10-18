---
status: aceita
date: 2026-10-16
decision-makers: Marcelo Ribeiro
---

# ADR-0060: Versões LTS fixadas (.NET 10 e Node LTS), atualizadas só quando necessário

## Contexto e problema

Os números de versão ficam nos arquivos do projeto (`global.json`, `.nvmrc`, versão exigida no Terraform); este ADR registra a política. O .NET 10 é LTS, com suporte até novembro de 2028; o .NET 11, previsto para novembro de 2026, é de suporte curto. O Node 26 passa a LTS em 28 de outubro de 2026, com suporte até abril de 2029; o Node só roda no build do React e no CI, não em produção. Qual política de versões seguir?

## Fatores de decisão

- Estabilidade e tempo de suporte.
- Pouco trabalho de atualização para um desenvolvedor sozinho.
- Compatibilidade com componentes de terceiros (por exemplo, Syncfusion).
- Correções de segurança.

## Opções consideradas

- A. Sempre a LTS mais recente, migrando a cada nova LTS
- B. Sempre a versão mais nova, inclusive as de suporte curto
- C. Fixar a versão e atualizar só quando necessário

## Resultado da decisão

Opção escolhida: **C. Fixar a versão e atualizar só quando necessário**, partindo das LTS mais recentes, por decisão de Marcelo.

Regras:

- .NET 10 (LTS).
- Node na LTS mais recente no início do código: Node 26, LTS a partir de 28 de outubro de 2026 (antes disso, Node 24).
- Bibliotecas na versão estável mais recente no momento em que entram no projeto.
- "Quando necessário" inclui: correções de segurança (versões de correção dentro da mesma versão principal, aplicadas pelos alertas de segurança do Dependabot), fim de suporte próximo (.NET 10 em novembro de 2028; Node 26 em abril de 2029) ou um recurso realmente necessário.
- Toda troca de versão principal é uma mudança própria no OpenSpec.
- Os números ficam em `global.json`, `.nvmrc`/`engines` do `package.json` e na versão exigida pelo Terraform.

### Consequências

- Bom, porque o projeto fica estável, sem migrações frequentes.
- Bom, porque começar em versões LTS dá até 2028 (.NET) e 2029 (Node) de suporte.
- Ruim, porque recursos novos da plataforma demoram a chegar.
- Ruim, porque, sem atualização periódica, a migração antes do fim do suporte tende a ser maior.

### Confirmação

O `global.json` fixa o SDK do .NET 10; o `.nvmrc` e o campo `engines` fixam o Node; o Dependabot está configurado para alertas e correções de segurança; o fim de suporte de cada plataforma está anotado no planejamento.

## Prós e contras das opções

### A. LTS mais recente, migrando a cada nova LTS

- Bom, porque mantém o projeto sempre suportado.
- Ruim, porque exige migração a cada dois anos.

### B. Sempre a mais nova

- Bom, porque traz recursos novos logo.
- Ruim, porque exige migração todo ano e esbarra na certificação de componentes.

### C. Fixar e atualizar quando necessário

- Bom, porque reduz o trabalho de atualização.
- Ruim, porque acumula diferença até a próxima migração.
