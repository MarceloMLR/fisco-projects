## Context

O nome e o e-mail de uma pessoa ficam no cadastro (`users.users`) e no vínculo com o provedor (`users.identity_links`, o e-mail na entrada local). Fora dele, os módulos guardam o id, e as telas leem o nome atual (auditoria pelo `IAuditReferenceNames`, sino ao listar), menos em quatro lugares: os registros da auditoria do próprio cadastro (rótulo, nome e e-mail antes e depois) e das entradas recusadas (o e-mail usado), as menções nas mensagens (o JSON do Tiptap guarda o nome), e o conteúdo dos e-mails da fila (até 30 dias). As notificações do sino e os itens do resumo da pessoa só têm sentido para ela.

## Goals / Non-Goals

**Goals:**
- Nenhum nome, e-mail ou vínculo da pessoa sobrando no banco depois da anonimização, com os chamados e as mensagens acessíveis.
- A troca toda numa transação, que pode ser repetida.
- O prazo da auditoria configurável e a limpeza mensal.

**Non-Goals:**
- Texto digitado (mensagens, descrições, nomes de arquivos) e exportar os dados da pessoa (ADR-0058).
- Excluir a pessoa.

## Decisions

- **Contrato `IPersonalDataEraser`** (`FiscoProjects.Persistence.PersonalData`): `EraseAsync(DbContext context, PersonErasure erasure)`, com o id, o nome, o e-mail e o identificador. Cada módulo que guarda uma cópia implementa na `Infrastructure`, abrindo um contexto dele na mesma conexão e na mesma transação, como o `ChangeAuditor`. Alternativa descartada: avisar depois de gravar (como os avisos), que deixaria a troca pela metade numa falha, sem o nome para repetir.
- **`AnonymizeUser`** (Users): numa transação serializável, confere a pessoa (existe, desativada, não anonimizada), toma o próximo número (`AnonymousNumber`, único), anonimiza o cadastro (`User.Anonymize`), apaga os vínculos, grava (com a auditoria da gravação) e chama os apagadores de todos os módulos, inclusive o da auditoria, que alcança o registro que acabou de ser gravado. O identificador vem de `Users:AnonymousName` ("Pessoa removida nº {0}"), configuração da instalação, não texto no código. `POST /api/users/{id}/anonymization`: 204, 404, 409 (`user-active` ou `user-anonymized`). A pessoa anonimizada não é reativada nem muda de papel (409).
- **Auditoria**: `AuditEntry.EraseOwner` troca o rótulo dos registros do item `User` da pessoa pelo identificador e transforma `Name` e `Email` em campos secretos, sem os valores; `AuditEntry.EraseIdentity` troca pelo identificador o e-mail das entradas recusadas com ele. A data da anonimização fica fora da auditoria, como a da criação: o registro já diz quando foi; o número entra, e é por ele que a tela reconhece a anonimização.
- **Chamados**: `RichText.RenameMention` troca o `label` das menções com o id da pessoa; as mensagens com o id no texto são lidas e regravadas. A descrição não tem menções (ADR-0026).
- **Avisos**: `OutboxEmail.Withdraw` esquece os e-mails para o endereço da pessoa (o pendente passa a Falhou, sem sair); `OutboxEmail.Rename` troca o nome dela pelo identificador nos e-mails ainda guardados de outras pessoas (assunto, texto e HTML, também com o nome codificado em HTML); as notificações e os itens do resumo dela são apagados.
- **Prazo**: `AuditRetentionYears` nas configurações da instalação (1 a 10, padrão 5), no `InstallationInfo` e em `PUT /api/installation`, e lido por `GET /api/installation/rules`. O `AuditCleanup`, em segundo plano, confere a cada hora se o mês da instalação já teve limpeza (`audit.cleanups`, um por mês); se não, apaga os registros anteriores ao início do dia de hoje menos o prazo, em lotes, e grava a limpeza e, quando apagou algum, um registro de auditoria (`Cleaned`, do sistema, com o primeiro dia guardado e a quantidade); sem nada apagado, o registro ficaria em toda instalação nova.
- **Telas**: em Usuários, a pessoa desativada ganha "Anonimizar" no "⋮", com um diálogo que diz o que muda e que não dá para desfazer; a anonimizada aparece com a situação "Anonimizado" (ícone, cor neutra e texto, no masculino como "Ativo" e "Inativo"), "Nome e e-mail removidos" no lugar do e-mail e sem ações. Nas Configurações gerais, "Guardar auditoria por (anos)". Na Auditoria, os textos da limpeza e dos campos que só dizem que mudaram.

## Risks / Trade-offs

- [Mensagens com o nome digitado] → fora, como decidiu o ADR-0058; a empresa revisa caso a caso.
- [Muitos registros da pessoa na auditoria] → lidos e regravados de uma vez, na transação; o volume de uma pessoa é pequeno perto do da auditoria.
- [Limpeza grande no primeiro mês] → em lotes, cada um na própria gravação.
- [Demonstração] → Sofia Martins sai da empresa uma vez, enquanto ninguém está anonimizado; o diretório de demonstração continua com ela, como quem nunca entrou, porque a entrada local só aceita quem está nele.
