---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0036: E-mails enviados pelo Microsoft Graph a partir de uma caixa compartilhada, com fila no banco

## Contexto e problema

Na v1, o e-mail é o canal de notificação do FiscoProjects: não há avisos dentro do sistema nem notificações no celular (ADR-0012). O ideia.md previa envio pelo Microsoft Graph a partir de uma caixa compartilhada, com a permissão da aplicação restrita a essa caixa. O e-mail fica atrás de uma interface (ADR-0004). Como os e-mails saem?

## Fatores de decisão

- Sem senhas guardadas no sistema (ADR-0004).
- Menor privilégio: a aplicação não pode enviar como qualquer pessoa da empresa.
- Ações do usuário não podem falhar por causa do envio de e-mail.
- E-mails saindo do domínio da empresa.

## Opções consideradas

- A. Microsoft Graph a partir de uma caixa compartilhada configurável
- B. SMTP do Exchange Online (smtp.office365.com)
- C. Serviço de e-mail transacional (por exemplo, Azure Communication Services ou SendGrid)

## Resultado da decisão

Opção escolhida: **A. Microsoft Graph a partir de uma caixa compartilhada**, porque usa OAuth, envia pelo domínio da empresa, mantém os enviados na caixa e segue o ADR-0004.

Regras:

- A caixa de envio é configurada na instalação, nunca fixa no código.
- A permissão de envio da aplicação fica restrita a essa caixa pelo RBAC para aplicações do Exchange Online; a permissão Mail.Send não é concedida no Entra ID (ela permitiria enviar como qualquer pessoa).
- Os e-mails são gravados numa fila no banco (outbox) e enviados em segundo plano, com novas tentativas em caso de falha; a ação do usuário não depende do envio.
- Em desenvolvimento e nos testes, os e-mails ficam numa implementação local, que os grava sem enviar (ADR-0004).

Configuração necessária na instalação (vai para o guia de instalação e para os requisitos de implementação):

1. Criar uma caixa compartilhada no Exchange Online (por exemplo, projetos@empresa.com.br), com nome de exibição "FiscoProjects".
2. Não conceder Mail.Send no registro do aplicativo no Entra ID.
3. No Exchange Online PowerShell: registrar o aplicativo como service principal (New-ServicePrincipal), criar um escopo de gerenciamento contendo só a caixa (New-ManagementScope) e atribuir o papel "Application Mail.Send" restrito a esse escopo (New-ManagementRoleAssignment).
4. Conferir com Test-ServicePrincipalAuthorization que a aplicação envia pela caixa e é negada nas demais.
5. Informar na configuração do FiscoProjects o endereço da caixa e as credenciais do aplicativo (preferencialmente certificado).

Os comandos exatos serão confirmados na documentação atual durante a implementação.

### Consequências

- Bom, porque não há senha de e-mail no sistema e a aplicação só envia pela caixa autorizada.
- Bom, porque falhas temporárias do Graph não afetam o uso do sistema.
- Ruim, porque a instalação exige um passo no Exchange Online feito por um administrador.
- Ruim, porque o envio depende do Microsoft 365 da empresa (ADR-0004).

### Confirmação

Testes cobrindo: uma ação que gera e-mail grava o item na fila, mesmo com o envio indisponível; o processo em segundo plano envia e marca como enviado; falhas são tentadas de novo e registradas; nenhum e-mail sai em desenvolvimento e nos testes. Revisão do guia de instalação: os passos de restrição da caixa estão descritos e testados.

## Prós e contras das opções

### A. Graph com caixa compartilhada

- Bom, porque usa OAuth e o domínio da empresa.
- Ruim, porque exige restringir a permissão no Exchange Online.

### B. SMTP do Exchange Online

- Bom, porque é um protocolo padrão.
- Ruim, porque o login básico no SMTP será desativado por padrão no fim de dezembro de 2026, e usar OAuth no SMTP dá mais trabalho que o Graph.

### C. Serviço de e-mail transacional

- Bom, porque não depende de uma caixa do Microsoft 365 e escala bem.
- Ruim, porque é mais um serviço para contratar e configurar (inclusive registros de domínio) e foge do ADR-0004.

## Mais informações

- RBAC para aplicações no Exchange Online: https://learn.microsoft.com/exchange/permissions-exo/application-rbac
- Aposentadoria do login básico no SMTP AUTH (MC786329).
