---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0005: Acesso controlado pelo Entra ID, com criação do usuário no primeiro login

## Contexto e problema

O login do FiscoProjects é pelo Entra ID (ADR-0004). É preciso decidir quem controla quais pessoas conseguem entrar e quando o usuário passa a existir no sistema. O ideia.md previa criar o usuário no primeiro login; o MVP tinha um botão "adicionar usuário". Este ADR trata só de autenticação (quem entra); papéis e permissões (autorização) são decididos em ADR próprio.

## Fatores de decisão

- Uma única fonte de verdade sobre quem pertence à empresa.
- Perda de acesso automática quando a pessoa sai da empresa.
- Funcionar tanto em empresas com governança rígida quanto em empresas pequenas.
- Menos caminhos de código e de teste.

## Opções consideradas

- A. Criação automática no primeiro login, com o controle de entrada no Entra ID
- B. Pré-cadastro na aplicação (convite)
- C. As duas, configuráveis

## Resultado da decisão

Opção escolhida: **A. Criação automática no primeiro login, com o controle de entrada no Entra ID**, porque mantém o Entra como única fonte de verdade, evita cadastro duplicado e funciona para empresas de qualquer porte só com configuração.

Regras:

- Quem consegue entrar é definido no Entra ID. O guia de instalação recomenda ligar a atribuição obrigatória no aplicativo empresarial (por pessoa ou por grupo); empresas pequenas podem deixar o tenant inteiro entrar, sem mudança no código.
- No primeiro login, o FiscoProjects cria o registro do usuário a partir dos dados do token.
- Para incluir em um projeto alguém que ainda não entrou no sistema, o FiscoProjects busca a pessoa no diretório da empresa (Microsoft Graph, leitura básica de usuários) e cria o registro.

### Consequências

- Bom, porque não há cadastro de pessoas duplicado entre o Entra e o sistema.
- Bom, porque quem é desativado no Entra deixa de conseguir entrar.
- Ruim, porque liberar uma pessoa nova depende do administrador do Entra quando a atribuição obrigatória está ligada.
- Ruim, porque a atribuição por grupo exige Entra ID P1 ou P2; no plano gratuito, a atribuição é por pessoa.
- Neutro, porque exige uma permissão adicional no Graph para buscar usuários no diretório (permissão exata a confirmar na implementação).

### Confirmação

Testes cobrindo: criação do usuário no primeiro login; segundo login sem duplicar o registro; inclusão em projeto de pessoa ainda não cadastrada, via busca no diretório (com fake do Graph).

## Prós e contras das opções

### A. Criação automática no primeiro login, controle no Entra

- Bom, porque o Entra é a única fonte de verdade.
- Bom, porque o mesmo código atende empresas com e sem atribuição obrigatória.
- Ruim, porque depende do administrador do Entra para liberar pessoas.

### B. Pré-cadastro na aplicação

- Bom, porque o administrador do FiscoProjects libera pessoas sem depender do Entra.
- Ruim, porque as pessoas passam a ser geridas em dois lugares.
- Ruim, porque a lista do sistema pode ficar desatualizada em relação ao Entra.

### C. As duas, configuráveis

- Bom, porque atende os dois perfis de empresa.
- Ruim, porque dobra caminhos de código e de teste para algo que a opção A resolve por configuração.

## Mais informações

- Gerenciar acesso a aplicativos no Entra ID: https://learn.microsoft.com/entra/identity/enterprise-apps/what-is-access-management
