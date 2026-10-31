## Why

Marcelo importou um arquivo do MS Project com tarefas inativas, linhas em branco e recursos de material e custo e não viu aviso nenhum: os avisos ficam abaixo da área visível do "Novo projeto", e o que fica de fora (as tarefas inativas, as linhas em branco, os recursos que não são pessoas, os recursos sem pessoa) não gera aviso. Quem vem do MS Project precisa saber o que mudou para não se perder no cronograma novo. O ADR-0135 já pedia o aviso das tarefas que ficam sem responsável. Gantt-3.1, correção da Gantt-3, a pedido de Marcelo (itens 1 a 4 aprovados no chat).

## What Changes

- O quadro da conversão aparece sempre que o arquivo é lido, com o resumo no título: quantas tarefas e quantos recursos do arquivo vieram ("14 de 17 tarefas e 3 de 5 recursos vieram do arquivo").
- Ao ler um arquivo novo, a tela rola até o quadro. A prévia ganha o atalho "N ajustes na conversão", que leva até ele.
- Novos avisos da conversão, com os nomes:
  - os recursos de material e de custo, que ficam de fora;
  - os recursos de trabalho que não são o primeiro de nenhuma tarefa trazida, que ficam de fora;
  - as tarefas inativas, que ficam de fora com as linhas abaixo delas;
  - as linhas em branco ou sem nome, que ficam de fora (só a quantidade).
- O aviso das tarefas sem responsável (ADR-0135): o quadro lista os recursos sem pessoa, com as tarefas de cada um, e muda conforme a escolha em "Responsáveis do arquivo". Cada recurso sem pessoa diz, na própria linha, que as tarefas dele ficam sem responsável.
- A prévia da API passa a trazer as quantidades do arquivo e do que veio (`counts`) e a quantidade de cada aviso (`count`).

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `cronograma`: os avisos e as quantidades da importação do MS Project, pela API e pela tela.

## Non-goals

- Guardar os avisos no projeto criado, para ver depois (item 5, não aprovado).
- Mudar o que vem ou não vem do arquivo: as regras de conversão continuam as do ADR-0135.
- Sugerir o cliente para o recurso com o nome "Cliente" ou outro nome genérico.
- Ler o .mpp (ADR-0135).

## Impact

- `src/Modules/Projects`:
  - o leitor guarda os recursos de material e custo (`MsProjectResource` com o tipo) e a conversão (`MsProjectImport`) ganha os quatro avisos novos, a quantidade de cada aviso e as quantidades do arquivo (`MsProjectCounts`);
  - a resposta da prévia (`MsProjectPreviewResponse`) traz `counts` e o `count` de cada aviso;
  - os testes da conversão e da API.
- `web/src/projects`: o quadro da conversão (`MsProjectWarnings`) sempre visível, com o resumo, os recursos sem pessoa e a rolagem; a nota de cada recurso sem pessoa (`MsProjectOwners`); o atalho na prévia (`MsProjectSchedulePreview`); os textos em `pt-BR.json`; o contrato e os tipos gerados de novo; os testes de tela.
- Documentação: `CLAUDE.md` e o roadmap (Gantt-3.1).
