---
status: aceita (posição do sino substituída pelo ADR-0066)
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0038: Sino de notificações no FiscoProjects, com avisos pelo Teams depois

## Contexto e problema

O e-mail é o canal de aviso da v1 (ADR-0036, ADR-0037), e o painel "Meu trabalho" mostra o que depende de cada pessoa (ADR-0010). O ideia.md previa notificações no Teams numa fase 2. Avisar no sino de atividades do Teams exige um aplicativo do FiscoProjects no Teams, instalado para as pessoas que vão receber; postar em canais do Teams dependia do Incoming Webhook, desligado entre 18 e 22 de maio de 2026 e substituído pelo Workflows (Power Automate). Haverá aviso dentro do sistema ou no Teams na v1?

## Fatores de decisão

- Quem está usando o sistema deve ver as novidades sem abrir o e-mail.
- Esforço e dependência de configuração em cada empresa.
- Facilidade de acrescentar novos canais de aviso depois.

## Opções consideradas

- A. Só e-mail na v1
- B. E-mail mais um sino de notificações dentro do FiscoProjects
- C. E-mail mais avisos no Teams

## Resultado da decisão

Opção escolhida: **B. E-mail mais sino dentro do FiscoProjects**, porque dá visibilidade às novidades dentro do sistema com esforço moderado e sem configuração extra em cada empresa; o Teams fica para depois.

Regra de arquitetura: os avisos saem por canais atrás de uma interface (e-mail, sino e, depois, Teams), no mesmo princípio do ADR-0004. As regras de quem recebe o quê não conhecem os canais; acrescentar o Teams é criar um canal novo.

Escopo:

| Item | Classificação |
| --- | --- |
| Sino na barra lateral com o número de notificações não lidas | v1 (substituído pelo ADR-0066: sino no canto superior direito, com contador vermelho) |
| Lista de notificações da pessoa, com os mesmos eventos dos e-mails (imediatos e do resumo), link para o item, "marcar como lida" e "marcar todas como lidas" | v1 |
| Atualização ao navegar e a cada minuto, sem tempo real | v1 |
| Notificações guardadas por 90 dias (configurável) | v1 |
| Atualização em tempo real | depois |
| Avisos no sino de atividades do Teams, com um aplicativo do FiscoProjects no Teams e o clique abrindo o FiscoProjects no navegador | depois (primeiro item após a v1) |
| Mensagens em canais do Teams pelo Workflows | depois |
| Notificações push no celular | fora (ADR-0012) |

### Consequências

- Bom, porque as novidades ficam visíveis dentro do sistema, com histórico de 90 dias.
- Bom, porque o Teams entra depois como um canal novo, sem mexer nas regras de notificação.
- Ruim, porque acrescenta um componente de tela e uma tabela de notificações.
- Ruim, porque, na v1, fora do sistema o aviso chega só por e-mail.

### Confirmação

Testes cobrindo: cada evento que gera e-mail gera também uma notificação no sino para os mesmos destinatários; o contador mostra só as não lidas; "marcar como lida" e "marcar todas como lidas" funcionam; notificações acima do prazo configurado são removidas. Revisão de código: as regras de notificação não chamam e-mail nem sino diretamente, só a interface de canais.

## Prós e contras das opções

### A. Só e-mail

- Bom, porque não exige trabalho a mais.
- Ruim, porque não há histórico de novidades dentro do sistema.

### B. E-mail mais sino

- Bom, porque mostra as novidades a quem está no sistema, reaproveitando os mesmos eventos.
- Ruim, porque acrescenta tela e armazenamento.

### C. E-mail mais Teams

- Bom, porque o aviso chega onde as pessoas trabalham, inclusive no celular.
- Ruim, porque exige criar, publicar e manter um aplicativo do Teams, com aprovação do administrador em cada empresa.

## Mais informações

- Avisos no feed de atividades do Teams: https://learn.microsoft.com/microsoftteams/platform/tabs/send-activity-feed-notification
- API de envio para usuário: https://learn.microsoft.com/graph/api/userteamwork-sendactivitynotification
