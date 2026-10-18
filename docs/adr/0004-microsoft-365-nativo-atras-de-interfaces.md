---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0004: Microsoft 365 nativo, com e-mail e arquivos atrás de interfaces

## Contexto e problema

O ideia.md apoia o sistema no Microsoft 365: login pelo Entra ID, e-mail pelo Microsoft Graph e arquivos no SharePoint. Com o FiscoProjects como projeto pessoal que pode ser vendido (ADR-0002) e com uma instalação por empresa (ADR-0003), é preciso decidir o quanto o produto depende do M365. O destino dos documentos (SharePoint ou outro) será decidido em ADR próprio, no bloco de documentos.

## Fatores de decisão

- Segurança: não manter senhas de usuários no sistema.
- Aproveitar o conhecimento de Marcelo (.NET e M365) e o perfil do comprador provável (empresas que usam M365).
- Facilidade de testar sem depender de serviços externos.
- Poder acrescentar outros provedores no futuro sem reescrever o sistema.
- Ambiente de desenvolvimento viável sem o tenant da empresa (ADR-0002).

## Opções consideradas

- A. M365 nativo, com e-mail e arquivos atrás de interfaces
- B. Independente (login próprio, SMTP, arquivos em blob ou disco)
- C. Só o login no M365; e-mail e arquivos próprios

## Resultado da decisão

Opção escolhida: **A. M365 nativo, com e-mail e arquivos atrás de interfaces**, porque evita guardar senhas, aproveita o ecossistema que o comprador provável já usa e, pelo princípio de inversão de dependência, permite acrescentar outros provedores depois sem reescrita.

Regras:

- Login pelo Entra ID.
- E-mail e arquivos acessados apenas por interfaces da aplicação (por exemplo, `IEmailSender` e `IFileStorage`); a implementação com Microsoft Graph é a principal.
- Implementações locais de e-mail e arquivos para desenvolvimento e testes, para não depender de um tenant licenciado.

### Consequências

- Bom, porque o sistema não guarda senhas de usuários; MFA e políticas de acesso ficam no Entra ID.
- Bom, porque os testes usam fakes no lugar do Graph.
- Bom, porque outro provedor (SMTP, blob) pode ser acrescentado como nova implementação.
- Ruim, porque na v1 só empresas com Microsoft 365 conseguem usar o sistema.
- Ruim, porque o desenvolvimento precisa de um tenant Microsoft pessoal: o sandbox E5 do Developer Program depende de elegibilidade; a alternativa é um plano em teste ou pago. Licenças e custos serão confirmados no bloco de infraestrutura (suposição ainda não verificada: e-mail pelo Graph e SharePoint exigem licença M365).

### Confirmação

Revisão de código: nenhuma regra de negócio chama o Graph diretamente; o acesso passa pelas interfaces. Os testes automatizados rodam sem conexão com o Microsoft 365.

## Prós e contras das opções

### A. M365 nativo, atrás de interfaces

- Bom, porque não há gestão de senhas no sistema.
- Bom, porque mantém a integração com SharePoint e a caixa de e-mail da empresa.
- Bom, porque as interfaces abrem caminho para outros provedores.
- Ruim, porque limita a v1 a empresas com M365.

### B. Independente

- Bom, porque atende qualquer empresa.
- Ruim, porque exige gestão de senhas, redefinição, MFA e bloqueio de conta.
- Ruim, porque perde o diferencial da integração com o M365.

### C. Só o login no M365

- Bom, porque reduz a dependência do Graph.
- Ruim, porque perde o SharePoint e o envio pela caixa da empresa.
- Ruim, porque mantém a dependência do M365 para o login sem aproveitar o resto.
