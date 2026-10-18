---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0057: Monitoramento com Application Insights e instrumentação OpenTelemetry

## Contexto e problema

A aplicação roda no App Service (ADR-0052), com processos em segundo plano (fila de e-mails, resumo diário, prazos) e integrações com o Microsoft 365. Sem monitoramento, problemas só seriam descobertos quando alguém reclamasse. Como monitorar?

## Fatores de decisão

- Descobrir falhas antes dos usuários.
- Custo dentro da faixa gratuita para o volume esperado.
- Não registrar dados sensíveis (LGPD, ADR-0033).
- Instrumentação em padrão aberto.

## Opções consideradas

- A. Application Insights, com instrumentação OpenTelemetry
- B. Só os logs do App Service
- C. Ferramenta de terceiros (por exemplo, Datadog ou Grafana Cloud)

## Resultado da decisão

Opção escolhida: **A. Application Insights com OpenTelemetry**, porque é nativo do Azure, cabe na faixa gratuita para esse volume e usa um padrão aberto.

Regras:

- Instrumentação com OpenTelemetry, exportando para o Application Insights; o recurso é criado pelo Terraform (ADR-0053).
- Logs estruturados, com identificador de correlação entre as etapas de uma requisição.
- Nunca registrar senhas, tokens ou o conteúdo dos acessos; dados pessoais só quando necessário.
- Endereço de saúde (`/health`) verificando o banco e o Microsoft Graph, mais um teste de disponibilidade do Azure.
- Alertas por e-mail para o administrador: sistema fora do ar, aumento de erros, fila de e-mails parada (ADR-0036) e banco perto do limite.
- Amostragem dos dados para ficar dentro da faixa gratuita.

Escopo:

| Item | Classificação |
| --- | --- |
| Logs, requisições, erros e métricas do backend no Application Insights | v1 |
| Endereço de saúde e teste de disponibilidade | v1 |
| Alertas por e-mail | v1 |
| Erros do frontend enviados ao monitoramento | depois |

### Consequências

- Bom, porque falhas e lentidões aparecem antes dos usuários perceberem.
- Bom, porque a instrumentação continua a mesma se a ferramenta mudar.
- Ruim, porque acrescenta configuração (coberta pelo Terraform).
- Neutro, porque o custo do teste de disponibilidade é baixo, a confirmar na implementação.

### Confirmação

Testes cobrindo que nenhum log contém senha, token ou conteúdo de acesso; o endereço de saúde falha quando o banco está indisponível; os alertas estão definidos no Terraform.

## Prós e contras das opções

### A. Application Insights com OpenTelemetry

- Bom, porque é nativo, barato para esse volume e usa padrão aberto.
- Ruim, porque exige configuração.

### B. Só logs do App Service

- Bom, porque não exige nada a mais.
- Ruim, porque não alerta nem mostra tendências.

### C. Ferramenta de terceiros

- Bom, porque é poderosa.
- Ruim, porque tem custo e fica fora do Azure.
