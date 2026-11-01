## Why

Toda dependência do cronograma é hoje "termina, então começa", sem intervalo. Nos projetos fiscais a equipe combina outras coisas: os testes começam junto com a parametrização, o treinamento termina junto com a migração, a homologação do cliente começa 5 dias úteis depois da entrega. Sem isso, a gestão acerta datas à mão e as perde no próximo empurrão, e o arquivo do MS Project que tem esses tipos chega com eles trocados e um aviso. GanttA-4, do grupo 14 (Aprimoramento do Gantt 1), a pedido de Marcelo, com o ADR-0137.

## What Changes

- Cada dependência passa a ter um tipo e um intervalo em dias úteis do calendário da instalação (ADR-0137, ADR-0018):
  - "Termina, então começa" (a de hoje): a seguinte começa no dia útil depois do fim da anterior (o marco, no dia do fim), mais o intervalo;
  - "Começa junto": a seguinte começa no início da anterior, mais o intervalo;
  - "Termina junto": a seguinte termina no fim da anterior, mais o intervalo;
  - o intervalo vai de -99 a 999 dias úteis: positivo espera, negativo adianta.
- O empurrão continua só para frente e em cadeia, com as três regras; antecipar não puxa (ADR-0019). O caminho crítico, o recálculo das datas pelo calendário (ADR-0126), o desfazer (ADR-0091) e os modelos de projeto (ADR-0017) levam o tipo e o intervalo.
- No detalhe da tarefa e do marco, cada item de "Depende de" ganha o tipo e o intervalo; a Lista diz o tipo e o intervalo de cada anterior; a Linha do tempo desenha a seta de "começa junto" de início a início e a de "termina junto" de fim a fim. Ligar duas barras arrastando continua criando "termina, então começa", sem intervalo.
- O pedido de incluir e editar item aceita `links` (anterior, tipo e intervalo); sem ele, `predecessorIds` continua valendo, com o tipo e o intervalo que cada dependência já tinha. A resposta do cronograma traz `links` em cada item.
- O XML do MS Project leva e traz os três tipos e o intervalo (`Type` 0, 1 e 3; `LinkLag` em décimos de minuto, `LagFormat` 7). Na importação, "começa, então termina" continua virando "termina, então começa" com aviso, e o intervalo em horas, dias corridos ou porcentagem vira dias úteis arredondados, com aviso (ADR-0135).
- O Excel diz as dependências como o MS Project em português: "3", "5II+2", "7TT-1".
- Migração no `Projects`: o tipo e o intervalo nas dependências do cronograma e dos modelos.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: os tipos e o intervalo das dependências, no cálculo das datas, na tela, no Excel e no MS Project.
- `modelos-de-projeto`: o modelo guarda o tipo e o intervalo das dependências.

## Non-goals

- O quarto tipo do MS Project ("começa, então termina") e o intervalo em horas, dias corridos ou porcentagem (ADR-0137).
- Puxar as tarefas seguintes quando a anterior antecipa (ADR-0019).
- Dependências entre projetos.

## Impact

- `src/Modules/Projects`: o tipo e o intervalo em `ScheduleDependency`, `DependencyState`, `OutlineDependency` e `ProjectTemplateDependency` (migração); o cálculo do início (`EarliestAfter`), o empurrão, o caminho crítico e o recálculo; os pedidos e as respostas do cronograma; o `MsProjectExport` e o `MsProjectImport`; os testes.
- `web/src`: o editor das dependências no detalhe, a coluna "Depende de", as setas da Linha do tempo, o Excel, os textos, os tipos gerados e os testes; a demonstração com uma dependência de cada tipo.
- Documentação: ADR-0137 (novo), ADR-0019 e ADR-0135 com a nota de substituição, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
