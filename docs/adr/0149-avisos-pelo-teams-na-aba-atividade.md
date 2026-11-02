---
status: aceita
date: 2026-11-02
decision-makers: Marcelo Ribeiro
---

# ADR-0149: Avisos pelo Teams na aba Atividade, com o canal escolhido por cada pessoa

Complementa o ADR-0038, que deixava os avisos no Teams para depois da v1. Substitui dele:

- a classificação "depois" do item "Avisos no sino de atividades do Teams", que passa a valer na v2;
- o "Bom, porque o aviso chega onde as pessoas trabalham, inclusive no celular" da opção C, que não vale para o aplicativo próprio da empresa.

Complementa o ADR-0037 só na forma de entrega: os avisos na hora continuam sem poder ser desligados, e a pessoa escolhe por onde eles chegam.

## Contexto e problema

Os avisos saem pelo e-mail (ADR-0036, ADR-0037) e pelo sino do FiscoProjects (ADR-0038). Fora do sistema, quem trabalha o dia inteiro no Teams só vê o aviso quando abre o e-mail. O ADR-0038 já separou os canais das regras de quem recebe, para o Teams entrar depois como um canal novo.

Em 02/11/2026, Marcelo viu um esquema da aba Atividade com os avisos do FiscoProjects e aprovou o aviso no Teams. A documentação do Microsoft Graph diz que:

- o aviso na aba Atividade exige um aplicativo do Teams, instalado para quem recebe;
- a permissão de envio pode vir com o próprio aplicativo (consentimento específico do recurso, `TeamsActivity.Send.User`), sem permissão nova no Entra ID;
- o clique no aviso só pode abrir um endereço do Teams;
- no iOS e no Android, o aviso só aparece para aplicativos da loja do Teams.

Como o aviso chega no Teams, e o e-mail continua?

## Fatores de decisão

- O aviso chega onde a pessoa já está trabalhando.
- Nenhuma permissão ampla no tenant da empresa (ADR-0030, ADR-0036).
- Pouca infraestrutura nova em cada instalação.
- Ninguém perde um aviso na hora: se o Teams não entrega, o e-mail entrega.
- As regras de quem recebe continuam num lugar só (ADR-0038).

## Opções consideradas

Formato (escolha de Marcelo):

- A. Aviso na aba Atividade do Teams
- B. Mensagem de um bot num chat com o FiscoProjects
- C. Post no canal do projeto pelo Workflows

E-mail com o Teams (escolha de Marcelo):

- D. Cada pessoa escolhe: e-mail, Teams ou os dois
- E. Sempre os dois
- F. O Teams substitui o e-mail na hora

## Resultado da decisão

Opções escolhidas: **A** e **D**, as duas por escolha de Marcelo.

### O aviso

- **Que avisos:** os mesmos que saem por e-mail na hora (ADR-0037), para as mesmas pessoas e pelas mesmas regras. O que vai para o resumo diário continua só no resumo e no sino.
- **Onde aparece:** na aba Atividade e no aviso do sistema operacional, com o ícone da instalação. O texto diz o que aconteceu e quem fez, e embaixo vêm o projeto e o título do item.
- **O clique:** abre, dentro do Teams, uma página do FiscoProjects com o botão "Abrir no FiscoProjects", que leva ao item no navegador. A página não mostra dados do item, porque dentro do Teams a pessoa não tem a sessão do FiscoProjects.

### O aplicativo do Teams

- **O pacote:** o administrador do FiscoProjects baixa o pacote pelas Configurações gerais, na seção Microsoft 365. O pacote sai com o nome, o logo ou o selo na cor principal, o endereço e a identificação daquela instalação.
- **A instalação:** o administrador do Teams da empresa sobe o pacote no centro de administração do Teams e instala o aplicativo para as pessoas por uma política. Isso entra no guia de instalação (ADR-0127).
- **A permissão:** só a que o próprio aplicativo declara (`TeamsActivity.Send.User`, para avisar, e `TeamsAppInstallation.Read.User`, para saber se ele está instalado para a pessoa), dada na instalação. Nada muda no Entra ID.
- **Ligar e desligar:** o administrador do FiscoProjects liga "Avisar pelo Teams" depois da instalação. Até lá, ninguém recebe pelo Teams.

### O canal de cada pessoa

- **A escolha:** em Meu perfil, a pessoa escolhe por onde chegam os avisos na hora: E-mail, Teams ou Os dois. Quem nunca escolheu fica com Os dois.
- **Quem pode escolher o Teams:** só quem entra pelo Entra ID, com os avisos pelo Teams ligados. Para os outros, os avisos na hora vão por e-mail, qualquer que seja a escolha.
- **Ninguém perde aviso:** quem escolheu só o Teams recebe o aviso por e-mail quando o aplicativo não está instalado para ela ou quando o Teams recusa o envio de vez.
- **O resumo diário:** continua só por e-mail, como hoje.

### Consequências

- Bom, porque o aviso chega no Teams, onde a pessoa já trabalha, sem permissão ampla no tenant.
- Bom, porque as regras de quem recebe não mudam: o Teams é mais um canal (ADR-0038).
- Bom, porque a pessoa que prefere o Teams deixa de receber o mesmo aviso duas vezes.
- Ruim, porque cada empresa precisa instalar o aplicativo no Teams, uma vez, e de novo quando o pacote muda (um logo novo, um tipo de aviso novo).
- Ruim, porque o aviso não aparece no celular, e o clique passa pela página do FiscoProjects no Teams antes do navegador.
- Neutro, porque a fila dos avisos do Teams segue a dos e-mails, com novas tentativas (ADR-0036).

### Confirmação

Testes cobrindo:

- cada aviso na hora indo para o Teams de quem escolheu Teams ou Os dois, e não para quem escolheu E-mail;
- quem escolheu Teams sem receber o e-mail do mesmo aviso, e recebendo o e-mail quando o aplicativo não está instalado para ela;
- os avisos do resumo fora do Teams;
- quem entra sem o Entra ID recebendo por e-mail;
- os avisos desligados na instalação;
- o pacote com a identificação, o endereço, os tipos de aviso e as permissões da instalação;
- a página do Teams levando só a endereços do próprio FiscoProjects;
- a anonimização e a exclusão do projeto apagando o que a fila do Teams guardava.

Nos testes, o Graph é simulado; nunca o Teams real (ADR-0056).

## Prós e contras das opções

### A. Aba Atividade

- Bom, porque é o lugar do Teams feito para avisos, com o aviso do sistema operacional.
- Bom, porque não pede recurso novo no Azure.
- Ruim, porque não avisa no celular, e o clique passa por uma página dentro do Teams.

### B. Bot no chat

- Bom, porque chega no celular e o botão do cartão abre direto no navegador.
- Ruim, porque pede um recurso de bot no Azure, um endereço novo na API e mais código.
- Ruim, porque o chat com o FiscoProjects acumula uma mensagem por aviso.

### C. Post no canal pelo Workflows

- Bom, porque não pede aplicativo instalado.
- Ruim, porque não é pessoal: avisa a equipe do canal, não quem precisa agir.

### D. Cada pessoa escolhe

- Bom, porque cada um recebe onde prefere, sem aviso duplicado.
- Ruim, porque é mais uma escolha no perfil.

### E. Sempre os dois

- Bom, porque é o mais simples.
- Ruim, porque o mesmo aviso chega duas vezes.

### F. O Teams substitui o e-mail

- Bom, porque não há aviso duplicado.
- Ruim, porque quem prefere o e-mail perde a escolha.

## Mais informações

- Avisos na aba Atividade: https://learn.microsoft.com/graph/teams-send-activityfeednotifications
- Envio para uma pessoa: https://learn.microsoft.com/graph/api/userteamwork-sendactivitynotification
- Permissões do próprio aplicativo: https://learn.microsoft.com/microsoftteams/platform/graph-api/rsc/resource-specific-consent
- Aplicativos próprios no centro de administração: https://learn.microsoft.com/microsoftteams/teams-custom-app-policies-and-settings
- Instalação por política: https://learn.microsoft.com/microsoftteams/teams-app-setup-policies
- Link para a página do aplicativo: https://learn.microsoft.com/microsoftteams/platform/concepts/build-and-test/deep-link-application
