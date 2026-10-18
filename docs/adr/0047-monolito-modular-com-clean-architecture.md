---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0047: Monolito modular com Clean Architecture em cada módulo

## Contexto e problema

O backend é ASP.NET Core e o frontend é React com TypeScript (ADR-0049). Cada empresa tem a própria instalação (ADR-0003), e o desenvolvimento é feito por uma pessoa. Marcelo estava entre o monolito modular e a Clean Architecture, com a preocupação de que o sistema não vire uma bagunça difícil de manter e de escalar. As duas abordagens respondem a perguntas diferentes: o monolito modular define como o sistema é dividido (por assunto, num único deploy); a Clean Architecture define como o código se organiza por dentro (regras no centro, infraestrutura na borda). Como organizar o backend?

## Fatores de decisão

- Manutenção a longo prazo, sem acoplamento entre assuntos diferentes.
- Clareza: qualquer pessoa entender onde fica cada coisa.
- Um único deploy por empresa.
- Testabilidade (SOLID e testes por caso de uso).
- Possibilidade de separar um módulo no futuro, se necessário.

## Opções consideradas

- A. Monolito modular com Clean Architecture dentro de cada módulo
- B. Clean Architecture única para o sistema todo (Domain, Application, Infrastructure e Web)
- C. Monolito modular sem camadas internas
- D. Microsserviços

## Resultado da decisão

Opção escolhida: **A. Monolito modular com Clean Architecture em cada módulo**, porque une a separação por assunto (fronteiras garantidas pelo compilador) à clareza das camadas da Clean Architecture, num único deploy.

Estrutura:

```
src/
  FiscoProjects.Api/                    host: sobe a aplicação, expõe a API e serve o React
  FiscoProjects.SharedKernel/           o mínimo comum (tipos base, eventos)
  Modules/
    Chamados/
      FiscoProjects.Chamados/           o módulo, em camadas:
        Domain/                           regras de negócio
        Application/                      casos de uso
        Infrastructure/                   EF Core, Graph, e-mail
        Endpoints/                        rotas da API do módulo
      FiscoProjects.Chamados.Contracts/ o que outros módulos podem usar
    Projetos/ Documentos/ Acessos/ Notificacoes/ Auditoria/ Administracao/ ...
web/                                    React + TypeScript
tests/
```

Regras:

- Cada módulo expõe aos outros apenas o projeto `Contracts` (interfaces, dados e eventos de integração); o restante é interno e invisível aos demais módulos.
- Cada módulo é dono das próprias tabelas, num schema próprio do banco; nenhum módulo lê ou altera tabelas de outro.
- Módulos se comunicam por interfaces publicadas em `Contracts` ou por eventos internos.
- Dentro do módulo, as dependências apontam para o centro: `Domain` não depende de nada externo; `Application` depende só de `Domain`; `Infrastructure` e `Endpoints` dependem das camadas internas.
- Cada caso de uso (abrir chamado, mover tarefa...) é uma unidade própria, com seus testes.
- Testes de arquitetura garantem as regras de dependência entre camadas e entre módulos.
- Os processos em segundo plano (envio de e-mails, resumo diário, fechamento automático, prazos) rodam dentro da própria aplicação na v1.
- O host da API serve também o build do React, num único deploy.

### Consequências

- Bom, porque o compilador impede um módulo de acessar o interior de outro.
- Bom, porque cada módulo mostra as camadas da Clean Architecture, facilitando entender onde fica cada coisa.
- Bom, porque um módulo pode virar um serviço separado no futuro sem reescrever as regras.
- Ruim, porque há mais projetos e mais cerimônia do que numa estrutura única.
- Ruim, porque consultas que cruzam módulos precisam passar pelos contratos, não por junções diretas no banco.

### Confirmação

Testes de arquitetura (por exemplo, com NetArchTest ou ArchUnitNET) falham quando: um módulo referencia algo fora do `Contracts` de outro; `Domain` depende de EF Core, Graph ou ASP.NET Core; `Application` depende de `Infrastructure`. Revisão de código: nenhum módulo acessa tabelas de outro schema.

## Prós e contras das opções

### A. Monolito modular com Clean Architecture em cada módulo

- Bom, porque combina isolamento por assunto e clareza de camadas.
- Ruim, porque tem mais projetos.

### B. Clean Architecture única

- Bom, porque é muito conhecida e tem poucos projetos.
- Ruim, porque as camadas viram grandes conjuntos em que os assuntos se misturam com o tempo.

### C. Monolito modular sem camadas

- Bom, porque é simples.
- Ruim, porque cada módulo fica livre para virar bagunça por dentro.

### D. Microsserviços

- Bom, porque cada parte escala separadamente.
- Ruim, porque multiplica deploy, monitoramento e custo sem ganho para um desenvolvedor sozinho e uma instalação por empresa.
