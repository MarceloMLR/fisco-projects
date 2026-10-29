---
status: aceita
date: 2026-10-28
decision-makers: Marcelo Ribeiro
---

# ADR-0126: Recálculo das datas dos cronogramas pelo calendário, pedido pelo administrador

Complementa o ADR-0018 (cronograma em dias úteis) e o ADR-0019 (dependências que empurram).

## Contexto e problema

Cada tarefa e marco guarda o início, a duração em dias úteis e o fim, calculados quando o item é incluído, alterado ou empurrado. Quando o administrador inclui um feriado, desmarca um nacional ou considera um ponto facultativo, essas datas não mudam: o item só segue o calendário novo na próxima alteração. Enquanto isso, o atraso, o caminho crítico, o previsto e o sombreado da linha do tempo, calculados na leitura, já usam o calendário novo, e uma tarefa pode terminar num feriado. Em 28/10/2026, Marcelo perguntou se a mudança vale para todos os cronogramas. Como os cronogramas em aberto passam a seguir o calendário atual?

## Fatores de decisão

- O servidor é a fonte da verdade das datas (ADR-0051), e a regra de dias úteis fica num lugar só.
- Mudar as datas de vários projetos de uma vez é uma decisão de quem administra, que precisa ver antes o tamanho dela.
- O que já foi concluído não deve ser reescrito.
- Os responsáveis precisam saber quando as datas das tarefas deles mudam (ADR-0037).
- O `Calendar` não conhece o `Projects` (ADR-0047).

## Opções consideradas

- A. Ao salvar uma mudança no calendário, a tela mostra quantas tarefas e marcos de projetos em aberto mudam de datas e oferece "Recalcular as datas".
- B. Recalcular sozinho todos os cronogramas a cada mudança do calendário.
- C. Manter as datas guardadas e recalcular cada item só na próxima alteração dele.

## Resultado da decisão

Opção escolhida: **A. O administrador vê e pede o recálculo**, escolhida por Marcelo, porque ele vê quantos itens de quais projetos mudam antes de mudar, pode fazer várias mudanças no calendário e recalcular uma vez, e nada muda sem ele pedir.

Regras:

- **O que muda.** Nos projetos não concluídos nem cancelados, cada tarefa e marco com datas próprias que não está concluído (tarefa abaixo de 100%, marco não atingido) mantém o início, passado ao dia útil seguinte quando deixou de ser útil, e a duração em dias úteis, e começa depois das anteriores, como em toda mudança. Como no ADR-0019, uma tarefa que termina antes não puxa as seguintes. As tarefas-resumo voltam a seguir as subtarefas (ADR-0088).
- **O que não muda.** As tarefas a 100%, os marcos atingidos, a linha de base, os cronogramas de projetos encerrados e os prazos dos chamados, calculados na abertura (ADR-0025).
- **Aviso.** A tela Calendário e feriados confere, ao abrir e depois de cada mudança gravada, quantos itens de quais projetos mudam com o calendário atual, e mostra o aviso com "Recalcular as datas" enquanto houver algum. A conferência é o próprio recálculo numa cópia: não há marca de "calendário mudou" guardada.
- **Só o administrador**, que é quem muda o calendário (ADR-0046), pede a conferência e o recálculo, depois de confirmar num diálogo com os projetos.
- **Gravação.** Um projeto por vez, cada um na transação serializável do cronograma; pedir de novo completa o que uma falha deixou de fora e não muda o que já segue o calendário.
- **Rastros.** A auditoria registra a alteração de cada item pelo administrador (ADR-0039), e os responsáveis das tarefas que mudam de datas são avisados como numa mudança do cronograma (ADR-0037). O recálculo não entra no desfazer: a mudança de alguém que ele alterou depois fica bloqueada, como quando outra pessoa muda os mesmos itens (ADR-0091).

### Consequências

- Bom, porque os cronogramas em aberto voltam a seguir o calendário com um passo, sem editar tarefa por tarefa.
- Bom, porque o administrador vê o tamanho da mudança antes e decide quando fazer.
- Bom, porque a regra das datas continua num lugar só, no domínio do cronograma.
- Ruim, porque, até o recálculo, as datas guardadas e o que é calculado na leitura podem divergir; o aviso fica na tela até lá.
- Ruim, porque o desfazer de mudanças anteriores aos itens recalculados fica bloqueado.
- Neutro, porque uma mudança retroativa do calendário pode mudar o fim de tarefas em andamento que atravessam o período.

### Confirmação

Testes do domínio: o feriado novo no meio de uma tarefa empurra o fim e as seguintes; o marco num feriado novo passa para o dia útil seguinte; o feriado desconsiderado antecipa o fim sem puxar as seguintes; os concluídos não mudam; as tarefas-resumo seguem as subtarefas; um cronograma em dia não muda. Testes da API: a conferência não muda nada, o projeto encerrado fica de fora, o responsável é avisado e a auditoria registra, só o administrador pede, recalcular de novo não muda nada e o desfazer bloqueado. Testes de tela: o aviso depois de mudar o calendário, o recálculo com confirmação e a tela sem aviso.

## Prós e contras das opções

### A. O administrador vê e pede o recálculo

- Bom, porque nada muda sem a decisão de quem administra.
- Ruim, porque pede um passo a mais depois de mudar o calendário.

### B. Recálculo automático

- Bom, porque as datas seguem o calendário sem nenhum passo.
- Ruim, porque um clique numa caixa do calendário muda, sem aviso, as datas de todos os projetos em aberto e avisa os responsáveis de cada tarefa.

### C. Só na próxima alteração

- Bom, porque não muda nada que ninguém pediu.
- Ruim, porque as datas guardadas ficam divergentes do calendário por tempo indefinido, inclusive tarefas terminando em feriados.
