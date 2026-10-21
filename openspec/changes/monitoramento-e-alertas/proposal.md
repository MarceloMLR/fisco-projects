## Why

O ADR-0057 pede monitoramento no Application Insights, com OpenTelemetry, para descobrir falhas antes dos usuários. A infraestrutura do Azure (Fundação-6) ainda espera a assinatura, mas a aplicação pode ficar pronta agora: instrumentada, sem vazar nada sensível e com as métricas que os alertas vão usar.

## What Changes

- ADR-0112: a distribuição do Azure Monitor para ASP.NET Core, ligada só com a conexão do Application Insights, sem as estatísticas do SDK, com a amostragem por traces por segundo e as métricas próprias para os alertas.
- O host liga o OpenTelemetry quando há `APPLICATIONINSIGHTS_CONNECTION_STRING` (ou `AzureMonitor:ConnectionString`), com os medidores `FiscoProjects.*` e sem o `/health` nas requisições; sem a conexão, nada muda.
- Métricas da fila de e-mails no módulo `Notifications`: e-mails esperando, a espera do mais antigo, enviados e falhados de vez.
- Comandos do EF Core só a partir de Warning na configuração padrão.
- Teste de que senhas, tokens e cookies não aparecem em nenhum log.
- Os alertas e o teste de disponibilidade ficam descritos no ADR para a Fundação-6 (Terraform), que já está adiada pela assinatura.

## Capabilities

### New Capabilities
- `monitoramento`: a telemetria da aplicação, o que nunca vai para os logs e as métricas que alimentam os alertas.

### Modified Capabilities

## Impact

- `FiscoProjects.Api` (configuração da telemetria, pacote `Azure.Monitor.OpenTelemetry.AspNetCore`), `Notifications` (métricas da fila), `appsettings.json`.
- Roadmap: os alertas e o teste de disponibilidade entram na Fundação-6.
