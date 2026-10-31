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
  - Um `SchedulePlanOutline` passa a ser o que o `Schedule.ApplyTemplate` aplica: as fases com nome e ordem; os itens com fase, tarefa em que está, tipo, nome, distância, duração e, opcionais, o progresso, o responsável ou o cliente; as dependências.
  - O `ProjectTemplate` o entrega (`ToOutline()`), e a importação o monta do arquivo.
  - A regra de dias úteis, ordem e dependências fica num lugar só.
  - Alternativa: criar um `ProjectTemplate` temporário, que é entidade com tabela e auditoria; misturaria o importado com os modelos.
- **Leitor próprio, em duas etapas, na `Application` do `Projects`.**
  - O `MsProjectImport.Read(Stream)` lê o XML com `XmlReader` sem DTD (`DtdProcessing.Prohibit`, sem resolvedor), até 5 MB e 2.000 tarefas. Ele devolve:
    - as linhas do arquivo: UID, nível, resumo, marco, ativa, nome, início, fim, percentual concluído e as ligações com o tipo e o intervalo;
    - os recursos de trabalho: UID, nome e e-mail;
    - as atribuições: tarefa e recurso, na ordem do arquivo.
  - O `MsProjectImport.Convert(file, days, today)` aplica as regras do ADR-0135 (níveis, datas, dependências, nomes, progresso e o primeiro recurso de cada tarefa). Ele devolve o `SchedulePlanOutline`, com cada item apontando a chave do seu recurso, a lista dos recursos e os avisos agrupados por tipo, com os nomes.
  - As duas etapas são testadas sem banco, com arquivos de exemplo e com o XML que o `MsProjectExport` grava, ida e volta.
  - O namespace `http://schemas.microsoft.com/project` é exigido; sem ele, o arquivo não é do MS Project.
- **Datas.**
  - O início e o fim de cada linha vêm de `Start` e `Finish` (só o dia).
  - A distância é a contagem de dias úteis do calendário da instalação (`IBusinessDays`) entre o primeiro dia do arquivo e o início do item, como no `ProjectTemplate.From`.
  - A duração da tarefa é a contagem de dias úteis entre o início e o fim, de 1 a 999.
  - O `Duration` do MS Project não é usado, porque depende das horas por dia e do calendário do arquivo.
- **Prévia sem guardar.**
  - `POST /api/projects/ms-project-preview` (multipart, com `SystemPolicies.ProjectCreators`) recebe o arquivo, o início opcional e o cliente opcional do formulário.
  - Ela devolve o título do arquivo, o primeiro dia, o plano convertido e os avisos, além das fases e dos itens com as datas finais. As datas saem de um `Schedule` montado em memória pelo mesmo `ApplyTemplate`, sem gravar.
  - Ela devolve também os recursos, cada um com o nome, o e-mail, quantas tarefas tem e a sugestão:
    - a pessoa achada pelo e-mail, por um contrato novo do `Users.Contracts` (`IPeopleLookup.ByEmailsAsync`), sobre os usuários ativos e o diretório da empresa, com o id do usuário ou o do diretório, como a busca de pessoas;
    - o cliente, quando o nome do recurso é o do cliente do formulário;
    - ou nenhuma.
  - A resposta vai com `no-store`.
  - Erros em Problem Details: 400 com o tipo `ms-project-file-refused` e o motivo (não é o XML do MS Project, grande demais, tarefas demais, sem tarefas).
- **Criação com o plano.**
  - `POST /api/projects` aceita `importedSchedule`. Ele vem no lugar de `templateId`, com `scheduleStart`; os dois juntos dão 400. Ele traz:
    - as fases com os itens, cada item com o índice da tarefa em que está, os índices dos que ele depende, o progresso e a chave do recurso;
    - os recursos, cada um com a pessoa (id do usuário ou do diretório) e o papel (Colaborador ou Desenvolvedor), o cliente ou nenhum.
  - O servidor valida o plano com as regras do cronograma: nomes, tipos, durações de 1 a 999, progresso de 0 a 100, dependências só entre itens com datas próprias e de itens anteriores, e cliente só num projeto com cliente.
  - Na transação da criação, ele inclui as pessoas no projeto com o papel, pelo `IUserProvisioning` para quem ainda não é usuário (ADR-0005). O gerente e o líder técnico mantêm o papel deles, e o leitor não é aceito (`ScheduleEditing.MayOwn`). Depois, ele gera o cronograma pelo mesmo `ApplyTemplate`, com os responsáveis e o progresso.
  - A tarefa a 100% fica concluída no fim dela, e o marco a 100% atingido no dia dele, nunca depois de hoje.
  - A tela manda o plano que a prévia devolveu, sem reenviar o arquivo.
  - Alternativa: reenviar o arquivo em multipart na criação, que obrigaria o formulário de criação inteiro a virar multipart.
- **Tela.**
  - A terceira opção do `ScheduleOriginChoice` mostra o campo do arquivo (`.xml`), o início e a prévia, num `TemplatePreview` generalizado que recebe as fases e os itens.
  - A prévia tem também um `Alert` de tom `info` com os avisos ("3 dependências de outro tipo saíram: …").
  - "Responsáveis do arquivo" é uma lista com uma linha por recurso: o nome e o e-mail, quantas tarefas, a pessoa (`PersonField`, com o cliente como opção num projeto com cliente) e o papel (`Select`, só para quem não é o gerente nem o líder técnico).
  - Fora do tom e da lista, o desenho é o das telas aprovadas do "Novo projeto", sem cor ou medida nova.
  - Trocar o início pede a prévia de novo.
  - O erro do arquivo volta ao campo, com o texto da recusa.
- **Auditoria e avisos.** O projeto criado e cada pessoa incluída entram na auditoria como na criação e na inclusão de pessoas, e o cronograma não entra item por item, como no modelo. Cada pessoa incluída recebe o aviso de inclusão no projeto, como na inclusão de pessoas; as tarefas assumidas não geram um aviso por tarefa.
- **O e-mail no XML exportado (ADR-0136).**
  - O `MsProjectExport` grava o `EmailAddress` do recurso de cada pessoa responsável, lido pelo `IUserDirectory` com o nome; a pessoa anonimizada vai sem e-mail, e o cliente continua só com o nome.
  - Quem baixa sai de um lugar só, `ProjectPermissions.CanExportSchedule` (o administrador e o gerente, também com o projeto encerrado), devolvido na resposta do projeto como `canExportSchedule`; as outras pessoas do projeto recebem 403.
  - O "⋮" da aba Cronograma mostra o XML só com `canExportSchedule`; o PDF continua para todos.
- **Demonstração.** O plano baixa o XML de um projeto da demonstração pela exportação e cria, pela prévia e pela criação, um projeto novo a partir dele. Isso exercita a ida e a volta pela própria API.

## Risks / Trade-offs

- [Arquivos com estruturas que o leitor não conhece (campos faltando, datas sem hora)] → o leitor usa só `UID`, `OutlineLevel`, `Summary`, `Milestone`, `Active`, `Name`, `Start`, `Finish` e `PredecessorLink`; o que faltar segue o padrão do MS Project ou recusa a linha com aviso. Os testes cobrem arquivos gravados pelo MS Project e pela exportação.
- [O plano devolvido pela prévia é alterado no navegador antes da criação] → o servidor valida o plano inteiro com as regras do cronograma e da inclusão de pessoas; a pessoa só consegue criar o cronograma e a equipe que poderia montar à mão.
- [E-mails de recursos no arquivo, dado pessoal de terceiros] → servem só para a sugestão na prévia, que sai com `no-store`, e não são guardados nem registrados.
- [Arquivo grande demais pesando a memória] → o limite de 5 MB vale antes da leitura (`RequestSizeLimit` e conferência do tamanho), e o `XmlReader` lê em fluxo.
