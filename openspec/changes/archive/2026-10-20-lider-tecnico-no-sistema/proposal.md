# Proposta: Líder técnico no sistema (Acesso-2.2)

## Why

Marcelo não encontrou o papel do líder técnico no sistema: pelo ADR-0040, "Líder técnico" existe só em cada projeto, e a tela Usuários e Meu perfil mostram quem lidera tecnicamente como "Usuário". Ele decidiu que o papel também existe no sistema e que só quem o tem pode ser líder técnico num projeto (ADR-0097, novo).

## What Changes

- **Papel no sistema Líder técnico**, ao lado de Administrador, Gerente de projeto e Usuário: na tela Usuários (lista, filtro e menu), em Meu perfil e na auditoria.
- **Quem pode ser líder técnico num projeto**: na criação, ao incluir e ao trocar o papel, só quem está ativo e tem o papel no sistema Líder técnico ou Administrador (400 no papel ou no campo, como o gerente pelo ADR-0085). A pessoa do diretório que nunca entrou deixa de poder ser escolhida como líder técnico na criação.
- **Busca de pessoas**: o filtro `techLeads` traz só quem pode ser líder técnico, e cada pessoa diz se pode (`canBeTechLead`), como já faz com o gerente.
- **Telas**:
  - no Novo projeto, o campo do líder técnico busca só quem pode;
  - em "Incluir pessoa", quem tem o papel no sistema Líder técnico já vem com o papel Líder técnico, e o papel fica indisponível para quem não pode;
  - na aba Membros, trocar o papel para líder técnico só aparece para quem pode.
- **Demonstração**: Rafael Souza recebe o papel no sistema Líder técnico antes dos projetos.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `usuarios-e-papeis`: o papel Líder técnico no sistema e a busca de quem pode ser líder técnico.
- `projetos`: o líder técnico na criação precisa do papel no sistema; sem o líder técnico do diretório.
- `membros-do-projeto`: líder técnico com o papel no sistema, na API e na tela.


## Impact

- `Users.Contracts` (`SystemRole.TechLead`, `SystemRoles.LeadsTechnically`, `PersonInfo.CanBeTechLead`), a busca de pessoas e a tela Usuários.
- `Projects`: `CreateProject`, `AddProjectMember`, `ChangeProjectMemberRole` e a lista de membros (`canBeTechLead`).
- Telas: `UsersPage`, `PersonField`, `NewProjectPage`, `IncludePersonDialog`, `ProjectMembersPage`; textos em `pt-BR.json`.
- Testes da API que criam projetos com o líder técnico passam a dar a ele o papel no sistema.
