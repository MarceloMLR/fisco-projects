## Context

Veja `proposal.md` (Why). O ADR-0165 escolheu gerar o PowerPoint no servidor pelo Open XML SDK (G1), com um modelo `.pptx` de campos entre chaves (M1). O ADR-0166 define o que entra das horas. Esta change faz só o PowerPoint no modelo padrão: o PDF fica para a Status-5, e o modelo da empresa para a Status-6. O preenchimento, porém, já é o que o modelo da empresa vai usar.

O que já existe e é reaproveitado:

- **`GetProjectOverview` (`Projects`)**, que já monta:
  - o andamento contra o previsto (`SchedulePlan.PlannedProgress`) e o prazo contra a linha de base;
  - as fases e os marcos (`OverviewTimeline`);
  - as tarefas atrasadas com a projeção (`ScheduleProjections`);
  - os chamados (`IProjectTickets`) e as ameaças (`ScheduleImpacts`).
- **O Open XML SDK (`DocumentFormat.OpenXml` 3.5.1)**, já aprovado e usado pelo módulo `Credentials` na importação da planilha (ADR-0094).
- **As horas (`TimeTracking`)**: o saldo (`HoursBalance`, ADR-0128) e as horas das tarefas (`TaskHours`, `GetTaskHours`, ADR-0138, ADR-0144).
- **A pasta de termos (`Documents`)**: marcada (`IsAcceptance`) e nunca desativada. É o mesmo desenho que a pasta dos relatórios segue.
- **O logo e as cores da instalação (`Administration`)**, guardados para a tela (ADR-0082, ADR-0146) e ainda sem contrato para outros módulos. O logo do cliente (`Projects`, PNG ou JPG até 1 MB).

## Goals / Non-Goals

**Goals:**

- Um preenchimento só (`StatusReportFiller`), que recebe qualquer modelo com os campos do ADR-0165 e os valores, para a Status-6 só trocar o modelo.
- Os números pelas mesmas regras da Visão geral, sem uma segunda conta.
- O arquivo abre sem reparo no PowerPoint, conferido pelo validador do Open XML SDK nos testes.

**Non-Goals:**

- Paginação das listas em vários slides (as listas têm um limite, com "e mais N").
- Gráficos nativos, fontes embutidas e o PNG de reserva para o logo SVG.
- Histórico dos textos de cada relatório (o histórico é o próprio documento, com as versões).

## Decisions

### D1. O relatório fica no módulo `Projects`

O `Projects` é dono da visão geral, do cronograma, dos clientes e das permissões do projeto. O relatório usa o `GetProjectOverview` direto e pede o que é de outros módulos por contratos do `Projects.Contracts`, que cada módulo implementa, como já é feito com o `IProjectTickets`, o `IProjectAcceptances` e o `IProjectDeletion`:

| Contrato | Quem implementa | O que entrega |
| --- | --- | --- |
| `IProjectReportTickets` | `Tickets` (`TicketReportSource`) | Os chamados em aberto do projeto: número, título, status, prioridade, o registro no fornecedor que a lista mostra (`VendorTickets.ShownIn`) e os itens ligados |
| `IProjectReportHours` | `TimeTracking` (`ReportHoursSource`) | Os totais do projeto e das tarefas, ou nada para quem não pode (`TimeEntryAccess.ShowsHoursInReport`, ao lado do `SeesTaskHours`) |
| `IStatusReportDocuments` | `Documents` (`StatusReportDocuments`) | Guarda o arquivo na pasta dos relatórios do projeto (criada se faltar) como documento novo ou nova versão, em qualquer situação do projeto |

O logo e as cores da instalação vêm de um contrato novo do `Administration.Contracts`, o `IInstallationBrand`: o nome, o id do arquivo do logo claro e as cores principal e secundária.

*Alternativa: um módulo novo `StatusReports`.* O `Projects` teria de expor, por contrato, uma segunda forma de tudo o que a visão geral já monta, e as permissões do projeto seriam repetidas. Descartada. O modelo da empresa (Status-6) fica no mesmo módulo, com rotas em `/api/admin/status-report-template`, como os modelos de projeto (`/api/admin/project-templates`).

### D2. O modelo padrão montado por código

O `DefaultStatusReportTemplate` (`Infrastructure`) monta o `.pptx` do modelo padrão pelo Open XML SDK a cada geração. Ele é montado só com os campos e os rótulos do `pt-BR.json` do relatório. Os valores entram depois, pelo `StatusReportFiller`, o mesmo caminho do modelo da empresa. Na Status-6, "Baixar o padrão" entrega esse mesmo arquivo.

*Alternativa: um `.pptx` desenhado no PowerPoint e guardado como recurso.* Seria mais fácil de desenhar, mas é um binário sem revisão no PR, que pode ficar com campos diferentes dos do código. Descartada.

### D3. Os campos e o preenchimento

Os nomes dos campos são em português, como no ADR-0165, porque a empresa os lê e escreve no PowerPoint (Status-6). A lista fica num lugar só (`StatusReportFields`), que a Status-6 usa na conferência.

| Tipo | Exemplos | Regra |
| --- | --- | --- |
| Texto | `{{projeto.nome}}`, `{{relatorio.data}}`, `{{prazo.desvio}}`, `{{texto.resumo}}` | O texto de um parágrafo é juntado de todos os pedaços para achar o campo. O valor fica no pedaço onde o campo começa, com o formato dele, e os outros pedaços do campo saem. Quebras de linha do texto viram parágrafos |
| Lista | `{{fase.nome}}`, `{{marco.dia}}`, `{{atraso.empurra}}`, `{{horasTarefa.previstas}}`, `{{riscoMarco.nome}}` | A linha da tabela com campos de uma lista é a linha-modelo, repetida para cada item. Com a lista vazia, a tabela sai, e `{{fases.vazio}}`, `{{atrasos.vazio}}` (e assim por diante) dizem o texto de lista vazia, que fica em branco quando a lista tem itens. `{{atrasos.mais}}` diz "e mais N" além do limite |
| Slide repetido | `{{risco.numero}}`, `{{risco.classificacao}}` | O slide com campos `risco.*` é copiado para cada risco, na ordem, e sai quando não há risco. Os marcos de cada risco são a lista `riscoMarco` dentro dele |
| Slide condicional | `{{horas.saldo}}` | O slide com campos `horas.*` sai sem "Mostrar horas" |
| Forma pelo nome | `logo.instalacao`, `logo.cliente`, `barra.andamento` com `barra.andamento.trilho` | O logo troca a imagem e cabe na caixa, mantendo a proporção. Sem logo, a forma vira uma caixa de texto do mesmo tamanho com o nome. A largura da barra é a parte do andamento na largura do trilho |

Os limites das listas ficam no domínio (`StatusReportLimits`):

| Lista | Limite |
| --- | --- |
| Fases | 10 |
| Próximos marcos | 5 |
| Tarefas atrasadas | 5, as mais atrasadas |
| Horas das tarefas | 15, na ordem do cronograma, com os totais das que ficaram de fora |
| Marcos de um risco | 5 |

### D4. Os slides do modelo padrão (16:9, branco)

```
1. Status do projeto
┌──────────────────────────────────────────────────────────────┐
│ [logo.instalacao]            Status do projeto  [logo.cliente]│
│ ERP-FIN · Implantação do ERP financeiro                       │
│ Cliente · Situação · Gerente · 20/11/2026                     │
│ ┌ Andamento ───┐┌ Prazo final ──┐┌ Próximo marco ┐┌ Chamados ─┐│
│ │ 62%  ▓▓▓▓░░░ ││ 13/11/2026    ││ Homologação   ││ 7 abertos ││
│ │ previsto 70% ││ +2 dias úteis ││ 03/11 · em 10 ││ 1 vencido ││
│ └──────────────┘└───────────────┘└───────────────┘└───────────┘│
│ Resumo do período                                              │
│ {{texto.resumo}}                                               │
└──────────────────────────────────────────────────────────────┘
2. Cronograma: a tabela das fases (fase, período, andamento, previsto), a tabela
   dos próximos marcos (marco, dia, situação) e a das tarefas atrasadas (tarefa,
   fim, atraso, o que empurra).
3. Próximos passos: o foco atual, os próximos passos e "O que precisamos de vocês".
4…n. Risco: "#539 · título", a classificação numa etiqueta (ícone, cor e texto),
   o status, a prioridade, o fornecedor, os marcos que dependem e o ponto de atenção.
último. Horas do projeto: contratadas, adicionais, apontadas, saldo e estado,
   e a tabela das tarefas (tarefa, previstas, apontadas).
```

Todos os slides têm, no pé, o código do projeto, a data e o número do slide.

As cores vêm do tema do arquivo, que o modelo padrão preenche com a instalação:

| Cor do tema | De onde vem |
| --- | --- |
| `accent1` | A cor principal da instalação ou `action` (`#047857`). Fica só em barras, filetes e marcas, nunca como fundo de texto, para qualquer cor escolhida funcionar |
| `accent2` | A secundária ou `bg-selected` |
| Textos, bordas e cabeçalho das tabelas | Os tokens `text`, `text-secondary`, `border` e `bg-subtle` |
| Etiquetas de classificação e de estado | Os tokens de status, que nunca mudam (ADR-0146) |

### D5. Fonte Calibri no arquivo

O PowerPoint vai para o cliente e abre em qualquer computador. A IBM Plex Sans não vem com o Office, e o PowerPoint a trocaria por outra, com outras larguras. A Calibri é a mesma da exportação para o Excel (`xlsx.ts`) e vem em todo Office. A empresa troca a fonte no modelo dela (Status-6).

*Alternativa: a IBM Plex Sans embutida no arquivo.* O formato da fonte embutida no PowerPoint é ofuscado e mal documentado. Fica para depois.

### D6. O logo da instalação em SVG

O PowerPoint aceita PNG e JPG direto. Para o SVG (aceito no logo da instalação), a imagem vai pela extensão `svgBlip`, que o PowerPoint 2016 ou mais novo, o Microsoft 365 e o PowerPoint na web desenham. Ela vai com um PNG transparente de reserva, que o formato exige. Um PowerPoint mais antigo mostra o espaço vazio.

*Alternativa: converter o SVG em PNG no servidor.* Pede uma biblioteca nova. Fica para depois, se alguém usar PowerPoint antigo.

### D7. Os dados e as regras no domínio

- **`StatusReportNotes`** (`projects.status_report_notes`, uma linha por projeto):
  - os quatro textos, os riscos (`StatusReportRisk`: o chamado, a classificação, o ponto de atenção e a ordem), quando e por quem;
  - trocada a cada relatório gerado, depois de o documento ser guardado;
  - fora da auditoria, porque o documento já entra nela;
  - apagada com o projeto (`DeleteProject`).
- **`RiskLevel`**: `High`, `Medium` e `Low` ("Alta", "Média", "Baixa"). A classificação Média é a da sugestão nova.
- **`StatusReportForm.Of`**: confere os textos, os riscos e as classificações e devolve os campos inválidos (`StatusReportField`), como o `ScheduleField`.
- **`StatusReportSuggestions`**: os riscos do último relatório que continuam em aberto, na ordem de então, e depois os chamados que ameaçam marcos, pela ordem das ameaças da visão geral.
- **O andamento previsto de cada fase**: o `SchedulePlan.PlannedProgress` sobre as tarefas da fase.
- **`GenerateStatusReport`**:
  1. Confere a permissão (`ProjectPermissions.CanGenerateStatusReport`, a mesma regra do `CanExportSchedule`) e o formulário.
  2. Monta os dados (`StatusReportData`) com o `GetProjectOverview`, os contratos e os logos (`IFileStorage`).
  3. Pede o arquivo ao `IStatusReportRenderer` (`Infrastructure`).
  4. Guarda o arquivo pelo `IStatusReportDocuments`.
  5. Grava as anotações.
- **`GetStatusReportDraft`**: traz as anotações, os chamados que podem entrar e os sugeridos.

### D8. A API

| Rota | Caso de uso | O que faz |
| --- | --- | --- |
| `GET /api/projects/{id}/status-report` | `GetStatusReportDraft` | Devolve o rascunho: o último relatório (quando e por quem), os quatro textos, os chamados que podem entrar (número, título, status, se ameaça marcos) e os riscos sugeridos |
| `POST /api/projects/{id}/status-report` | `GenerateStatusReport` | Recebe os quatro textos, os riscos (`number`, `level`, `attention`) e `showHours`. Devolve o documento guardado: o id, a pasta, o nome e a versão |

- Os erros vão em Problem Details: 400 com os campos (`summary`, `focus`, `nextSteps`, `clientNeeds`, `risks`), 403 e 404.
- As permissões do projeto ganham `canGenerateStatusReport`.
- A tela baixa o arquivo pela rota dos documentos (`.../documents/{id}/content?download=true`), que a gestão já usa.

### D9. A tela

- **"Gerar relatório de status"** fica em "Ações do projeto", logo depois de "Editar dados", só com `canGenerateStatusReport`, e abre o `StatusReportDialog` (`web/src/projects`), um `Dialog` grande com o formulário.
- **O formulário**, em React Hook Form com Zod:
  - "Último relatório: 13/11/2026, por Marina Costa", quando houver.
  - Os quatro textos, pelo `TextArea` com o contador.
  - "Riscos": o `MultiSelect` dos chamados em aberto, com "Ameaça marcos" no detalhe da opção. Para cada escolhido, o título, a classificação (`SegmentedControl`) e o ponto de atenção.
  - "Mostrar horas", pelo `Checkbox`, com a ajuda do que entra e do que nunca entra.
- **"Gerar relatório"** fica em andamento até o arquivo baixar. Depois, o aviso "Relatório guardado em {pasta}" tem a ação "Abrir documentos", e as consultas dos documentos são pedidas de novo.
- Na Administração, Pastas padrão mostra a pasta dos relatórios com a mesma marca da pasta de termos, sem "Desativar".

### D10. A demonstração

O plano ganha `statusReport` no ERP-FIN: os quatro textos e o chamado da rejeição 539 como risco Alta. A carga gera o relatório pela gerente depois dos passos dos chamados, e só se o rascunho não tem último relatório.

## Risks / Trade-offs

- [Texto longo estoura a caixa no slide] → Os limites (1.000 e 500 caracteres) cabem nas caixas do modelo padrão no tamanho da fonte dele, sem depender de o PowerPoint encolher o texto. O relatório com os textos no limite é aberto e conferido antes do PR.
- [Calibri e não a fonte da marca] → É o mesmo caminho da exportação para o Excel. A empresa troca a fonte no modelo dela.
- [Logo SVG em PowerPoint antigo] → Aparece o espaço vazio. A conversão para PNG fica para depois.
- [O gerente passa a saber o saldo pelo relatório] → É a decisão do ADR-0166. Nada muda nas telas das horas.
- [Lista cortada no limite] → "e mais N" e os totais das horas que ficaram de fora mantêm os números certos.
- [O preenchimento é código nosso, e o PowerPoint guarda o texto em pedaços] → Testes do preenchimento com os campos quebrados, com as listas e com os slides repetidos, e o validador do Open XML SDK em todo arquivo gerado nos testes.

## Migration Plan

- **`Documents`**: migração que inclui a pasta padrão "Relatórios de status", com `IsStatusReport`, vista só pela gestão. Os projetos que já existem a recebem no primeiro relatório.
- **`Projects`**: migração das tabelas `status_report_notes` e `status_report_risks`.
- **Para voltar atrás**: remover as rotas e a ação da tela. As tabelas e a pasta ficam sem uso, e os documentos gerados continuam nos projetos.
