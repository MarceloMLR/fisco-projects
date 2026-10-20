# Proposta: avisos por e-mail (Avisos-2)

## Why

A fila de e-mails existe (Avisos-1), mas nenhum evento do sistema avisa ninguém. O ADR-0037 define o que chega por e-mail:
- **Na hora:** o que pede ação da pessoa (chamado novo para o time dela, resposta ou pedido de informação num chamado em que está envolvida, menção, troca de responsável ou de solicitante para ela, chamado resolvido aguardando a confirmação dela e prazo de chamado vencendo ou vencido).
- **No resumo diário:** o restante (status dos chamados que acompanha, fornecedores, datas do cronograma, tarefas que vencem nos próximos dias, inclusão em projetos e mudanças de papel ou de status do projeto), num e-mail só, no horário da instalação.

Quem fez a ação não recebe o e-mail dela, todo e-mail leva ao item e a pessoa pode desligar o resumo diário; os e-mails na hora não se desligam.

## What Changes

- **Contrato dos avisos** (`INoticeBoard` do `Notifications.Contracts`): os módulos dizem o que aconteceu (o tipo, as pessoas, quem fez, o projeto e o caminho do item); quem recebe por qual canal fica no `Notifications`, atrás dos canais (ADR-0038). Nesta change, o canal é o e-mail.
- **Regras de quem recebe**, num lugar só: tira quem fez a ação, quem está desativado e quem não vê mais o projeto.
- **Avisos dos chamados:** abertura e transferência para quem atende o time; mensagem para quem acompanha; menção para quem foi mencionado; pedido de informação e resolução para o solicitante; troca de responsável e de solicitante para a pessoa escolhida; mudanças de status, fornecedores e fechamento (inclusive o automático e o do projeto cancelado) para o resumo de quem acompanha.
- **Prazos dos chamados:** a cada hora, os prazos que passam a vencer (hoje ou no próximo dia útil) ou que vencem avisam o responsável, ou quem atende o time sem responsável, uma vez para cada prazo e estado.
- **Avisos dos projetos:** inclusão e troca de papel para o resumo da pessoa; status do projeto para o resumo das pessoas do projeto; datas do cronograma alteradas, inclusive em cadeia, para o resumo do responsável de cada tarefa, um item por tarefa.
- **Resumo diário:** no horário da instalação (padrão 07:30, em "Horário do resumo diário" das Configurações gerais), um e-mail por pessoa com as novidades desde o último resumo e as tarefas dela que vencem até o segundo dia útil seguinte. Resumo sem novidades não sai.
- **Preferência no perfil:** o cartão "Avisos por e-mail" de Meu perfil liga e desliga o resumo diário (`PUT /api/profile/daily-summary`) e mostra os avisos na hora, sempre ligados.
- **Textos:** um assunto, um parágrafo e um botão para cada tipo, no arquivo de tradução dos e-mails (ADR-0013).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `avisos` (criada na Avisos-1): as regras de quem recebe, os avisos dos chamados e dos projetos, os alertas de prazo e o resumo diário.
- `configuracao-da-instalacao`: o horário do resumo diário.
- `perfil`: a preferência do resumo diário.

## Impact

- **Backend:** contrato `INoticeBoard` e os tipos de aviso; tabelas `notifications.digest_items` e `notifications.daily_summaries`; os alertas de prazo nos chamados (migração no `Tickets`); `DailySummaryAt` na instalação (migração no `Administration`); `ReceivesDailySummary` no usuário (migração no `Users`); contrato `IUpcomingTasks` do `Projects`.
- **API:** `PUT /api/profile/daily-summary`; `dailySummaryAt` em `PUT /api/installation` e em `GET /api/installation/rules`; `receivesDailySummary` em `GET /api/profile`.
- **Frontend:** "Horário do resumo diário" nas Configurações gerais e o cartão "Avisos por e-mail" em Meu perfil.
- **Depende da Avisos-1** (`fila-de-emails`), ainda sem merge: o PR desta change vem depois do dela.
- **Fora do escopo:** o sino (Avisos-3), os pedidos de aceite e os aceites pendentes (Documentos-5) e escolher evento por evento o que receber (ADR-0037, depois).
