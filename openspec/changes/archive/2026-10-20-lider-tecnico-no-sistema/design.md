## Context

O ADR-0085 já faz isso para o gerente: `SystemRoles.ManagesProjects`, o `MayBeManagerAsync` dos membros, o filtro `managers` e o `canBeManager` da busca de pessoas, e o `canBeManager` de cada membro na aba Membros. O líder técnico segue o mesmo desenho (ADR-0097).

## Goals / Non-Goals

**Goals:** o papel no sistema; a regra na criação, na inclusão e na troca de papel; as telas sugerindo e restringindo.

**Non-Goals:** trocar sozinho o papel no sistema de quem já é líder técnico num projeto (o administrador o faz na tela Usuários); impedir trocar o papel no sistema de quem lidera projetos (o ADR-0085 deixa o papel no projeto como está).

## Decisions

- **`SystemRole.TechLead = 4`**, guardado como os outros papéis; `SystemRoles.LeadsTechnically` diz quem pode ser líder técnico (Administrador e Líder técnico), num lugar só.
- **`PersonInfo.CanBeTechLead`** (com valor padrão falso, para não mudar quem já constrói o registro) e o filtro `TechLeadsOnly` da busca, que deixa de fora as pessoas do diretório, como o filtro de gerentes.
- **Criação**: o `techLeadDirectoryId` continua no contrato, mas é recusado (400 no líder técnico), porque quem nunca entrou tem o papel Usuário; a tela deixa de enviá-lo.
- **Ordem das checagens na troca de papel**: a regra do papel no sistema vem antes do último gerente, como a do gerente; o cenário "Rebaixar o último gerente" passa a usar o papel Desenvolvedor, que qualquer pessoa pode ter.
- **Sugestão na tela**: quem pode ser líder técnico e não pode ser gerente tem o papel no sistema Líder técnico; "Incluir pessoa" usa isso para já escolher o papel Líder técnico, sem expor o papel no sistema na busca.

## Risks / Trade-offs

- **[Instalações com líderes técnicos "Usuário"]** Continuam líderes nos projetos (ADR-0085), mas não podem ser escolhidos de novo até o administrador dar o papel. Mitigação: a demonstração já carrega assim, e o ADR-0097 orienta o administrador.
