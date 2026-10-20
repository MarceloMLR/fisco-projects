# Proposta: linha de base e caminho crítico (Cronograma-5)

## Why

Com as dependências empurrando as tarefas (ADR-0019), o cronograma perde a informação de quanto o projeto escorregou desde o plano aprovado. O gerente também não vê quais tarefas, se atrasarem, atrasam o fim do projeto. O ADR-0021 pôs na v1 a linha de base (uma por projeto, salva e atualizada pelo gestor, com os dias de diferença por tarefa) e o caminho crítico destacado na Linha do tempo. O ADR-0071 definiu como eles aparecem: o menu "Exibir" liga e desliga os dois, e o menu "⋮" traz a linha de base.

## What Changes

- **Linha de base** (ADR-0021, ADR-0043):
  - o administrador e o gerente salvam a linha de base do projeto (`PUT /api/projects/{id}/schedule/baseline`), uma cópia das datas de cada tarefa e marco e do início e fim do projeto, com o dia em que foi salva;
  - salvar de novo atualiza a linha de base, depois de confirmar na tela;
  - o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403; projeto encerrado, 409; cronograma sem tarefas nem marcos, 400.
- **Diferença em dias úteis:**
  - cada tarefa e marco com linha de base diz quantos dias úteis o fim está depois (positivo) ou antes (negativo) do fim na linha de base;
  - o cronograma diz o mesmo do fim do projeto;
  - o que foi incluído depois da linha de base não tem linha de base.
- **Caminho crítico** (ADR-0021, ADR-0051), calculado no servidor:
  - as tarefas e os marcos com datas próprias sem folga até o fim do projeto, pelas dependências "termina, então começa" e pelos dias úteis do calendário da instalação;
  - a tarefa-resumo não entra; as subtarefas, sim (ADR-0088).
- **API:**
  - o `GET /schedule` passa a trazer, em cada item, `critical`, `baselineStart`, `baselineEnd` e `baselineDeviation`;
  - no cronograma, `baseline` (dia em que foi salva, início, fim e a diferença do fim).
- **Tela** (ADR-0067, ADR-0071):
  - **"Exibir":** liga e desliga a linha de base e o caminho crítico na Linha do tempo; no Expandir, as duas caixas ficam no cabeçalho;
  - **"⋮" ("Linha de base e exportação"):** traz "Salvar linha de base" ou "Atualizar linha de base", para quem edita;
  - **Linha do tempo:**
    - a linha de base aparece como uma barra fina cinza embaixo de cada barra;
    - o caminho crítico tem as barras em vermelho, com a parte concluída ainda em verde;
    - a legenda ganha os dois;
  - **resumo:** ao lado do progresso, a diferença do fim do projeto contra a linha de base;
  - **detalhe de uma tarefa ou marco:** a seção Linha de base, com as datas salvas e a diferença.
- **Auditoria:** salvar e atualizar a linha de base entram na auditoria como "Linha de base", com o dia, o início e o fim antes e depois. A tela de Auditoria ganha o tipo no filtro (ADR-0039, ADR-0084).
- **Dados de demonstração:** o ERP-FIN ganha a linha de base, salva antes de uma tarefa escorregar, para a diferença e o caminho crítico aparecerem (ADR-0089).

## Non-goals

- Mais de uma linha de base por projeto, ou o histórico delas: o ADR-0021 decidiu uma, atualizada pelo gestor; a auditoria guarda quem salvou e quando.
- A diferença contra a linha de base na lista de Projetos e na Visão geral: entram na Síntese-2 (`visao-geral-do-projeto`, ADR-0073), que já trata do "Está no prazo?".
- Uma coluna da linha de base na Lista: a diferença de cada item fica no detalhe, e a Lista já usa toda a largura.
- O caminho crítico na Lista ou no detalhe: o ADR-0021 o destaca na Linha do tempo.
- Desfazer e exportar: Cronograma-6 e Cronograma-7. O menu "⋮" nasce agora só com a linha de base.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: quem salva a linha de base, a linha de base e a diferença por item e do projeto, o caminho crítico, os dois na tela e na auditoria.
- `dados-de-demonstracao`: o ERP-FIN com a linha de base e uma tarefa escorregada.

## Impact

- **Backend (`Projects`):**
  - `ScheduleBaseline`, com as datas de cada item (`ScheduleBaselineItem`), no agregado `Schedule`, e a migração das duas tabelas;
  - `ScheduleCriticalPath`, que calcula o caminho crítico pelo `IBusinessDays`;
  - o caso de uso `SaveScheduleBaseline` e a rota, com a matriz de permissões;
  - o `GetSchedule` e as respostas com o caminho crítico e a linha de base;
  - a auditoria com a linha de base pelo nome do projeto.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:**
  - a barra da aba Cronograma ("Exibir" e "⋮") e o Expandir;
  - o `ScheduleGantt`, com a linha de base e as barras do caminho crítico, e a legenda;
  - o resumo e o detalhe com a linha de base;
  - os textos da Auditoria;
  - os testes.
- **Dados de demonstração:** `web/scripts/demo/plan.ts` e `load.ts`.
- **Documentação:** `CLAUDE.md`, glossário (Linha de base, Diferença, Caminho crítico, Folga) e roadmap.
