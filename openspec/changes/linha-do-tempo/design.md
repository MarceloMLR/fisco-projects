# Design: linha-do-tempo (Cronograma-3)

## Context

- A Cronograma-1 e a Cronograma-2 criaram o agregado `Schedule`, com as datas calculadas no domínio pelo `IBusinessDays` do `Calendar`, e a aba Cronograma só com a Lista (`ProjectSchedulePage`).
- O ADR-0051 escolheu o Gantt do Syncfusion na v1, isolado num componente próprio (`ScheduleGantt`, ADR-0074), com o servidor como fonte da verdade das datas.
- O ADR-0071 decidiu como a Linha do tempo aparece: ajustada ao espaço, com a escala escolhida, "Hoje", Expandir e a barra de ferramentas do canvas ProjetoCronograma.
- O design system já tem os tokens do Gantt (`gantt-bar`, `gantt-progress`, `gantt-summary`, `gantt-milestone`, `gantt-dependency`, `gantt-today`, `gantt-nonworking` e os da linha de base e do caminho crítico).
- O Syncfusion 35 funciona com o React 19. Os temas vêm num pacote à parte (`@syncfusion/ej2-material3-theme`), com variáveis CSS, e o CSS de base importa a fonte Roboto do Google.

## Goals / Non-Goals

**Goals:**

- A Linha do tempo com o visual do design system, nos temas claro e escuro, sem regra de datas no navegador.
- Arrastar, puxar a borda e ligar barras, sempre confirmados pelo servidor.
- O Syncfusion num ponto só, fácil de trocar pelo Gantt próprio (ADR-0051).

**Non-Goals:**

- Linha de base, caminho crítico, desfazer e exportação (Cronograma-4 a 6).
- Editar nas células da tabela; a edição continua no detalhe (ADR-0081).

## Decisions

### 1. `ScheduleGantt` isolado e carregado sob demanda

- `web/src/schedule-gantt/` é a única pasta que importa `@syncfusion/*`; uma regra `no-restricted-imports` do ESLint reprova o import em qualquer outro lugar (confirmação do ADR-0051).
- A aba carrega o componente com `React.lazy`, então quem fica na Lista não baixa o Syncfusion.
- As propriedades e os eventos são do FiscoProjects:
  - entra o `Schedule` da API, a escala, os dias não úteis, se edita e se é a versão detalhada (Expandir);
  - saem `onChoose(item ou fase)`, `onMove(itemId, start)`, `onResize(itemId, start, end)` e `onLink(predecessorId, successorId)`.
- A conversão do `Schedule` para as linhas do Syncfusion e de volta fica em funções puras (`ganttRows.ts`), testadas sem o Syncfusion.

### 2. Datas sem regra no navegador

- O Syncfusion roda em modo manual (`taskMode="Manual"`, `autoCalculateDateScheduling={false}`), com o dia inteiro como horário de trabalho e os fins de semana incluídos. Assim ele desenha as datas que recebe, sem mover nada.
- Cada dia vira a meia-noite local; o fim de uma tarefa vai até a meia-noite do dia seguinte, e o marco fica no próprio dia.
- Os dias não úteis vêm da API no `GET /schedule` (`nonWorkingDays`), do primeiro dia do mês anterior ao início ao último dia do mês seguinte ao fim, calculados pelo `IBusinessDays.IsBusinessDay`. A tela os passa como `holidays` do Syncfusion, só para o sombreado. Assim o feriado local e o ponto facultativo aparecem sem a tela conhecer a regra.
- Alternativa: mandar ao Syncfusion os dias da semana e os feriados para ele calcular. Foi rejeitada, porque repetiria a regra do calendário no navegador (ADR-0051).

### 3. Arrastar, puxar a borda e ligar, confirmados pelo servidor

- No fim do arrasto, o `taskbarEdited` do Syncfusion é cancelado e vira uma chamada à API:
  - arrastar manda só o início;
  - puxar a borda manda o início e o fim.
- A API recalcula e empurra as seguintes. A tela invalida o cronograma e desenha o que voltou, com a mensagem dos itens empurrados.
- Ligar duas barras cancela a mudança do Syncfusion e manda a alteração da tarefa seguinte com a nova dependência e sem o progresso (ADR-0086). O ciclo volta como o problema `schedule-cycle` e vira uma mensagem.
- Só no computador e só para quem tem `canEditSchedule`; nos outros casos, `allowTaskbarEditing` fica desligado.

### 4. `PUT /schedule/items/{itemId}/dates`

- O corpo é `{ start, end? }`.
- No domínio, `Schedule.Reschedule(itemId, start, end, days, today)` monta o plano a partir do item: mesmo nome, fase, responsável e dependências, sem mudar o progresso. Depois chama a mesma rotina de `ChangeItem`: início num dia útil, depois das anteriores, e as seguintes empurradas.
  - Sem o fim, a duração fica a mesma.
  - Com o fim, a duração é `IBusinessDays.CountBusinessDays(início ajustado, fim)`.
- Fim antes do início, fim num marco ou duração fora de 1 a 999 respondem 400 no campo `end`.
- Alternativa: a tela calcular a duração e usar o `PUT` da tarefa. Foi rejeitada, porque contar dias úteis no navegador repetiria o calendário.

### 5. Escala e ajuste ao espaço

- `ganttLayout.ts` calcula, para a escala e a largura disponível, o início e o fim da vista e a largura da unidade:
  - Dias vai do início ao fim do cronograma;
  - Semanas, da segunda-feira anterior ao domingo seguinte;
  - Meses, do primeiro ao último dia dos meses.
- A largura da unidade é o maior valor entre a largura disponível dividida pelas unidades e o mínimo da escala: 24 px por dia, 48 por semana e 64 por mês. Acima do mínimo não há rolagem; abaixo, a rolagem fica dentro da linha do tempo.
- Um `ResizeObserver` recalcula quando a área muda (janela, menu lateral, Expandir).
- "Hoje" rola a linha do tempo até o dia de hoje.
- As datas da régua são formatadas pela própria tela (`formatter` do Syncfusion) em português, sem os dados CLDR do Syncfusion.

### 6. Visual do design system sobre o tema Material 3

- O tema Material 3 do Syncfusion é a base (ADR-0051), com as variáveis `--color-sf-*` ligadas aos tokens do design system dentro do `ScheduleGantt`. Assim o tema claro e o escuro seguem o MUI sem um segundo tema.
- Uma folha própria (`ScheduleGantt.css`) aplica os tokens do Gantt às barras, ao resumo das fases, aos marcos, às setas, à linha de hoje e aos dias não úteis, com a fonte IBM Plex Sans (ADR-0064), seguindo o `fp-gantt` do design system.
- Um plugin do Vite tira o `@import` da fonte do Google do CSS do Syncfusion. O `frontend.sh` reprova o build que contenha `fonts.googleapis.com` (ADR-0064).
- As dicas (tooltips) do Syncfusion ficam desligadas; o detalhe abre na gaveta.

### 7. Chave da licença (ADR-0087)

- `GET /api/client-settings`, no host da API e com sessão, devolve `{ timelineLicense }` da configuração `Syncfusion:LicenseKey`.
- O `ScheduleGantt` chama `registerLicense` antes da primeira Linha do tempo; sem a chave, aparece o aviso de avaliação do Syncfusion.

### 8. Aba Cronograma

- Uma barra de ferramentas acima das duas vistas:
  - "Linha do tempo" ou "Lista";
  - na Linha do tempo, Dias, Semanas ou Meses e "Hoje";
  - o progresso do projeto e os atrasos;
  - Expandir;
  - "Nova fase" e "Nova tarefa" para quem edita.
- A vista escolhida fica no endereço (`?view=list`), para voltar a ela.
- Abaixo da Linha do tempo fica a legenda de Concluído, A fazer, Fase e Fim de semana e feriado. Caminho crítico e Linha de base entram na Cronograma-4.
- Expandir usa o `Dialog` em tela quase inteira. Leva título, código e nome, escala, "Hoje", "Nova tarefa" e "Recolher", e a tabela com nome, início, fim, responsável e progresso.

### 9. Licenças do frontend

- O `licenses` do `web/package.json` passa a excluir os pacotes `@syncfusion/`, cuja licença comercial comunitária foi aceita pelo ADR-0051.
- Os demais continuam na lista permitida.

## Risks / Trade-offs

- [O Syncfusion é grande (vários MB)] → Carregado só na Linha do tempo, num pedaço separado do build.
- [O visual do Syncfusion pode fugir do design system] → Folha própria com os tokens e conferência nas capturas nos dois temas.
- [O jsdom não desenha o Syncfusion de forma fiel] → A lógica fica em funções puras testadas. A tela é testada com um `ScheduleGantt` simulado, mais um teste do componente real que confere as linhas e a linha de hoje. O ajuste ao espaço é conferido nas capturas em 1440×900 e 1920×1080.
- [Os termos da licença comunitária podem mudar] → O isolamento num componente deixa a troca restrita a ele (ADR-0051).

## Migration Plan

Sem migração de banco. A chave do Syncfusion entra nos segredos do usuário (desenvolvimento) e no Key Vault (produção), conforme `docs/instalacao.md`.
