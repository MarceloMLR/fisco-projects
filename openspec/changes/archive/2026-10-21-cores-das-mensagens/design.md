## Context

A conversa do chamado pinta cada mensagem pelo papel de quem fala com os tokens `msg-*` (cartão), `role-*` (selo e nome) e `avatar-*` (avatar) do design system (ADR-0069). A cor principal e a secundária já geram tokens no `brandColors.ts` e chegam a todas as telas pelo `BrandColors` (ADR-0082, ADR-0100).

## Goals / Non-Goals

**Goals:**
- Uma cor por papel, com a prévia antes de salvar.
- Contraste garantido nos dois temas.

**Non-Goals:**
- Cores diferentes por projeto ou por pessoa.
- Mudar os papéis da conversa.

## Decisions

- **Domínio**: seis colunas opcionais em `installation_settings` (`RequesterMessageColor`, `OwnerMessageColor`, `ManagerMessageColor`, `TechLeadMessageColor`, `DevelopmentMessageColor`, `FunctionalMessageColor`), com o registro `MessageColors` e `TryNormalizeMessageColors` (qualquer `#RRGGBB`). Colunas e não um JSON para a auditoria dizer qual cor mudou.
- **API**: `messageColors` (objeto com os seis papéis, cada um `#RRGGBB` ou `null`) na marca e no formulário; sem o objeto, nenhum papel tem cor.
- **Tokens**: `messageTokens(cores, tema)` calcula, para cada papel com cor, `msg-{tom}-bg` e `-border` e, fora o solicitante, `role-{tom}-bg`, `role-{tom}-text` e `avatar-{tom}`: claridade do token do design system, croma e matiz do perfil do papel (o do responsável para o solicitante) deslocados pela cor escolhida contra o avatar do papel; o texto do selo vai até 4,5:1 sobre o selo, o cartão e a superfície (`withContrast`); o avatar é a cor no tema claro e o tom no escuro, escurecido até as iniciais brancas terem 4,5:1 (`darkerUntil`). `brandVariables` e `brandStyleSheet` recebem as cores, depois da principal e da secundária.
- **Tela**: a seção "Mensagens dos chamados" usa o `ColorField` (ADR-0100) com "Restaurar padrão" e a cor do design system na amostra (o avatar do papel, e o texto secundário para o solicitante); a prévia (`MessagePreview`) usa o `PersonAvatar` e o `RoleTag` da conversa com as variáveis calculadas no próprio estilo.

## Risks / Trade-offs

- [A cor aparece suave no cartão] → é o que mantém o texto legível; a cor cheia fica no avatar.
