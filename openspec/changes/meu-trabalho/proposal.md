# Proposta: Meu trabalho (Síntese-1)

## Why

A tela inicial ainda está vazia: quem entra não vê o que depende dele sem abrir Chamados, cada cronograma e cada aba de termos. O ADR-0070 decidiu o Meu trabalho como uma fila única por urgência, com a saúde dos projetos da pessoa e os próximos marcos:

- **Fila:** os chamados, as tarefas e os termos de aceite da pessoa, nos grupos Atrasado, Aguardando você, Hoje e o próximo dia útil, e Próximos 7 dias;
- **Ação:** uma por linha (Concluir, Abrir, Responder, Iniciar, Atualizar; Atribuir só para o gerente e o líder técnico);
- **Visões:** "Para mim", "Abertos por mim" e "Que acompanho";
- **Meus projetos:** a faixa de fases (ADR-0071), a porcentagem concluída e o atraso em dias úteis;
- **Próximos marcos:** a data em destaque e os dias úteis que faltam;
- **Sem atalhos:** nada de "Abrir chamado" nem "Novo projeto" na tela inicial.

O ADR-0096 (novo) decide o que o ADR-0070 não decide: a fila montada no servidor por um módulo próprio, o que entra em cada visão, a janela de 7 dias corridos e o atraso do projeto pelo item mais atrasado.

## What Changes

- **Módulo `MyWork`** (sem tabelas, ADR-0096):
  - o contrato `IMyWorkSource`, que o `Tickets`, o `Projects` e o `Documents` implementam, cada um com os itens da pessoa e as próprias regras de quem vê e quem atende;
  - a regra de urgência num lugar só: o grupo de cada item pelo prazo, com o calendário da instalação, a ação de cada linha e a ordem;
  - `GET /api/my-work`, com as três visões, a contagem de cada uma e os grupos.
- **Itens de cada módulo:**
  - `Tickets`: os chamados de que a pessoa é responsável, os que ela abriu, os que acompanha e os sem responsável dos times dela, com o prazo da primeira resposta ou da resolução;
  - `Projects`: as tarefas da pessoa abaixo de 100%, com a fase e o atraso;
  - `Documents`: os pedidos de aceite pendentes para a pessoa e os que ela fez, pelo dia do marco.
- **Projetos:**
  - a lista de projetos passa a dizer o atraso de cada cronograma em dias úteis, o do item mais atrasado;
  - `GET /api/projects/upcoming-milestones`: os próximos marcos dos projetos abertos da pessoa, com os dias úteis que faltam e o aceite pedido;
  - o endereço do cronograma com `?item=` abre os detalhes da tarefa, para onde a fila leva.
- **Na tela** (canvas Main e CelularMeuTrabalho):
  - o Meu trabalho na rota raiz, com a data de hoje, as visões no `SegmentedControl` e a fila;
  - Concluir, Iniciar e Atualizar mudam o progresso ali mesmo, e Atribuir abre o diálogo de troca do responsável;
  - os cartões "Meus projetos" e "Próximos marcos";
  - as seções vazias com mensagem;
  - no celular, a fila em linhas de duas camadas, seguida dos projetos e dos marcos.
- **Demonstração:** itens na fila das pessoas da demonstração.

## Capabilities

### New Capabilities

- `meu-trabalho`: a fila por urgência, as visões, as ações, os projetos da pessoa, os próximos marcos e a tela.

### Modified Capabilities

- `projetos`: o atraso em dias úteis na lista de projetos e os próximos marcos.
- `cronograma`: o endereço que abre os detalhes de uma tarefa.
- `dados-de-demonstracao`: itens na fila das pessoas da demonstração.

## Impact

- **Backend:** o módulo novo `MyWork` (`FiscoProjects.MyWork` e `FiscoProjects.MyWork.Contracts`), registrado no host; as fontes no `Tickets`, no `Projects` e no `Documents`; a rota dos próximos marcos e o atraso na lista de projetos; um contrato novo no `IProjectAcceptances` para os marcos com aceite pedido; o dia de hoje da instalação no `Administration.Contracts`.
- **Frontend:** a página Meu trabalho em `web/src/my-work`, o diálogo de progresso reaproveitado do cronograma e o `?item=` do cronograma.
- **Documentação:** ADR-0096, CLAUDE.md, glossário e roadmap.

## Non-goals

- Indicadores gerenciais, atividade recente e personalizar o painel (depois ou fora, ADR-0070).
- Atalhos "Abrir chamado" e "Novo projeto" na tela inicial (fora, ADR-0070).
- Itens com prazo depois dos 7 dias, que continuam nas telas de cada módulo (ADR-0096).
- Marcos na fila: eles aparecem em "Próximos marcos" e contam no atraso do projeto.
