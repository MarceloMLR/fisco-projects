---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0007: Somente usuários da empresa na v1, com contas de convidado recusadas

## Contexto e problema

Pessoas de fora da empresa (clientes, fornecedores) poderiam precisar abrir chamados ou responder a termos de aceite. Além disso, pelo ADR-0005, se a empresa não ligar a atribuição obrigatória no Entra ID, qualquer convidado já existente no tenant (por exemplo, alguém chamado para o Teams) conseguiria entrar no FiscoProjects. Usuários externos devem ter acesso na v1?

## Fatores de decisão

- Segurança: não expor dados a terceiros antes de a matriz de permissões estar madura.
- Fechar a brecha de convidados do tenant entrando sem intenção.
- Coerência com o ADR-0004 (sem senhas no sistema).
- Esforço da v1.

## Opções consideradas

- A. Somente pessoas da empresa na v1; contas de convidado recusadas no login
- B. Convidados do Entra (B2B) já na v1, com papel restrito
- C. Portal externo com login próprio

## Resultado da decisão

Opção escolhida: **A. Somente pessoas da empresa na v1**, porque é a mais simples, fecha a brecha dos convidados e evita expor dados a terceiros antes de o catálogo de papéis e a matriz de permissões estarem decididos.

Regras:

- No login, o FiscoProjects identifica contas de convidado do tenant e recusa o acesso com mensagem clara.
- Termos assinados por clientes externos continuam possíveis: o gestor anexa o termo recebido por fora.
- O acesso de externos volta como novo ADR depois da decisão sobre papéis e permissões; o caminho preferido é o de convidados do Entra (opção B).

### Consequências

- Bom, porque nenhum terceiro acessa dados da empresa na v1.
- Bom, porque convidados existentes no tenant não entram por acidente, mesmo sem atribuição obrigatória.
- Ruim, porque clientes não abrem chamados nem aceitam termos diretamente no sistema.

### Confirmação

Teste cobrindo: conta de convidado autenticada no Entra é recusada pelo FiscoProjects e não gera registro de usuário.

## Prós e contras das opções

### A. Somente pessoas da empresa na v1

- Bom, porque é simples e seguro.
- Bom, porque mantém um único perfil de usuário enquanto as permissões amadurecem.
- Ruim, porque clientes dependem do gestor para registrar aceites.

### B. Convidados do Entra (B2B) na v1

- Bom, porque não exige senhas no sistema e segue o ADR-0004.
- Ruim, porque a TI precisa convidar cada pessoa.
- Ruim, porque toda tela precisa de regras extras para externos.
- Ruim, porque pode haver custo no Entra conforme o volume (a confirmar).

### C. Portal externo com login próprio

- Bom, porque o cliente não precisa de conta Microsoft.
- Ruim, porque cria um segundo caminho de autenticação e contradiz o ADR-0004.
- Ruim, porque aumenta a superfície de ataque.
