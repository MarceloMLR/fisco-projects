## Why

Quem chega ao FiscoProjects costuma ter o cronograma no MS Project e hoje precisa redigitá-lo. O FiscoProjects já exporta para o MS Project, mas o caminho de volta ficou para depois da v1 (ADR-0021). Com a Linha do tempo própria (ADR-0134), Marcelo pediu a importação. Gantt-3, da v2 (ADR-0135).

## What Changes

- O "Novo projeto" ganha a terceira origem do cronograma, "A partir do MS Project", ao lado de "Em branco" e "A partir de um modelo":
  - a pessoa escolhe o arquivo XML do MS Project;
  - confere o início, que vem preenchido com o primeiro dia do arquivo;
  - vê a prévia das fases e dos itens, com as datas que o cronograma vai ter, e a lista do que mudou na conversão;
  - cria o projeto, que nasce com o cronograma, como num modelo.
- A API ganha a prévia (`POST /api/projects/ms-project-preview`), que lê o arquivo e devolve o cronograma convertido e os avisos, sem guardar nada. A criação do projeto (`POST /api/projects`) aceita o cronograma convertido no lugar do modelo.
- O que vem e o que não vem do arquivo:
  - **Vêm:** as fases, as tarefas, as subtarefas, os marcos, a ordem, as durações e as dependências "termina, então começa", com as datas no calendário da instalação.
  - **Não vêm:** os responsáveis, o progresso, a linha de base, os calendários e as restrições.
- A conversão segue o ADR-0135, com um aviso para cada ajuste:
  - níveis abaixo do terceiro sobem para subtarefa;
  - tarefas soltas no primeiro nível entram numa fase com o título do projeto;
  - saem as dependências de outro tipo, as de tarefas-resumo e as que formariam ciclo;
  - saem os intervalos;
  - durações acima de 999 dias úteis ficam em 999;
  - nomes longos são cortados.
- Arquivo que não é XML do MS Project, com DTD, acima de 5 MB, com mais de 2.000 tarefas ou sem tarefas é recusado com a mensagem de salvar como XML.
- A demonstração ganha um projeto importado do XML que a própria exportação grava.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `cronograma`: importar do MS Project, pela API e pela tela.
- `dados-de-demonstracao`: o projeto importado do MS Project na demonstração.

## Non-goals

- Ler o .mpp; ele é salvo como XML no MS Project (ADR-0135).
- Trazer os responsáveis, o progresso, a linha de base, os recursos, os calendários, as restrições e as notas do arquivo.
- Importar num projeto já criado, pela aba Cronograma.
- Outros tipos de dependência, intervalos ou mais níveis no cronograma (ADR-0019, ADR-0088).

## Impact

- `src/Modules/Projects`:
  - o leitor e a conversão (`MsProjectImport`, na `Application`, com `System.Xml.Linq` sem DTD);
  - a prévia e o `CreateProject` com o cronograma convertido;
  - o `Schedule.ApplyTemplate` passa a aceitar o modelo ou o cronograma convertido;
  - os testes, com arquivos de exemplo.
- `web/src/projects`: a terceira origem no `NewProjectPage` e no `ScheduleOriginChoice`, o arquivo, a prévia com os avisos e os textos em `pt-BR.json`; o contrato e os tipos gerados de novo.
- `web/scripts/demo`: o projeto importado do MS Project no plano.
- Documentação: `CLAUDE.md`, `docs/glossario.md`, o `discovery.md` e o contexto do OpenSpec (a importação sai do "não faz").
- ADRs: 0135 (novo), 0017, 0019, 0021 e 0088.
