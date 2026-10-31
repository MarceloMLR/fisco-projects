## Context

O modelo de projeto (ADR-0017) já faz quase todo o caminho:

- O `ProjectTemplate` guarda as fases, os itens (fase, tarefa em que está, tipo, nome, distância em dias úteis ao primeiro início, duração e ordem) e as dependências.
- O `CreateProject` recebe `templateId` e `scheduleStart`, e o `Schedule.ApplyTemplate` gera o cronograma na mesma transação, contando os dias úteis e pondo cada item depois dos que ele depende.
- A tela do "Novo projeto" escolhe a origem (`ScheduleOriginChoice`) e mostra a prévia (`TemplatePreview`).
- A exportação (`MsProjectExport`) grava o MSPDI com `System.Xml.Linq`.
- A importação da planilha dos acessos já tem o desenho de prévia sem guardar e criação com o que a tela confirmou (ADR-0094).

## Goals / Non-Goals

**Goals:**

- A mesma regra do modelo para gerar o cronograma, sem um segundo caminho no domínio.
- A prévia com as datas que o cronograma vai ter de verdade, calculadas pelo servidor.
- Leitura segura do arquivo enviado.

**Non-Goals:**

- Guardar o arquivo, ou o cronograma convertido entre a prévia e a criação.

## Decisions

- **O plano do cronograma como contrato do domínio.**
  - Um `SchedulePlanOutline` (fases com nome e ordem; itens com fase, tarefa em que está, tipo, nome, distância e duração; dependências) passa a ser o que o `Schedule.ApplyTemplate` aplica.
  - O `ProjectTemplate` o entrega (`ToOutline()`), e a importação o monta do arquivo.
  - A regra de dias úteis, ordem e dependências fica num lugar só.
  - Alternativa: criar um `ProjectTemplate` temporário, que é entidade com tabela e auditoria; misturaria o importado com os modelos.
- **Leitor próprio, em duas etapas, na `Application` do `Projects`.**
  - O `MsProjectImport.Read(Stream)` lê o XML com `XmlReader` sem DTD (`DtdProcessing.Prohibit`, sem resolvedor), até 5 MB e 2.000 tarefas. Ele devolve as linhas do arquivo: UID, nível, resumo, marco, ativa, nome, início, fim e as ligações com o tipo e o intervalo.
  - O `MsProjectImport.Convert(lines, days)` aplica as regras do ADR-0135 (níveis, datas, dependências, nomes) e devolve o `SchedulePlanOutline` com os avisos agrupados por tipo, com os nomes.
  - As duas etapas são testadas sem banco, com arquivos de exemplo e com o XML que o `MsProjectExport` grava, ida e volta.
  - O namespace `http://schemas.microsoft.com/project` é exigido; sem ele, o arquivo não é do MS Project.
- **Datas.**
  - O início e o fim de cada linha vêm de `Start` e `Finish` (só o dia).
  - A distância é a contagem de dias úteis do calendário da instalação (`IBusinessDays`) entre o primeiro dia do arquivo e o início do item, como no `ProjectTemplate.From`.
  - A duração da tarefa é a contagem de dias úteis entre o início e o fim, de 1 a 999.
  - O `Duration` do MS Project não é usado, porque depende das horas por dia e do calendário do arquivo.
- **Prévia sem guardar.**
  - `POST /api/projects/ms-project-preview` (multipart, com `SystemPolicies.ProjectCreators`) recebe o arquivo e o início opcional.
  - Ela devolve o título do arquivo, o primeiro dia, o plano convertido e os avisos, além das fases e dos itens com as datas finais. As datas saem de um `Schedule` montado em memória pelo mesmo `ApplyTemplate`, sem gravar.
  - A resposta vai com `no-store`.
  - Erros em Problem Details: 400 com o tipo `ms-project-file-refused` e o motivo (não é o XML do MS Project, grande demais, tarefas demais, sem tarefas).
- **Criação com o plano.**
  - `POST /api/projects` aceita `importedSchedule`: as fases com os itens, cada item com o índice da tarefa em que está e os índices dos que ele depende. Ele vem no lugar de `templateId`, com `scheduleStart`; os dois juntos dão 400.
  - O servidor valida o plano com as regras do cronograma: nomes, tipos, durações de 1 a 999 e dependências só entre itens com datas próprias, de itens anteriores. Ele gera o cronograma pelo mesmo `ApplyTemplate`, na transação da criação.
  - A tela manda o plano que a prévia devolveu, sem reenviar o arquivo.
  - Alternativa: reenviar o arquivo em multipart na criação, que obrigaria o formulário de criação inteiro a virar multipart.
- **Tela.**
  - A terceira opção do `ScheduleOriginChoice` mostra o campo do arquivo (`.xml`), o início e a prévia, num `TemplatePreview` generalizado que recebe as fases e os itens.
  - A prévia tem também um `Alert` de tom `info` com os avisos ("3 dependências de outro tipo saíram: …"). Fora do tom, o desenho é o das telas aprovadas do "Novo projeto", sem cor ou medida nova.
  - Trocar o início pede a prévia de novo.
  - O erro do arquivo volta ao campo, com o texto da recusa.
- **Auditoria e avisos.** O projeto criado entra na auditoria como qualquer criação, e o cronograma não entra item por item, como no modelo. Ninguém é avisado, porque não há responsáveis.
- **Demonstração.** O plano baixa o XML de um projeto da demonstração pela exportação e cria, pela prévia e pela criação, um projeto novo a partir dele. Isso exercita a ida e a volta pela própria API.

## Risks / Trade-offs

- [Arquivos com estruturas que o leitor não conhece (campos faltando, datas sem hora)] → o leitor usa só `UID`, `OutlineLevel`, `Summary`, `Milestone`, `Active`, `Name`, `Start`, `Finish` e `PredecessorLink`; o que faltar segue o padrão do MS Project ou recusa a linha com aviso. Os testes cobrem arquivos gravados pelo MS Project e pela exportação.
- [O plano devolvido pela prévia é alterado no navegador antes da criação] → o servidor valida o plano inteiro com as regras do cronograma; a pessoa só consegue criar o cronograma que poderia criar à mão.
- [Arquivo grande demais pesando a memória] → o limite de 5 MB vale antes da leitura (`RequestSizeLimit` e conferência do tamanho), e o `XmlReader` lê em fluxo.
