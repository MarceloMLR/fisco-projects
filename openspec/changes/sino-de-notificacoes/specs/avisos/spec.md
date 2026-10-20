## ADDED Requirements

### Requirement: Notificações no sino

Cada aviso MUST virar, na hora, uma notificação no sino para cada pessoa que o recebe, pelas mesmas regras de quem recebe, inclusive os avisos que por e-mail vão para o resumo diário (ADR-0038). A notificação MUST dizer o que aconteceu, quem fez (pelo nome atual), quando, o projeto, e levar ao item. Cada pessoa MUST ver só as próprias notificações.

#### Scenario: Notificação do mesmo aviso do e-mail
- **WHEN** uma mensagem num chamado avisa o solicitante por e-mail
- **THEN** o solicitante também tem a notificação no sino, com o link para o chamado

#### Scenario: Notificação do resumo na hora
- **WHEN** muda o status de um chamado que a pessoa acompanha
- **THEN** a notificação aparece no sino na hora, mesmo que o e-mail fique para o resumo

#### Scenario: Só as próprias notificações
- **WHEN** uma pessoa pede para marcar como lida a notificação de outra
- **THEN** a API responde 404 e nada muda

### Requirement: Sino e painel

O sino MUST ficar no canto superior direito, com o número de notificações não lidas em vermelho, e no celular na barra do topo (ADR-0066). O número MUST ser atualizado ao navegar e a cada minuto, sem tempo real. O painel MUST mostrar as notificações agrupadas por dia, as não lidas destacadas, com "Não lidas" e "Todas", "Marcar todas como lidas" e "Ver todas"; escolher uma notificação MUST marcá-la como lida e abrir o item (ADR-0038).

#### Scenario: Contador de não lidas
- **WHEN** a pessoa tem 3 notificações não lidas
- **THEN** o sino mostra 3 e se anuncia como "Notificações, 3 não lidas"

#### Scenario: Abrir uma notificação
- **WHEN** a pessoa escolhe uma notificação não lida no painel
- **THEN** ela fica lida, o contador diminui e a página do item abre

#### Scenario: Marcar todas como lidas
- **WHEN** a pessoa escolhe "Marcar todas como lidas"
- **THEN** o contador some e as notificações deixam de estar destacadas

#### Scenario: Atualização a cada minuto
- **WHEN** chega uma notificação com a tela aberta
- **THEN** o contador mostra a nova notificação em até um minuto, sem recarregar a página

### Requirement: Guarda das notificações

As notificações mais antigas que o prazo da instalação MUST ser apagadas, lidas ou não; o prazo MUST ser escolhido pelo administrador em "Guardar notificações por", nas Regras gerais das Configurações gerais, de 1 a 365 dias, com o padrão de 90 dias (ADR-0038).

#### Scenario: Notificação antiga apagada
- **WHEN** uma notificação passa de 90 dias
- **THEN** ela some do sino

#### Scenario: Trocar a guarda
- **WHEN** o administrador escolhe 30 dias e salva
- **THEN** as notificações com mais de 30 dias são apagadas na rodada seguinte
