# Proposta: Iniciar atendimento assume o chamado (Chamados-3.1)

## Why

Hoje "Iniciar triagem" e "Iniciar atendimento" só mudam o status: o chamado em andamento pode ficar sem responsável até alguém escrever na conversa ou atribuí-lo. Marcelo pediu que "Iniciar atendimento" torne responsável quem clica, e que "Iniciar triagem" não mude nada (ADR-0098, novo).

## What Changes

- **"Iniciar atendimento"** num chamado sem responsável torna responsável quem fez a ação, se essa pessoa pode ser responsável (quem atende o time ou o gerente do projeto), e a faz acompanhar o chamado, com o evento na conversa; quem acompanha recebe o aviso da mudança de status, como hoje.
- **Com responsável**, nada muda; **o administrador de fora do projeto** não vira responsável.
- **"Iniciar triagem"** continua sem mexer no responsável.
- **Demonstração**: um chamado é levado à triagem e depois ao atendimento pelos botões, ficando com responsável.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: o responsável definido por "Iniciar atendimento".
- `dados-de-demonstracao`: um chamado com o atendimento iniciado pelo botão.

## Impact

- `ChangeTicketStatus` (Tickets), com testes de unidade e da API; a tela já invalida o chamado depois da ação.
