## ADDED Requirements

### Requirement: Telemetria no Application Insights

Com a conexão do Application Insights configurada, a aplicação MUST enviar requisições, dependências, erros, logs e métricas por OpenTelemetry, com amostragem limitada por traces por segundo e sem as requisições do `/health`. Sem a conexão, nada MUST ser coletado nem enviado. As estatísticas do próprio SDK MUST ficar desligadas (ADR-0057, ADR-0112).

#### Scenario: Sem a conexão
- **WHEN** a aplicação sobe sem a conexão do Application Insights
- **THEN** ela funciona como antes, sem coletar nem enviar telemetria

#### Scenario: Com a conexão
- **WHEN** a aplicação sobe com a conexão do Application Insights
- **THEN** os traces, as métricas e os logs são coletados, com os medidores do FiscoProjects, e o `/health` fica de fora das requisições

### Requirement: Nada sensível nos logs

Nenhum log MUST trazer senhas dos acessos, cookies da sessão ou tokens, em nenhum nível (ADR-0033, ADR-0057).

#### Scenario: Fluxos sensíveis com todos os logs ligados
- **WHEN** alguém entra, cadastra um acesso com senha, mostra a senha e troca a senha, com os logs no nível mais detalhado
- **THEN** nenhum log tem a senha nem o valor do cookie da sessão

### Requirement: Métricas da fila de e-mails

A cada rodada do envio, a aplicação MUST medir quantos e-mails esperam envio e há quanto tempo o mais antigo espera, e contar os enviados e os que falharam de vez, para os alertas de fila parada (ADR-0036, ADR-0112).

#### Scenario: Fila parada
- **WHEN** dois e-mails esperam envio porque a caixa não responde
- **THEN** a métrica de espera mostra dois e-mails, e a espera do mais antigo cresce com o tempo, mesmo sem novas rodadas
