---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0112: Como a aplicação envia o monitoramento

Complementa o ADR-0057 (monitoramento com Application Insights e OpenTelemetry), que decidiu o quê; este decide o pacote, quando a telemetria sai, a amostragem, as métricas próprias e o que fica para o Terraform.

## Contexto e problema

O ADR-0057 decidiu instrumentar com OpenTelemetry e exportar para o Application Insights, com amostragem, sem dados sensíveis, e com alertas para sistema fora do ar, aumento de erros, fila de e-mails parada e banco perto do limite. A infraestrutura do Azure (Fundação-6) ainda espera a assinatura. Faltava decidir qual pacote usar, como a aplicação se comporta sem o Application Insights (desenvolvimento, testes, demonstração), como amostrar e de onde saem os sinais dos alertas.

## Fatores de decisão

- Instrumentação em padrão aberto, trocável sem mexer no resto do código.
- Nada sai da máquina sem o Application Insights configurado, e nada sai para a Microsoft além da telemetria da própria instalação.
- Volume dentro da faixa gratuita.
- Alertas que não dependam da amostragem.

## Opções consideradas

1. A distribuição do Azure Monitor para ASP.NET Core (`Azure.Monitor.OpenTelemetry.AspNetCore`), ligada só com a conexão configurada.
2. A distribuição nova da Microsoft (`Microsoft.OpenTelemetry`), que junta o Azure Monitor, OTLP e a observabilidade de agentes.
3. Os pacotes do OpenTelemetry montados à mão, com o exportador do Azure Monitor.

## Resultado da decisão

Opção 1: é a distribuição que o ADR-0057 descreve, licença MIT, estável desde 2023, só com o que a aplicação usa (ASP.NET Core, HttpClient e SqlClient). A opção 2 é recomendada pela Microsoft para aplicações novas, mas traz recursos de agentes que o FiscoProjects não usa e é bem mais recente; como a instrumentação é OpenTelemetry, trocar depois é mudar a configuração da telemetria no host. A opção 3 dá mais trabalho e perde as métricas padrão do Application Insights.

- **Quando sai**: só com a conexão do Application Insights em `APPLICATIONINSIGHTS_CONNECTION_STRING` (no App Service, pelo Key Vault) ou em `AzureMonitor:ConnectionString`. Sem ela, nada é registrado nem enviado: desenvolvimento, testes e demonstração continuam como estão.
- **Sem estatísticas para a Microsoft**: as estatísticas do próprio SDK (statsbeat) ficam desligadas, como a telemetria do Syncfusion e do OpenSpec.
- **Amostragem**: a padrão da distribuição, limitada por traces por segundo (`Monitoring:TracesPerSecond`, padrão 5). No volume de uma empresa, guarda quase tudo; num pico, limita o custo. Os logs seguem a amostragem dos traces.
- **Logs**: os mesmos do ASP.NET Core, com o identificador do trace; os comandos do EF Core só a partir de Warning em produção, e o `/health` fica fora das requisições registradas (o teste de disponibilidade bate nele o tempo todo). Senhas, tokens e cookies nunca aparecem: um teste roda os fluxos sensíveis com todos os logs ligados e confere.
- **Métricas próprias**, que não sofrem amostragem, para os alertas: `fiscoprojects.emails.waiting` (e-mails esperando envio), `fiscoprojects.emails.oldest_waiting` (há quantos segundos o mais antigo espera, que cresce também se o envio parar) e `fiscoprojects.emails.sent` e `fiscoprojects.emails.failed` (enviados e os que falharam de vez). Os medidores ficam nos módulos (`System.Diagnostics.Metrics`, sem pacote), com nomes que começam por `FiscoProjects.`, e o host liga todos.
- **Saúde**: o `/health` continua com o banco; o Microsoft Graph entra com a integração (M365).
- **Para o Terraform (Fundação-6)**: o recurso do Application Insights, o teste de disponibilidade no `/health` e os alertas por e-mail para o administrador — sistema fora do ar (teste de disponibilidade), aumento de erros (requisições com falha), fila parada (`fiscoprojects.emails.oldest_waiting` acima de 15 minutos) e banco perto do limite (métrica do Azure SQL).

### Consequências

- Boa, porque a aplicação fica pronta para o monitoramento sem depender da assinatura do Azure.
- Boa, porque os alertas usam métricas que a amostragem não corta.
- Ruim, porque os alertas só existem quando o Terraform da Fundação-6 rodar.
- Neutra, porque trocar de distribuição depois muda só a configuração da telemetria no host.
