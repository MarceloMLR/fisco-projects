## Why

Os avisos que pedem ação saem na hora por e-mail e, com o Teams ligado na instalação, pela aba Atividade do Teams. Hoje cada pessoa só escolhe entre E-mail, Teams e Os dois, quem nunca escolheu recebe pelos dois, e sem o Teams o e-mail não se desliga. Em 02/11/2026, ao testar o Teams, Marcelo pediu que cada pessoa possa ligar e desligar o e-mail e o Teams à vontade, inclusive os dois, com o e-mail ligado e o Teams desligado de início, e que a auditoria registre quem ligou ou desligou cada um (ADR-0151). Avisos-5, fora do roadmap aprovado: a proposta é aprovada por Marcelo antes da implementação.

## What Changes

- **Meu perfil, cartão Avisos:** os avisos que pedem ação ganham duas caixas independentes, gravadas na hora.
  - "Por e-mail", marcada para quem nunca escolheu.
  - "Pelo Teams", desmarcada para quem nunca escolheu, só com o Teams disponível para a pessoa, com a ajuda de que o aplicativo do FiscoProjects precisa estar no Teams dela.
  - Com as duas desmarcadas, o cartão diz que os avisos continuam no sino e no Meu trabalho.
  - O resumo diário continua numa caixa própria.
- **Entrega:** cada aviso na hora vai por e-mail só para quem tem o e-mail ligado e pelo Teams só para quem tem o Teams ligado e disponível. O sino recebe sempre.
  - **BREAKING:** sai a volta ao e-mail quando o aviso não chega pelo Teams.
  - **BREAKING:** quem nunca escolheu deixa de receber pelo Teams até ligá-lo.
- **Escolhas já gravadas:** E-mail vira só o e-mail ligado, Teams vira só o Teams ligado, e Os dois vira os dois ligados.
- **Auditoria:** ligar e desligar o e-mail, o Teams e o resumo diário entram na auditoria, com a pessoa e quem mudou.
- **API (BREAKING):** `GET` e `PUT /api/profile/immediate-notices` passam a usar `email` e `teams`, cada um verdadeiro ou falso, no lugar de `channel`; o `GET` continua com `teamsAvailable`.
- **Documentação:** o ADR-0151, os ADR-0037 e 0149 apontando para ele, o guia de instalação e o roadmap.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `avisos`: o canal dos avisos na hora em duas escolhas independentes, com o e-mail ligado e o Teams desligado de início, sem a volta ao e-mail.
- `perfil`: o cartão Avisos com as duas caixas e o resumo diário, e as três escolhas na auditoria.

## Non-goals

- Desligar o sino ou tirar algo do Meu trabalho.
- Escolher o canal por tipo de aviso (só para menções, só para prazos).
- Mudar quais avisos saem na hora e quais vão para o resumo diário.
- Mudar a configuração da caixa de envio ou de "Avisar pelo Teams" pelo administrador.
- Dizer em Meu perfil se o aplicativo do Teams está instalado para a pessoa.
- Dados de demonstração novos: as pessoas da demonstração ficam com o padrão.

## Impact

- **Backend (`Notifications`):** a preferência com as duas escolhas, a migração das escolhas gravadas e a auditoria dela; o roteamento dos avisos na hora pelas duas; a fila do Teams sem a volta ao e-mail; os casos de uso e as rotas do perfil; os testes.
- **Backend (`Users`):** o resumo diário na auditoria.
- **Frontend:** o cartão Avisos de Meu perfil, os textos da Auditoria para as três escolhas e os testes de tela.
- **Contrato:** `web/src/api/openapi.json` e `schema.ts` gerados de novo.
- **Docs:** ADR-0151, os ADR-0037 e 0149 apontando para ele, `docs/instalacao.md`, `CLAUDE.md` e o roadmap.
