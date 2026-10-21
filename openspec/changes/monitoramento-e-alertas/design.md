## Context

O host (`FiscoProjects.Api`) compõe a aplicação pelo `AddFiscoProjectsApi`, usado também pelos testes. Os logs vão para o console, no nível Information por padrão, inclusive os comandos do EF Core. O `/health` verifica o banco. O envio de e-mails roda a cada 30 segundos no `EmailDispatcher` (Notifications), que chama o `DispatchEmails`.

## Goals / Non-Goals

**Goals:**
- Telemetria pronta para o Application Insights, ligada só pela conexão.
- Prova de que nada sensível vai para os logs.
- Métricas para o alerta de fila parada.

**Non-Goals:**
- Criar o Application Insights, o teste de disponibilidade e os alertas (Terraform, Fundação-6).
- Erros do frontend no monitoramento (depois, ADR-0057).
- Verificar o Microsoft Graph no `/health` (com a integração, M365).

## Decisions

- **`Monitoring.AddFiscoProjectsMonitoring`** no host, chamado pelo `AddFiscoProjectsApi`: lê a conexão de `APPLICATIONINSIGHTS_CONNECTION_STRING` ou `AzureMonitor:ConnectionString`; sem ela, retorna sem registrar nada. Com ela, desliga as estatísticas do SDK (`APPLICATIONINSIGHTS_STATSBEAT_DISABLED` e `APPLICATIONINSIGHTS_SDKSTATS_DISABLED` no processo, antes de o exportador nascer), chama `AddOpenTelemetry().UseAzureMonitor` com a conexão e `TracesPerSecond` de `Monitoring:TracesPerSecond` (padrão 5), põe o nome do serviço (`FiscoProjects`) e o ambiente nos recursos, liga os medidores `FiscoProjects.*` e tira o `/health` da instrumentação do ASP.NET Core.
- **`EmailQueueMetrics`** (Notifications, Infrastructure, singleton): o medidor `FiscoProjects.Notifications` com os medidores observáveis `fiscoprojects.emails.waiting` e `fiscoprojects.emails.oldest_waiting` (calculado na hora da leitura, pelo `TimeProvider`, a partir do mais antigo visto na última rodada) e os contadores `fiscoprojects.emails.sent` e `fiscoprojects.emails.failed`. O `DispatchEmails` passa a devolver a fila depois da rodada (`IOutboxStore.WaitingAsync`: quantos esperam e desde quando o mais antigo), e o `EmailDispatcher` entrega o resultado às métricas.
- **Logs**: `Microsoft.EntityFrameworkCore` em Warning no `appsettings.json`; o `appsettings.Development.json` continua como está.
- **Teste dos logs**: um provedor de logs em memória no `SessionApp`, no nível Trace, guarda a mensagem, o estado e a exceção de cada log; o teste roda a entrada local, o cadastro de um acesso com senha, mostrar a senha e a troca, e procura a senha e o cookie em tudo o que foi guardado.

## Risks / Trade-offs

- [Variáveis de ambiente do processo para as estatísticas do SDK] → é a forma documentada de desligar; vale para o processo todo, que é só a aplicação.
- [Amostragem por traces por segundo] → no volume esperado não corta nada; as métricas dos alertas não sofrem amostragem.
