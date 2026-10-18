---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0008: Escopo da administração de usuários na v1, com desativação no sistema

## Contexto e problema

Com o acesso controlado pelo Entra ID (ADR-0005) e os papéis no banco (ADR-0006), a tela de Usuários do MVP (papel global, ativar e desativar, adicionar usuário) precisa ser revista. O que entra na v1, e a desativação deve existir também no FiscoProjects ou só no Entra?

## Fatores de decisão

- Autonomia do administrador do FiscoProjects para bloquear acesso sem depender da TI.
- Bloqueio efetivo, sem esperar a sessão expirar.
- Preservar histórico e auditoria.
- Não duplicar o que o Entra já faz.

## Opções consideradas

- A. Desativação também no sistema, com o escopo abaixo
- B. Desativação só no Entra

## Resultado da decisão

Opção escolhida: **A. Desativação também no sistema**, porque permite ao administrador bloquear alguém na hora, sem depender da TI, mantendo o histórico.

Escopo:

| Item | Classificação |
| --- | --- |
| Lista de usuários com busca e filtro por papel e situação | v1 |
| Trocar o papel global, com registro na auditoria | v1 |
| Desativar e reativar no sistema, com bloqueio na requisição seguinte e histórico preservado | v1 |
| Nome e e-mail sempre vindos do Entra, atualizados a cada login, sem edição no sistema | v1 |
| Ver os projetos e as pendências de uma pessoa, para redistribuir quando ela sair | depois |
| Adicionar usuário manualmente | fora (substituído pelo primeiro login e pela busca no diretório, ADR-0005) |
| Importar usuários em lote | fora |
| Excluir usuário | fora (preserva histórico e auditoria; anonimização a pedido, pela LGPD, será tratada no bloco de governança) |

### Consequências

- Bom, porque o administrador bloqueia o acesso imediatamente.
- Bom, porque nenhum dado histórico é apagado.
- Ruim, porque as telas precisam indicar "inativo" onde a pessoa ainda aparece como responsável (chamados, tarefas).
- Ruim, porque a checagem de usuário ativo acontece em toda requisição, não só no login.

### Confirmação

Testes cobrindo: usuário desativado é bloqueado na requisição seguinte, mesmo com sessão aberta; usuário reativado volta a acessar; nome e e-mail são atualizados no login; não existe caminho para criar ou excluir usuário manualmente.

## Prós e contras das opções

### A. Desativação também no sistema

- Bom, porque dá autonomia ao administrador do FiscoProjects.
- Bom, porque o bloqueio vale na hora.
- Ruim, porque exige tratar pessoas inativas nas telas.

### B. Desativação só no Entra

- Bom, porque é mais simples e o Entra continua como única fonte.
- Ruim, porque depende da TI para bloquear alguém.
- Ruim, porque a pessoa mantém acesso até a sessão expirar.
