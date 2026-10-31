## Why

Quem chega ao FiscoProjects costuma ter o cronograma no MS Project e hoje precisa redigitá-lo. O FiscoProjects já exporta para o MS Project, mas o caminho de volta ficou para depois da v1 (ADR-0021). Com a Linha do tempo própria (ADR-0134), Marcelo pediu a importação. Gantt-3, da v2 (ADR-0135).

## What Changes

- O "Novo projeto" ganha a terceira origem do cronograma, "A partir do MS Project", ao lado de "Em branco" e "A partir de um modelo":
  - a pessoa escolhe o arquivo XML do MS Project;
  - confere o início, que vem preenchido com o primeiro dia do arquivo;
  - vê a prévia das fases e dos itens, com as datas que o cronograma vai ter, e a lista do que mudou na conversão;
  - liga cada recurso do arquivo a uma pessoa (sugerida pelo e-mail no diretório), ao cliente ou a ninguém, e escolhe o papel de quem entra no projeto (Colaborador de início);
  - cria o projeto, que nasce com o cronograma, como num modelo.
- A API ganha a prévia (`POST /api/projects/ms-project-preview`), que lê o arquivo e devolve o cronograma convertido e os avisos, sem guardar nada. A criação do projeto (`POST /api/projects`) aceita o cronograma convertido no lugar do modelo.
- O que vem e o que não vem do arquivo:
  - **Vêm:** as fases, as tarefas, as subtarefas, os marcos, a ordem, as durações e as dependências "termina, então começa", com as datas no calendário da instalação, o progresso de cada tarefa e marco e os responsáveis, a pedido de Marcelo.
  - **Não vêm:** a linha de base, os calendários, as restrições, os recursos de material e custo e as notas.
- A conversão segue o ADR-0135, com um aviso para cada ajuste:
  - níveis abaixo do terceiro sobem para subtarefa;
  - tarefas soltas no primeiro nível entram numa fase com o título do projeto;
  - saem as dependências de outro tipo, as de tarefas-resumo e as que formariam ciclo;
  - saem os intervalos;
  - durações acima de 999 dias úteis ficam em 999;
  - nomes longos são cortados.
- Arquivo que não é XML do MS Project, com DTD, acima de 5 MB, com mais de 2.000 tarefas ou sem tarefas é recusado com a mensagem de salvar como XML.
- As pessoas ligadas aos recursos entram no projeto com o papel escolhido, como na inclusão de pessoas, e assumem as tarefas dos recursos delas. O e-mail dos recursos só serve para achar a pessoa e não é guardado.
- A demonstração ganha um projeto importado do XML que a própria exportação grava.
- A exportação para o MS Project passa a levar o e-mail de cada responsável, para a ida e volta sugerir as pessoas, e fica só com o administrador e o gerente do projeto; o PDF continua para todos (ADR-0136, a pedido de Marcelo).

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `cronograma`: importar do MS Project, pela API e pela tela.
- `dados-de-demonstracao`: o projeto importado do MS Project na demonstração.

## Non-goals

- Ler o .mpp; ele é salvo como XML no MS Project (ADR-0135).
- Trazer a linha de base, os calendários, as restrições, os recursos de material e custo e as notas do arquivo.
- Mais de um responsável por tarefa (ADR-0021).
- Importar num projeto já criado, pela aba Cronograma.
- Outros tipos de dependência, intervalos ou mais níveis no cronograma (ADR-0019, ADR-0088).

## Impact

- `src/Modules/Users`: a busca de pessoas pelos e-mails (`Users.Contracts`), sobre os usuários e o diretório da empresa.
- `src/Modules/Projects`:
  - o leitor e a conversão (`MsProjectImport`, na `Application`, com `System.Xml.Linq` sem DTD);
  - as pessoas dos recursos incluídas no projeto, com o papel, na criação;
  - a prévia e o `CreateProject` com o cronograma convertido;
  - o `Schedule.ApplyTemplate` passa a aceitar o modelo ou o cronograma convertido;
  - os testes, com arquivos de exemplo.
- `web/src/projects`: a terceira origem no `NewProjectPage` e no `ScheduleOriginChoice`, o arquivo, a prévia com os avisos, os responsáveis do arquivo (pessoa e papel) e os textos em `pt-BR.json`; o contrato e os tipos gerados de novo.
- `web/scripts/demo`: o projeto importado do MS Project no plano.
- Documentação: `CLAUDE.md`, `docs/glossario.md`, o `discovery.md` e o contexto do OpenSpec (a importação sai do "não faz").
- `src/Modules/Projects`, na exportação: o e-mail dos responsáveis no XML e a permissão de baixá-lo (`ProjectPermissions`); `web/src/projects`, o XML no "⋮" só para quem pode.
- ADRs: 0135 e 0136 (novos), 0005, 0017, 0019, 0021, 0042, 0043, 0088, 0102 e 0119.
