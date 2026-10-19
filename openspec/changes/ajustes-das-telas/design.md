# Design: ajustes das telas (Cronograma-4.1)

## Context

São seis ajustes pequenos que Marcelo pediu ao conferir a Linha do tempo, os dados de demonstração e as subtarefas. Todos seguem os ADRs e as telas aprovadas; nenhum muda o modelo de dados. Só os dias de atraso mexem no backend, porque o servidor é a fonte da verdade das datas e dos dias úteis (ADR-0051).

## Goals / Non-Goals

**Goals:**

- Dizer há quantos dias úteis cada tarefa e marco atrasado está atrasado, com o número calculado pela API.
- Tirar a barra de rolagem das abas do projeto, deixar a Linha do tempo reabrir o detalhe e o CNPJ dizer o motivo exato.
- Deixar as subtarefas fáceis de achar e de criar, na Lista e no detalhe.
- Levar quem carrega os dados de demonstração às telas atualizadas (5173).

**Non-Goals:**

- Dias de atraso da fase e do projeto, recolher fases na Lista e o redirecionamento da porta 5175 (proposta).

## Decisions

### 1. O atraso conta os dias úteis depois do fim, até hoje

`ScheduleItem.DaysLate(today, days)` devolve `CountBusinessDays(End + 1, today)` quando o item está atrasado, e zero quando não está. A tarefa que terminou na quinta 15/10 tem, na segunda 19/10, 2 dias úteis de atraso (16/10 e 19/10): o dia de hoje conta, porque é mais um dia em que a tarefa devia estar pronta. O fim de semana e os feriados não contam, como em todo o cronograma (ADR-0018).

Quando o atraso começou num dia não útil (fim na sexta, hoje sábado), o item está atrasado e a conta dá zero: nenhum dia útil foi perdido ainda. A tela mantém "Atrasada" ou "Atrasado" nesse caso, em vez de inventar "1 dia útil" ou esconder o atraso.

- **Alternativa:** contar dias corridos. Recusada: prazos e durações do sistema são sempre em dias úteis, e as telas aprovadas dizem "2 dias úteis de atraso".
- **Alternativa:** calcular no navegador com os `nonWorkingDays`. Recusada: o cálculo de dias úteis fica num lugar só, no servidor (ADR-0051, regra de não duplicar).

A tarefa-resumo usa o maior atraso das subtarefas (`ScheduleFigures.LateDaysOf`): ela já fica atrasada quando uma subtarefa está (ADR-0088), e o próprio fim dela pode estar no futuro.

O `GetSchedule` já lê o calendário para os `nonWorkingDays`; o mesmo calendário serve para os dias de atraso, sem outra consulta. A lista de projetos e o cabeçalho continuam sem o número, só com as contagens.

### 2. Onde o atraso aparece

O texto é o das telas aprovadas: "1 dia útil de atraso" e "N dias úteis de atraso", pelo `DueDate` em estado `overdue` (ícone, cor e texto, ADR-0065).

- **Lista:** a tarefa mostra o atraso embaixo do fim, em duas linhas, como o canvas Projetos faz com o fim e o desvio; o marco mostra o atraso no estado, no lugar de "Atrasado". As colunas Fim e Progresso ficam um pouco mais largas para o texto caber; a fase continua com "Atrasada".
- **Detalhe:** o cabeçalho da tarefa e o estado do marco mostram o mesmo texto.
- **Linha do tempo:** a coluna do progresso é estreita; o ícone de atraso diz o número na dica (`titleAccess`), e o marco mantém "Atrasado" com a dica.

### 3. A Linha do tempo esquece a linha ao fechar o detalhe

O Syncfusion mantém a linha escolhida depois que o detalhe fecha, e um clique na barra de uma linha já escolhida não dispara `rowSelected`; por isso a mesma barra não abria o detalhe de novo. O `ScheduleGantt` passa a chamar `clearSelection()` quando o `selectedId` deixa de existir, e a aba passa a informar também a fase aberta (`selectedId` com a fase), como a Lista já faz. Assim a linha só fica marcada enquanto o detalhe ou a fase estão abertos.

- **Alternativa:** usar o clique na barra (`onTaskbarClick`) além da seleção. Recusada: duplicaria o caminho de escolha e não resolveria o clique no nome.

### 4. Abas sem rolagem vertical

O traço da aba escolhida ficava 1 px abaixo da linha das abas (`bottom: -1`), e o `overflow-x: auto` da linha faz a vertical também rolar. A linha das abas passa a ter `overflow-y: hidden` e `margin-bottom: -1px`, e o traço fica em `bottom: 0`: ele continua sobre a linha do cabeçalho, sem nada para rolar.

### 5. CNPJ com o motivo

`cnpjProblem()` em `format/cnpj.ts` diz qual regra falhou: `incomplete` (menos de 14 caracteres), `repeated` (todos iguais) ou `digits` (os dois últimos não batem, ou não são números). O `ClientDialog` mostra a mensagem de cada um; a regra continua a mesma do `Cnpj` do módulo Projects, e a API segue recusando com 400.

### 6. Subtarefas na Lista e no detalhe

- **Seta na Lista:** a tarefa-resumo ganha um botão de seta antes do nome (`aria-expanded`, "Recolher as subtarefas de …" e "Mostrar as subtarefas de …"). O clique e o Enter no botão não chegam à linha, que continua abrindo o detalhe. O que está recolhido fica só na memória da tela, e tudo começa à mostra. Para o nome da tarefa-resumo ficar alinhado com as outras, o recuo dos itens passa a 24 px (`--space-6`), a largura do botão, e o das subtarefas a 48 px.
- **Seção Subtarefas no detalhe:** para uma tarefa direto na fase, a seção lista as subtarefas, cada uma um botão que leva o detalhe a ela, e tem o "Nova subtarefa" para quem edita, do tamanho dos outros botões das seções (Iniciar, Concluir). Quem só vê o cronograma vê a seção quando há subtarefas, sem o botão. A linha "Subtarefas: N" sai do detalhe de leitura, porque a seção já mostra quais são.
- **Rodapé do detalhe:** "Nova subtarefa" sai de perto do "Salvar", e "Excluir" passa ao tamanho padrão, o mesmo do "Salvar".

### 7. Endereço das telas na carga de demonstração

O `loadDemo` termina, carregando ou não, com "Abra as telas em http://localhost:5173 (npm run dev)". A 5175 continua sendo a API e serve o último `npm run build`, que pode estar velho; a 5173 mostra sempre o código atual.

## Risks / Trade-offs

- **[Duas linhas na célula do fim]** → A linha da Lista tem 48 px e cabe a data mais o atraso (20 + 18 px), como no canvas Projetos.
- **[A largura das colunas]** → Fim de 172 para 196 px e Progresso de 148 para 176 px; a 1440 px ainda cabem todas as colunas, e abaixo disso a Lista já rola para o lado dentro da tabela.
- **[`clearSelection` num componente de terceiros]** → Coberto por um teste do `ScheduleGantt` e conferido no navegador; se a API do Syncfusion mudar, o teste quebra.
