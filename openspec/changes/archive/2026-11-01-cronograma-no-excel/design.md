## Context

A aba Cronograma já recebe do `GET /schedule` tudo o que a planilha precisa: as fases e os itens na ordem da tela, os responsáveis pelo nome, as datas, a duração em dias úteis, o progresso, o atraso (`late`, `lateDays`), as dependências (`predecessorIds`), o caminho crítico (`critical`) e a linha de base (`baselineStart`, `baselineEnd`, `baselineDeviation`). A Auditoria e as Horas dos projetos já geram planilhas no navegador pelo gerador próprio de `.xlsx` (`web/src/format/xlsx.ts`, ADR-0099), com as cores dos tokens e o `saveFile`. O "⋮" do cronograma (`ScheduleBaseline.tsx`) tem o PDF para todos e o XML só para a gestão (ADR-0136).

## Goals / Non-Goals

**Goals:**

- Uma planilha que se filtra e se ordena no Excel sem retrabalho: uma linha por fase e item, datas como datas, números como números.
- O mesmo conteúdo que a pessoa já vê na tela, sem dado a mais (sem e-mails).

**Non-Goals:**

- Rota nova na API ou geração no servidor.
- Desenhar as barras na planilha.
- Escolher colunas ou período.

## Decisions

- **Montada no navegador, sem ADR novo.** Segue o ADR-0099, como a Auditoria e as Horas dos projetos. O ADR-0021 já previa o Excel para depois da v1, e não há alternativa real a decidir.
- **Para todas as pessoas do projeto.** A planilha mostra o que a aba já mostra a quem vê o cronograma, como o PDF; só o XML, que leva os e-mails, fica com a gestão (ADR-0136). Também com o projeto encerrado. Desabilitada com o cronograma vazio, como o PDF.
- **Aba "Cronograma"**, com o cabeçalho fixo e o filtro do Excel, nesta ordem:
  - Nº: a linha, contando as fases, como o MS Project numera;
  - Fase; Nome, em negrito na fase e recuado na subtarefa; Tipo (Fase, Tarefa, Subtarefa ou Marco);
  - Responsável: a pessoa, o cliente pelo `ownerText`, ou vazio;
  - Início, Fim (datas do Excel, dd/mm/aaaa) e Duração (dias úteis; vazio no marco);
  - Progresso (porcentagem do Excel); Situação, o mesmo texto da tela (a tarefa concluída, atrasada, em andamento ou não iniciada; o marco atingido, atrasado ou previsto); Atraso (dias úteis), vazio sem atraso;
  - Depende de: os números das linhas das predecessoras, separados por "; ";
  - Caminho crítico: "Sim" ou vazio;
  - Início e fim na linha de base e a diferença em dias úteis, com o sinal, vazios sem linha de base.
- **Aba "Projeto"**: pares de rótulo e valor com o código, o nome, o cliente, o gerente, o período, o progresso, as tarefas e os marcos atrasados, a data em que a linha de base foi salva e quando a planilha foi gerada (no fuso da instalação, pelo `toSpreadsheetDateTime`).
- **Gerador.** O `xlsx.ts` ganha três células: o dia sem hora (`{ day }`, número de série do Excel com o formato dd/mm/aaaa, a partir do `yyyy-mm-dd` sem fuso), a porcentagem (`{ percent }`, de 0 a 1 com o formato 0%) e o texto recuado (`{ indented }`, com o recuo de alinhamento do Excel).
- **Nome do arquivo:** `{código} - Cronograma.xlsx`. O código do projeto só tem letras, números e hífen, e o nome do projeto pode ter caracteres que o sistema de arquivos recusa.
- **Item do menu.** "Exportar para Excel", logo depois de "Exportar em PDF", com o ícone de planilha; baixar não fica em andamento (ADR-0131).

## Risks / Trade-offs

- [A planilha pode ficar diferente da tela se o cronograma mudar enquanto o menu está aberto] → Ela sai do cronograma que a aba mostra naquele momento, como a impressão.
- [Cronograma grande, como o de 1.500 linhas da demonstração de importação] → O gerador já monta milhares de linhas da Auditoria no navegador; a planilha do cronograma tem no máximo 2.000 itens.
