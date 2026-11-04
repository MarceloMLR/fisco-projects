## Why

As novidades da v2 (horas apontadas, contratadas e previstas, carga da equipe, ausências, datas-limite, notas, tipos de dependência) entraram na demonstração quase só no ERP-FIN, e o ambiente `dev` nem as tem nos projetos que já existiam, porque o comando carrega só os projetos que faltam. Marcelo pediu para zerar o `dev` e recarregar com dados que mostrem o sistema como ele está hoje (a pedido de Marcelo, fora do roadmap).

## What Changes

- Horas apontadas também no FARMA-REINF, no REFORMA-26 e mais no TMS-DELTA, para todas as pessoas que trabalham nos projetos, inclusive Beatriz Lima e Rafael Souza, que hoje quase não têm horas.
- Horas contratadas no FARMA-REINF, dentro do saldo; o REFORMA-26, interno, continua sem horas contratadas.
- Horas previstas em tarefas do TMS-DELTA (uma acima, uma perto e uma dentro, que a segunda gerente vê na Lista), do FARMA-REINF e do REFORMA-26, para a Carga da equipe mostrar uma pessoa acima, uma perto do limite, pessoas dentro e uma semana livre.
- Ausências de Carla, Beatriz e Marina, registradas pela gerente de outros projetos, uma delas durante uma tarefa da pessoa, sem se cruzar com as que já existem.
- Datas-limite (uma estourada), notas e uma dependência "começa junto" fora do projeto principal; termo de aceite no FARMA-REINF; acessos no FARMA-REINF e no SPED-27; links e documentos no SPED-27.
- Testes do plano para as regras que a API confere (ausências sem se cruzar por pessoa, só pela gerente do projeto e para quem está nele; horas previstas só em tarefas do próprio cronograma).

## Capabilities

### New Capabilities

### Modified Capabilities
- `dados-de-demonstracao`: as novidades da v2 também fora do projeto principal.

## Impact

- `web/scripts/demo/plan.ts` e os testes de `web/scripts/demo`. Nenhuma mudança na API, nas telas nem no comando.
- Depois do merge, o banco do `dev` é apagado e recriado (operação de Marcelo autorizada no chat, fora do repositório), mantendo o aplicativo do Teams e as Configurações gerais, e a publicação carrega a demonstração nova.
